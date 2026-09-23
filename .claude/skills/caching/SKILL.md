---
name: caching
description: >
  Caching guidance for .NET applications. Covers when caching is appropriate,
  HybridCache, in-memory and distributed caching, output caching, cache keys,
  expiration, invalidation, consistency, and testing. Use when implementing,
  reviewing, or troubleshooting application or HTTP caching.
---

# Caching

## Core Principles

1. **Caching is optional** — Introduce it for a concrete performance or scalability need.
2. **Define consistency semantics first** — Know how stale data may become and how it is invalidated.
3. **Keep cache technology out of Domain**.
4. **Do not leak infrastructure cache APIs into Application without a deliberate reason**.
5. **Every cache entry needs a lifetime or another explicit invalidation strategy**.
6. **Measure before adding caching solely for performance**.

## Choosing the Cache Layer

### HTTP response caching

Use ASP.NET Core output caching when the entire HTTP response can safely be reused.

This belongs in the API layer.

### Application/data caching

When an Application use case genuinely needs cache-aware behavior, define an
appropriate abstraction in Application and implement it in Infrastructure.

Do not introduce an application-level cache abstraction if caching can remain an
internal Infrastructure concern.

### Infrastructure caching

Infrastructure adapters may cache expensive remote or persistence operations
when doing so preserves the contract exposed to Application.

## HybridCache

`HybridCache` is a strong option when the application benefits from:

- local in-memory caching;
- optional distributed L2 caching;
- stampede protection;
- common cache-aside behavior.

It is not mandatory for every project.

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddHybridCache();

    return services;
}
```

Add a distributed backend such as Redis only when deployment requirements justify it.

## Cache Abstraction Example

When Application must coordinate cache invalidation explicitly:

```csharp
public interface IProductCache
{
    Task<ProductSummary?> GetAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task SetAsync(
        ProductSummary product,
        CancellationToken cancellationToken);

    Task RemoveAsync(
        Guid id,
        CancellationToken cancellationToken);
}
```

Infrastructure may implement this contract using `HybridCache`.

Do not expose `HybridCache` itself from Application contracts.

## Cache Keys

Keys must uniquely represent the cached value.

Include all dimensions that affect the result, such as:

- entity identifier;
- tenant;
- user when genuinely user-specific;
- locale;
- relevant query/filter parameters.

Avoid global keys for user-specific or tenant-specific data.

Centralize key construction when duplicated key formats become difficult to maintain.

## Expiration

Choose expiration based on data semantics rather than arbitrary universal defaults.

Consider:

- how frequently the data changes;
- cost of regeneration;
- acceptable staleness;
- memory footprint;
- failure behavior when the source is unavailable.

Avoid unbounded cache entries.

## Invalidation

Invalidation should follow the consistency requirements of the use case.

Typical strategies include:

- remove affected key after a successful mutation;
- short TTL with tolerated staleness;
- tag/group invalidation;
- versioned keys.

Do not invalidate a cache before the underlying mutation is known to have succeeded
unless the workflow deliberately accepts that behavior.

## Output Caching

Use output caching for responses that are safe to reuse.

```csharp
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy(
        "ProductById",
        policy => policy
            .Expire(TimeSpan.FromMinutes(5))
            .SetVaryByRouteValue("id"));
});

app.UseOutputCache();
```

For Controllers, apply the project's selected output-cache policy through the
appropriate controller/action metadata.

Do not cache authenticated or user-specific HTTP responses unless the cache key
and policy correctly isolate users and authorization context.

## Mutations

Application Services should perform their normal repository/UoW workflow.

If cache invalidation is part of the required use-case semantics, coordinate it
through an appropriate abstraction.

Do not bypass repositories or `IUnitOfWork` merely to make cache invalidation convenient.

## Failure Behavior

Decide whether cache failure should:

- fall back to the source;
- fail the request;
- return stale data.

Do not silently turn a cache into a second source of truth.

## Testing

Test cache behavior when it affects observable correctness.

Examples:

- stale entries are invalidated after successful mutation;
- tenant/user keys are isolated;
- a cache miss falls back correctly;
- a cache outage follows the intended failure policy.

Do not unit-test framework cache implementation details.

## Anti-Patterns

Avoid:

- caching by default without a demonstrated reason;
- global keys for tenant/user-specific data;
- indefinitely cached mutable data without an invalidation strategy;
- placing cache technology in Domain;
- making Application depend directly on `HybridCache` without a deliberate architectural decision;
- caching EF tracked entities across Unit of Work boundaries;
- using cache as authoritative persistence;
- adding Redis merely because the application has caching.

## Decision Guide

| Need | Default consideration |
|---|---|
| Entire reusable HTTP response | ASP.NET Core output caching |
| Local application cache | In-memory or HybridCache |
| Multi-instance shared cache | Distributed cache / HybridCache L2 |
| Expensive repeated remote read | Cache in Infrastructure when contract permits |
| Application-controlled invalidation | Application abstraction + Infrastructure implementation |
| Unknown performance problem | Measure before adding caching |