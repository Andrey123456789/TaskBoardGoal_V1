---
name: ef-core
description: >
  Entity Framework Core implementation guidance for the Infrastructure layer.
  Covers DbContext configuration, repositories, Unit of Work implementation,
  tracking, projections, pagination, migrations, transactions, bulk operations,
  compiled queries, interceptors, and query performance.
  Use when implementing or reviewing EF Core persistence, LINQ queries,
  DbContext configuration, migrations, repositories, database transactions,
  or database performance.
---

# Entity Framework Core

## Role in the Architecture

EF Core is an Infrastructure implementation detail.

Application code must not depend on:

- `DbContext`;
- `DbSet<T>`;
- EF Core APIs;
- `IQueryable<T>` backed by EF Core;
- provider-specific database types.

Application defines persistence contracts such as specific repository interfaces
and `IUnitOfWork`. Infrastructure implements those contracts using EF Core.

EF Core's `DbContext` already provides change tracking and unit-of-work behavior
internally. `IUnitOfWork` is the Application-facing abstraction over the commit
boundary; its Infrastructure implementation normally delegates to the same
`DbContext`.

## DbContext Configuration

Keep entity configuration separate with `IEntityTypeConfiguration<T>`.

```csharp
internal sealed class AppDbContext(
    DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
    }
}
```

```csharp
internal sealed class OrderConfiguration
    : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Total)
            .HasPrecision(18, 2);

        builder.HasIndex(x => x.CreatedAt);
    }
}
```

Keep persistence-specific configuration out of Domain entities where practical.

## Registration

Register EF Core and persistence implementations in Infrastructure.

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
```

Use the database provider selected by the actual project. Do not add a provider
merely because it appears in an example.

## Specific Repositories

Implement Application repository interfaces in Infrastructure.

```csharp
internal sealed class OrderRepository(AppDbContext db)
    : IOrderRepository
{
    public Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return db.Orders
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task AddAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        await db.Orders.AddAsync(order, cancellationToken);
    }
}
```

Repositories normally stage changes. They do not call `SaveChangesAsync`
after every operation.

Do not expose EF query objects from repository interfaces.

## Unit of Work Implementation

The Unit of Work normally represents the Application-facing commit boundary.

It should not act as a container exposing every repository. Repositories are
injected independently and participate in the same Unit of Work by sharing the
same scoped `AppDbContext`.

```csharp
internal sealed class UnitOfWork(AppDbContext db)
    : IUnitOfWork
{
    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return db.SaveChangesAsync(cancellationToken);
    }
}
```

Repositories participating in the same application operation must use the same
scoped `AppDbContext`.

## Generic Repository

A generic repository may be used internally in Infrastructure to remove genuine
implementation duplication.

```csharp
internal abstract class Repository<TEntity>(AppDbContext db)
    where TEntity : class
{
    protected DbSet<TEntity> Set => db.Set<TEntity>();
}
```

Do not expose `IGenericRepository<T>` to Application.

Do not force complex EF queries through a generic CRUD abstraction.
Concrete repositories may use the full EF Core API internally.

## Read Queries and Projections

For read-only queries that need only selected data, prefer database-side
projection.

```csharp
public Task<OrderSummary?> GetSummaryAsync(
    Guid id,
    CancellationToken cancellationToken)
{
    return db.Orders
        .Where(x => x.Id == id)
        .Select(x => new OrderSummary(
            x.Id,
            x.Total,
            x.CreatedAt))
        .FirstOrDefaultAsync(cancellationToken);
}
```

Projection is not mandatory when the use case genuinely needs the entity.

Avoid loading a complete aggregate only to return two scalar fields.

## Tracking

Use tracking intentionally.

Use normal tracking queries when loaded entities will be modified and committed
through the current Unit of Work.

Use `AsNoTracking()` for read-only entity queries where change tracking provides
no value.

Do not add `AsNoTracking()` mechanically to projections that already produce
non-entity DTOs.

## Primary-Key Lookups

`FindAsync` is appropriate when:

- querying by primary key;
- an entity instance is required;
- returning an already tracked entity is desirable.

```csharp
var order = await db.Orders.FindAsync(
    [orderId],
    cancellationToken);
```

Use LINQ when additional predicates, projection, related data, or query shaping
are required.

## Related Data

Prefer explicit query shape.

Use projection when only selected related data is needed.

Use `Include` / `ThenInclude` when an entity graph itself is required.

Avoid lazy loading by default because it hides database access and can cause
N+1 queries.

For large collection includes, consider split queries when appropriate and
verify the generated SQL and performance characteristics.

## Pagination

Apply filtering, ordering, and pagination in the database.

```csharp
var query = db.Orders
    .Where(x => x.CustomerId == customerId)
    .OrderByDescending(x => x.CreatedAt);

var totalCount = await query.CountAsync(cancellationToken);

