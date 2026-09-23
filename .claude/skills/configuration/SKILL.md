---
name: configuration
description: >
  Configuration patterns for .NET 10 applications. Covers the Options pattern,
  IOptionsSnapshot vs IOptions, secrets management, and environment-based
  configuration.
  Load this skill when setting up application configuration, managing secrets,
  binding configuration sections, or when the user mentions "configuration",
  "appsettings", "Options pattern", "IOptions", "IOptionsSnapshot", "secrets",
  "user secrets", "environment variables", "connection string", or "config binding".
---

# Configuration

## Core Principles

1. **Prefer strongly typed options for structured settings consumed by services** —
   Avoid string-based `IConfiguration` lookups throughout application code.
   Direct `IConfiguration` access is appropriate in the composition root,
   configuration-provider setup, and infrastructure registration when needed.

2. **Validate important configuration early** —
   Use startup validation for settings whose absence or invalid value prevents
   the application from operating correctly.

3. **Secrets never in source** —
   Use `dotnet user-secrets` or another local secret mechanism for development,
   and environment-specific secret injection or a managed secret store in
   deployed environments.

4. **Understand provider precedence** —
   With the normal ASP.NET Core host, configuration commonly layers
   `appsettings.json`, `appsettings.{Environment}.json`, development user
   secrets, environment variables, and command-line arguments, with later
   providers overriding earlier ones. Custom providers follow their registration
   order.

## Patterns

### Options Pattern

```csharp
// Options class with validation attributes
public class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required]
    public required string ConnectionString { get; init; }

    [Range(1, 100)]
    public int MaxRetryCount { get; init; } = 3;

    [Range(1, 60)]
    public int CommandTimeoutSeconds { get; init; } = 30;
}

// Registration with validation
builder.Services.AddOptions<DatabaseOptions>()
    .BindConfiguration(DatabaseOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart(); // Fails at startup if configuration is invalid
```

```json
// appsettings.json
{
  "Database": {
    "ConnectionString": "",
    "MaxRetryCount": 3,
    "CommandTimeoutSeconds": 30
  }
}
```

### Injecting Options

```csharp
// IOptions<T> — stable configuration value
public sealed class PricingService(
    IOptions<PricingOptions> options)
{
    private readonly PricingOptions _pricing = options.Value;
}

// IOptionsSnapshot<T> — refreshed for each created scope
public sealed class PricingService(
    IOptionsSnapshot<PricingOptions> options)
{
    private readonly PricingOptions _pricing = options.Value;
}

// IOptionsMonitor<T> — supports observing current values over time
public sealed class BackgroundWorker(
    IOptionsMonitor<WorkerOptions> options)
{
    public void DoWork()
    {
        var current = options.CurrentValue;
    }
}
```

### Custom Validation (Complex Rules)

```csharp
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration("Jwt")
    .Validate(options =>
    {
        if (string.IsNullOrEmpty(options.Key) || options.Key.Length < 32)
            return false;
        if (options.ExpirationMinutes <= 0)
            return false;
        return true;
    }, "JWT key must be at least 32 characters and expiration must be positive")
    .ValidateOnStart();
```

### External Secret Provider Example: Azure Key Vault

Azure Key Vault is one possible managed secret provider. Use the provider that
matches the hosting environment; do not add Azure-specific dependencies unless
the project actually uses Azure.

```csharp
// Program.cs — add Key Vault as a configuration source
if (builder.Environment.IsProduction())
{
    var keyVaultUri = new Uri(builder.Configuration["KeyVault:Uri"]!);
    builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
}
```

### Configuration for Multiple Environments

```csharp
// Named options — different config per named instance
builder.Services.AddOptions<SmtpOptions>("internal")
    .BindConfiguration("Smtp:Internal");
builder.Services.AddOptions<SmtpOptions>("customer")
    .BindConfiguration("Smtp:Customer");

// Usage
public class EmailService(IOptionsSnapshot<SmtpOptions> options)
{
    public async Task SendInternalEmail(string to, string body)
    {
        var smtp = options.Get("internal");
        // ...
    }
}
```

## Anti-patterns

### Don't Spread String-Based IConfiguration Access Through Services

Using `IConfiguration` in `Program.cs`, composition code, or infrastructure
registration is normal. The problem is making ordinary application services
depend on configuration keys and parsing.

```csharp
// BAD — stringly typed and scattered configuration access
public sealed class PricingService(IConfiguration configuration)
{
    public decimal GetDiscount()
    {
        return decimal.Parse(
            configuration["Pricing:DefaultDiscount"]!);
    }
}

// GOOD — strongly typed options
public sealed class PricingService(
    IOptions<PricingOptions> options)
{
    public decimal GetDiscount()
    {
        return options.Value.DefaultDiscount;
    }
}
```

### Don't Put Secrets in appsettings.json

```json
// BAD — committed to source control
{
  "Jwt": { "Key": "super-secret-key" },
  "Database": { "ConnectionString": "Server=prod;Password=secret" }
}

// GOOD — appsettings.json has defaults/structure only
{
  "Jwt": { "Key": "", "Issuer": "myapp", "Audience": "myapp" },
  "Database": { "ConnectionString": "" }
}
// Secrets provided via user-secrets (dev) or env vars / Key Vault (prod)
```

### Don't Skip Startup Validation

```csharp
// BAD — misconfiguration discovered at runtime
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// GOOD — fail fast at startup
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration("Jwt")
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| Binding config to class | Options pattern with `BindConfiguration` |
| Simple, immutable config | `IOptions<T>` |
| Config that changes per request | `IOptionsSnapshot<T>` |
| Background service watching config | `IOptionsMonitor<T>` |
| Development secrets | `dotnet user-secrets` |
| Production secrets | Environment/platform secret injection or chosen managed secret store |
| Validating config | `ValidateDataAnnotations()` + `ValidateOnStart()` |
| Multiple configs of same type | Named options with `IOptionsSnapshot<T>.Get(name)` |
