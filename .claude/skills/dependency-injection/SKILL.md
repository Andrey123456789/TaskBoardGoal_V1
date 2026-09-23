---
name: dependency-injection
description: >
  Dependency injection patterns for .NET 10. Covers service lifetimes, keyed
  services, the decorator pattern, factory pattern, and common DI pitfalls.
  Load this skill when registering services, resolving lifetime issues, designing
  service composition, or when the user mentions "DI", "dependency injection",
  "service registration", "AddScoped", "AddTransient", "AddSingleton", "keyed
  services", "decorator", "Scrutor", "IServiceCollection", or "captive dependency".
---

# Dependency Injection

## Core Principles

1. **Constructor injection is the default** — Pass required dependencies explicitly through constructors. Avoid service locator and property injection.
2. **Respect service lifetimes** — Never capture a shorter-lived dependency inside a longer-lived service.
3. **Use abstractions at architectural boundaries** — Application services, repositories, and external adapters normally use interface-to-implementation registration. Concrete registration is fine for implementation-only helpers that do not need a separate contract.
4. **Keep registration close to the owning layer** — Application registers Application services; Infrastructure registers persistence and infrastructure adapters; API acts as the composition root.
5. **Prefer explicit registration for small applications** — Add convention scanning, keyed services, factories, or decorators when they solve an actual composition problem.

## Layer Registration

Application exposes its own registration method:

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();

        return services;
    }
}
```

Infrastructure registers technical implementations:

```csharp 
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            // Configure the provider selected by the project.
        });

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}

```

API composes the application:

```csharp
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);
```
## Patterns

### Keyed Services (.NET 8+)

Use keyed services when multiple implementations of the same abstraction are
selected by a known key. Do not introduce them when a normal single registration
or a simple explicit factory is clearer.

```csharp
// Registration
builder.Services.AddKeyedScoped<INotificationService, EmailNotificationService>("email");
builder.Services.AddKeyedScoped<INotificationService, SmsNotificationService>("sms");

internal sealed class EmailOrderNotifier(
    [FromKeyedServices("email")] INotificationService notifier)
{
    public Task NotifyAsync(
        Notification notification,
        CancellationToken cancellationToken)
    {
        return notifier.SendAsync(notification, cancellationToken);
    }
}
```

### Decorator Pattern

Use decorators when the same cross-cutting behavior should wrap an abstraction
without changing its implementation.

```csharp
public interface IOrderService
{
    Task<Guid> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken);
}

internal sealed class OrderService(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
    : IOrderService
{
    public async Task<Guid> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = new Order(
            Guid.NewGuid(),
            request.CustomerId,
            clock.GetUtcNow());

        await orders.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}

internal sealed class LoggingOrderService(
    IOrderService inner,
    ILogger<LoggingOrderService> logger)
    : IOrderService
{
    public async Task<Guid> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating order");

        var id = await inner.CreateAsync(
            request,
            cancellationToken);

        logger.LogInformation(
            "Order {OrderId} created",
            id);

        return id;
    }
}
```

If Scrutor is already used by the project:

```csharp
services.AddScoped<IOrderService, OrderService>();
services.Decorate<IOrderService, LoggingOrderService>();
```
Do not add Scrutor solely for one decoration if normal composition is simpler.

### Registration by Convention (Scrutor)

Prefer explicit registrations in small and medium projects because they are easy
to discover and debug.

Use assembly scanning when registration volume creates genuine repetitive
boilerplate. Scan the assembly that actually owns the services; do not
accidentally scan only the API assembly.


```csharp
// Auto-register all services matching a convention
builder.Services.Scan(scan => scan
    .FromAssemblyOf<IOrderService>()
    .AddClasses(classes => classes.AssignableTo<ITransientService>())
    .AsImplementedInterfaces()
    .WithTransientLifetime()
    .AddClasses(classes => classes.AssignableTo<IScopedService>())
    .AsImplementedInterfaces()
    .WithScopedLifetime());
```

### Factory Pattern

When you need runtime logic to select an implementation.

```csharp
builder.Services.AddScoped<IPaymentProcessor>(sp =>
{
    var config = sp.GetRequiredService<IOptions<PaymentOptions>>().Value;
    return config.Provider switch
    {
        "stripe" => ActivatorUtilities.CreateInstance<StripeProcessor>(sp),
        "paypal" => ActivatorUtilities.CreateInstance<PayPalProcessor>(sp),
        _ => throw new InvalidOperationException($"Unknown payment provider: {config.Provider}")
    };
});
```

### Options Registration

```csharp
// Bind configuration section to a strongly-typed options class
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration("Jwt")
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Inject as IOptions<T>
public class TokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _jwt = options.Value;
}
```

## Anti-patterns

### Don't Capture Scoped Services in Singletons

```csharp
// BAD — singleton captures a scoped application service
public sealed class BackgroundWorker(IOrderProcessor processor)
{
}

```
When a singleton or hosted service genuinely needs to perform scoped work,
create a scope for each operation and resolve a scoped orchestration service,
not Infrastructure internals.
```csharp
public sealed class BackgroundWorker(
    IServiceScopeFactory scopeFactory)
{
    public async Task ProcessAsync(
        CancellationToken cancellationToken)
    {
        await using var scope =
            scopeFactory.CreateAsyncScope();

        var processor =
            scope.ServiceProvider
                .GetRequiredService<IOrderProcessor>();

        await processor.ProcessAsync(cancellationToken);
    }
}
```
### Don't Register Everything as Singleton

```csharp
// BAD — application service depends on scoped repositories/UoW
services.AddSingleton<IOrderService, OrderService>();

// GOOD
services.AddScoped<IOrderService, OrderService>();
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| Application service using repositories/UoW | Scoped|
| Pure stateless helper with no scoped dependency | Transient or concrete registration|
| Immutable/shared configuration | Singleton-compatible |
| DbContext | Scoped (registered by `AddDbContext`) |
| Multiple implementations | Keyed services (strategy pattern) |
| Cross-cutting behavior | Decorator pattern |
| Convention-based registration | Scrutor |
| Runtime implementation selection | Factory delegate |
| Strongly-typed config | `AddOptions<T>().BindConfiguration()` |
