---
name: api-versioning
description: >
  API versioning guidance for ASP.NET Core Controller APIs. Covers when API
  versioning is actually needed, Asp.Versioning, URL/header/query strategies,
  breaking versus compatible changes, deprecation, OpenAPI integration, and
  version migration. Use when introducing or evolving multiple API contract
  versions or discussing backward compatibility.
---

# API Versioning

## Core Principles

1. Do not introduce API versioning merely because an API exists.
2. Add explicit versioning when contract stability and backward compatibility
   justify the additional complexity.
3. Once a published API version is relied on by external clients, avoid breaking
   that contract silently.
4. Breaking contract changes normally require a new API version.
5. Compatible additions normally remain in the current version.
6. Versioning belongs to the API contract, not Domain or Application.

## When Versioning Is Useful

Explicit API versioning is most useful when:

- external clients cannot be upgraded atomically with the server;
- multiple client generations must coexist;
- the API is public or consumed by independent teams;
- breaking contract changes are expected;
- a migration window between contracts is required.

For a small application where frontend and backend are deployed together,
explicit API versioning may provide little value.

Do not add `v1` automatically without considering whether the project actually
needs a long-lived versioning contract.

## Strategy

When explicit versioning is required, URL-segment versioning is a clear default
for many HTTP APIs:

```text
/api/v1/orders
/api/v2/orders
```

Benefits include:

- visible version in the URI;
- easy routing and troubleshooting;
- clear generated documentation.

Header and query-string versioning are valid alternatives when project
requirements justify them.

Use one strategy consistently.

## Asp.Versioning Setup

For Controller-based APIs, use the Controller integration of `Asp.Versioning`.

```csharp
builder.Services
    .AddApiVersioning(options =>
    {
        options.ReportApiVersions = true;
        options.ApiVersionReader =
            new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });
```

Do not add the package until the project has actually adopted API versioning.

## Controller Versions

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orders")]
public sealed class OrdersV1Controller(
    IOrderService orders)
    : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponseV1>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(
            id,
            cancellationToken);

        if (order is null)
            return NotFound();

        return Ok(OrderResponseV1.From(order));
    }
}
```

A second contract may expose another controller/version:

```csharp
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/orders")]
public sealed class OrdersV2Controller(
    IOrderService orders)
    : ControllerBase
{
}
```

Do not create separate Domain or persistence models merely because the API has
multiple representations.

Different API versions may map the same Application result into different HTTP
contracts.

## Breaking Changes

Changes that commonly justify a new API version include:

- removing a field clients may depend on;
- changing a field's meaning or type;
- changing required input;
- changing resource or workflow semantics incompatibly;
- removing an endpoint;
- changing previously documented HTTP behavior incompatibly.

Do not determine breaking compatibility solely from whether the code compiles.

## Compatible Changes

Changes that are often compatible include:

- adding an optional response field;
- adding a new endpoint;
- adding an optional query parameter with backward-compatible behavior;
- internal implementation changes that preserve the API contract.

Client-generation behavior should also be considered when OpenAPI clients are
used.

## Deprecation

Deprecation is a migration process, not merely an attribute.

When deprecating a version:

- mark it deprecated in versioning metadata;
- document the replacement version;
- provide a realistic migration period;
- communicate removal timing to consumers;
- keep the old version working during the promised support period.

Do not remove an externally consumed version without considering its published
support contract.

## OpenAPI

Generate separate or clearly grouped OpenAPI descriptions for supported versions
when clients need them.

Follow the `openapi` skill for documentation details.

The documented version must match runtime routing and Controller metadata.

## HTTP Semantics

Version changes do not override normal HTTP semantics.

Follow the `http-api` skill for status codes, conditional requests,
ProblemDetails, idempotency, and other HTTP behavior.

## Testing

When multiple versions coexist, test meaningful differences between them.

Useful cases include:

- version routing;
- version-specific response shape;
- deprecated version remains operational during support period;
- unsupported version behavior;
- shared Application behavior remains consistent where intended.

Do not duplicate identical tests merely because two versions use the same
Application behavior.

## Anti-Patterns

Avoid:

- adding versioning without a compatibility requirement;
- Minimal API version-group examples in a Controller-based project;
- duplicating Domain models per API version;
- changing an existing version's contract incompatibly;
- creating a new API version for every implementation change;
- silently removing deprecated versions;
- mixing URL, header, and query versioning without a concrete reason.