var items = await query
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .Select(x => new OrderSummary(
        x.Id,
        x.Total,
        x.CreatedAt))
    .ToListAsync(cancellationToken);
```

Always use deterministic ordering for paged queries.

For very large or frequently traversed datasets, consider keyset pagination
instead of large `Skip` offsets when justified.

## Bulk Update and Delete

`ExecuteUpdateAsync` and `ExecuteDeleteAsync` can be useful for set-based
operations.

```csharp
await db.Orders
    .Where(x => x.Status == OrderStatus.Cancelled)
    .ExecuteDeleteAsync(cancellationToken);
```

These operations execute directly against the database and bypass normal change
tracking.

Do not mix them casually with tracked entities representing the same rows.
Understand their transaction and consistency implications before using them.

## Transactions

A single `SaveChangesAsync` is normally sufficient as the transaction boundary
for one application operation.

Do not create explicit transactions by default.

Use an explicit transaction when a concrete use case requires multiple database
operations or multiple `SaveChangesAsync` calls to succeed atomically.

Do not attempt to solve distributed transactions with EF transaction APIs.

## Compiled Queries

Normal EF Core LINQ is the default.

EF Core already caches query compilation based on query shape.

Use `EF.CompileQuery` or `EF.CompileAsyncQuery` only for a measured hot path
where profiling demonstrates that query compilation/cache lookup overhead is
material.

Do not introduce compiled queries merely because a query executes frequently.

## Interceptors

Use EF Core interceptors for infrastructure-level cross-cutting behavior when
they provide a clear benefit, for example auditing.

```csharp
internal sealed class AuditInterceptor(TimeProvider clock)
    : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;

        if (context is null)
            return ValueTask.FromResult(result);

        var now = clock.GetUtcNow();

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;

            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }

        return ValueTask.FromResult(result);
    }
}
```

Do not hide business behavior in persistence interceptors.

## Development Data Seeding

Development databases should contain representative seed data by default when
the database is empty.

Development seeding is an Infrastructure/bootstrap concern and may use
`AppDbContext` directly. It does not need to go through Application repositories
or `IUnitOfWork`.

Keep development seeding in a dedicated file, for example:

```text
Persistence/
  Seeding/
    DbSeeder.cs
```

Do not place substantial seed-data construction directly in `Program.cs`,
`AppDbContext`, entity configurations, or migration files.

### Environment

Automatic development seeding must run only in the `Development` environment.

Never automatically seed Production.

Tests must use their own explicit test-data setup and must not depend on the
development seeder.

### Empty Database Behavior

The development seeder assumes that the Development database schema has already
been created and migrated to the current application revision.

The seeder must not call `Database.Migrate()`, `EnsureCreated()`, or otherwise
create or upgrade the database schema.

For a new local database, the developer first applies the current migrations.
Application startup may then populate the empty schema with Development seed
data.

The development seeder initializes an empty Development database after its
schema has been brought to the current migration revision.

Use one or more representative root/anchor entities to determine whether
development data already exists.

```csharp
if (await db.Projects.AnyAsync(cancellationToken))
{
    return;
}
```

If development data already exists, the seeder should normally leave it
unchanged.

Do not use the development seeder as an automatic synchronization, repair, or
upgrade mechanism for an existing development database.

Do not delete, overwrite, or recreate existing development data automatically.

When a developer needs the latest seed dataset in an existing local database,
the database may be dropped and recreated explicitly.

### Seed Dataset

Seed data should cover the main meaningful states and relationships needed to
exercise the application during development.

Include representative examples such as, where applicable:

- newly created;
- active or in progress;
- completed;
- inactive or archived;
- entities with and without optional values;
- entities with and without optional relationships;
- representative status values;
- meaningful boundary states.

Do not attempt to generate every possible combination.

Prefer a small, readable, representative dataset over large amounts of random
data.

Use deterministic data where practical so that development behavior remains
predictable between database recreations.

Preserve all Domain invariants and valid relationships when constructing seed
entities.

When the required entity graph or scenarios are complex, the task requirements
should describe the desired dataset explicitly rather than having Claude invent
a large generic object graph.

### Seeder Example

```csharp
internal static class DbSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        if (await db.Projects.AnyAsync(cancellationToken))
        {
            return;
        }

        var projects = CreateProjects();

        await db.Projects.AddRangeAsync(
            projects,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyCollection<Project> CreateProjects()
    {
        // Create deterministic, representative development scenarios.
        throw new NotImplementedException();
    }
}

