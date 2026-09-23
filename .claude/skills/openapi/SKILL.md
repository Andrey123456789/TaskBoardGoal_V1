---
name: openapi
description: >
  OpenAPI documentation for ASP.NET Core Controller APIs using built-in
  Microsoft.AspNetCore.OpenApi support. Covers AddOpenApi, MapOpenApi,
  controller response metadata, ProblemDetails, security schemes,
  transformers, descriptions, XML documentation, and client-generation
  considerations. Use when configuring or reviewing OpenAPI/API documentation.
---

# OpenAPI

## Core Principles

1. OpenAPI documents the actual API contract; it does not define runtime behavior.
2. Controller metadata must agree with the responses the action can actually return.
3. Follow the `http-api` skill when deciding status-code semantics.
4. Follow the `error-handling` skill for ProblemDetails/error behavior.
5. Prefer built-in ASP.NET Core OpenAPI support for new projects unless the
   project has deliberately chosen another OpenAPI implementation.
6. Do not add response codes merely to make documentation look comprehensive.

## Basic Setup

```csharp
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
```

Serving the runtime OpenAPI document only in Development is a reasonable
default. Expose it in other environments only when deployment requirements
justify doing so.

## Controller Response Metadata

Document important response types explicitly.

```csharp
[ApiController]
[Route("api/orders")]
public sealed class OrdersController(
    IOrderService orders)
    : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderResponse>(
        StatusCodes.Status200OK,
        Description = "Order returned successfully.")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        Description = "The order does not exist.")]
    public async Task<ActionResult<OrderResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(id, cancellationToken);

        if (order is null)
            return NotFound();

        return Ok(order);
    }
}
```

Do not document `201` when the action actually returns `200`.

Do not document `204` when the action returns a response body.

## Creation Responses

For creation endpoints, document `201` when a new API resource is created.

```csharp
[HttpPost]
[ProducesResponseType<OrderResponse>(
    StatusCodes.Status201Created)]
[ProducesResponseType<ValidationProblemDetails>(
    StatusCodes.Status400BadRequest)]
public async Task<ActionResult<OrderResponse>> Create(
    CreateOrderRequest request,
    CancellationToken cancellationToken)
{
    var created = await orders.CreateAsync(
        request,
        cancellationToken);

    return CreatedAtAction(
        nameof(GetById),
        new { id = created.Id },
        created);
}
```

The runtime response and OpenAPI metadata must remain consistent.

## Error Responses

Use `ProblemDetails` or `ValidationProblemDetails` as the documented error
contracts when those are the runtime contracts.

Do not document raw exception types.

Do not expose internal exception details in OpenAPI examples or descriptions.

For detailed status-code selection use the `http-api` skill.

## Authentication Metadata

When the API uses Bearer authentication, document the corresponding OpenAPI
security scheme.

Do not mark anonymous endpoints as requiring authentication.

Do not add a Bearer security scheme merely because JWT packages are installed;
the OpenAPI document should reflect the configured runtime authentication model.

Use an OpenAPI document/operation transformer when global or policy-derived
security metadata cannot be expressed accurately through normal controller
metadata.

## Document and Operation Transformers

Use transformers for concerns that genuinely apply across the generated
document, for example:

- document title/version/description;
- security schemes;
- shared response metadata;
- schema customization;
- environment-specific document changes.

Keep transformers focused.

Do not hide endpoint-specific API semantics inside a large global transformer.

## Document Information

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Orders API";
        document.Info.Version = "v1";
        document.Info.Description = "API for managing orders.";

        return Task.CompletedTask;
    });
});
```

## Descriptions

Describe behavior that is useful to API consumers.

Good descriptions explain:

- important semantics;
- preconditions;
- idempotency requirements;
- pagination behavior;
- meaningful status outcomes.

Avoid descriptions that merely repeat the method name.

## XML Documentation

XML comments may be useful for public API contracts when the project uses them.

Do not add XML comments to every internal implementation type solely for
OpenAPI generation.

Prefer API-consumer-facing documentation over implementation commentary.

## Multiple Documents

Use multiple OpenAPI documents only when there is a real separation such as:

- public vs internal API;
- substantially different API surfaces;
- version-specific documents.

Do not create multiple documents merely because the framework supports them.

API versioning strategy belongs to the `api-versioning` skill.

## Generated Clients

When the OpenAPI document is used to generate Angular or other clients:

- treat breaking schema changes seriously;
- keep response metadata accurate;
- avoid unstable anonymous response shapes;
- use explicit request/response contracts;
- review generated-client changes when the API contract changes.

An OpenAPI document that differs from runtime behavior is worse than incomplete
documentation.

## ProblemDetails and Client Generation

Keep error contracts stable enough for clients to understand them.

When clients need machine-readable failure categories, expose stable error codes
or structured fields rather than requiring clients to parse `title` or `detail`.

## Anti-Patterns

Avoid:

- Minimal API examples in a Controller-based project unless explicitly requested;
- `ISender`, command handlers, or MediatR assumptions;
- stale `[ProducesResponseType]` declarations;
- documenting impossible response codes;
- exposing OpenAPI publicly without a reason;
- using anonymous objects as important long-lived API contracts;
- treating OpenAPI as a substitute for integration tests;
- globally applying authentication metadata to genuinely anonymous operations.

## Verification

When API behavior changes:

1. verify the Controller behavior;
2. verify relevant integration tests;
3. verify response metadata;
4. inspect the generated OpenAPI document when the contract change is meaningful;
5. review generated clients when the project generates them from OpenAPI.