public static class DatabaseInitializationExtensions
{
    public static async Task SeedDevelopmentDataAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        await DbSeeder.SeedAsync(
            db,
            cancellationToken);
    }
}
```

The concrete seed entities and states must match the actual current Domain and
persistence model.

### Startup

Invoke the development seeder from application startup only when the host is
running in the `Development` environment.

Keep `DbSeeder` itself internal to Infrastructure. Expose a narrow Infrastructure
initialization extension rather than exposing the seeder implementation.

```csharp
if (app.Environment.IsDevelopment())
{
    await app.Services.SeedDevelopmentDataAsync();
}
```

The Infrastructure extension creates the required scope and delegates to the
internal `DbSeeder`.

Do not automatically run migrations as part of this rule.

Database migration strategy is separate from development data seeding.

### Keeping Seed Data Current

Seed data is part of the maintained application code.

Whenever a Domain or persistence change affects seeded entities, update the
development seeder in the same change.

This includes changes such as:

- new required properties;
- removed or renamed properties;
- changed relationships;
- new required related entities;
- changed enum/status values;
- new meaningful entity states;
- changed Domain invariants.

If an EF Core migration reflects a model change that affects the seeded entity
graph, review and update `DbSeeder` together with that migration.

A migration of an existing development database does not require the seeder to
patch already seeded rows automatically.

The goal is that recreating the development database at any revision produces
seed data that is valid and representative for that revision.

### Seeding Rules

- Seed development databases by default when they are empty.
- Automatically seed only in `Development`.
- Never automatically seed Production.
- Do not use the development seeder as test setup.
- Keep seed logic in a dedicated file.
- Do not overwrite existing development data automatically.
- Keep seed data synchronized with the current Domain and persistence model.
- Review seed data whenever a related migration is introduced.
- Prefer deterministic and readable data.
- Cover the main meaningful states and relationships.
- Preserve Domain invariants.
- Use `AppDbContext` directly inside Infrastructure seeding code.
- Let developers explicitly recreate local databases when a fresh dataset is needed.

## Migrations

Treat migrations as source code.

Create migrations from the Infrastructure project using the API project as the
startup project when required.

```bash
dotnet ef migrations add AddOrderIndex --project src/MyApp.Infrastructure --startup-project src/MyApp.Api
```

Review generated migrations before committing them.

When a migration changes entities or relationships represented by development
seed data, review and update `DbSeeder` in the same change.

After the change, recreating an empty development database must produce seed data
that is valid for the migrated schema and current Domain model.

Pay particular attention to:

- destructive schema changes;
- unexpected column recreation;
- data migrations;
- indexes;
- foreign keys and delete behavior.

Never automatically apply migrations to Production on application startup.

Production database migrations must be performed as an explicit deployment or
operational step using the project's chosen migration strategy.

For controlled production deployment, generated migration scripts are often
preferable.

## Query Performance

Start with clear normal LINQ.

Optimize after identifying a real problem.

Before introducing specialized optimization, inspect:

- number of database round trips;
- selected columns;
- N+1 behavior;
- indexes;
- generated SQL;
- query plan where necessary;
- amount of data materialized;
- tracking overhead.

Do not use `ValueTask`, compiled queries, raw SQL, caching, or manual pooling
merely as speculative optimization.

## Raw SQL

Use raw SQL only when EF Core cannot express the query effectively or measured
performance justifies it.

Always parameterize external values.

Prefer EF APIs that preserve parameterization rather than constructing SQL with
string concatenation.

## Anti-Patterns

Do not:

- expose `DbContext`, `DbSet<T>`, or `IQueryable<T>` from Infrastructure;
- make Application depend directly on EF Core;
- expose a generic CRUD repository contract to Application;
- call `SaveChangesAsync` independently from every repository operation;
- load all rows and filter in memory when the database can filter;
- use lazy loading by default;
- fire-and-forget EF asynchronous operations;
- introduce compiled queries without evidence;
- introduce raw SQL merely to appear more performant.
- automatically seed development data in Production;
- use the development seeder to silently patch or overwrite an existing local database;
- change the seeded entity model without updating the maintained seed dataset.

## Decision Guide

| Scenario | Default |
|---|---|
| Application persistence dependency | Specific repository interface |
| Commit application changes | `IUnitOfWork.SaveChangesAsync` |
| Repository implementation | EF Core inside Infrastructure |
| Read-only entity query | `AsNoTracking()` when useful |
| Read model | Database-side projection |
| Primary-key entity lookup | `FindAsync` when its semantics fit |
| Complex entity graph | Explicit `Include` / projection |
| Set-based mass update/delete | `ExecuteUpdateAsync` / `ExecuteDeleteAsync` when appropriate |
| Normal query | Ordinary LINQ |
| Measured extremely hot query | Consider compiled query |
| Multiple commits requiring atomicity | Explicit transaction |
| Production schema change | Reviewed migration / controlled deployment |
| Empty development database | Run maintained development seeder |
| Existing development database | Leave seed data unchanged by default |
| Model/migration affects seeded entities | Update `DbSeeder` in the same change |
| Need fresh/current local seed dataset | Developer explicitly recreates the local database |
| Test data | Use dedicated test setup, not development seeding |
| Production | Never run development seeding automatically |