---
name: error-handling
description: >
  Error handling and validation guidance for ASP.NET Core applications.
  Covers ProblemDetails, IExceptionHandler, controller error responses,
  expected application outcomes, validation boundaries, safe error logging,
  cancellation, and optional Result-style patterns.
  Use when implementing API errors, exception handling, validation,
  application failure contracts, or ProblemDetails responses.
---

# Error Handling

## Core Principles

1. **Use predictable API error contracts** — HTTP API errors should normally use
   ASP.NET Core `ProblemDetails` / `ValidationProblemDetails`.
2. **Handle unexpected failures centrally** — avoid repetitive catch/log/rethrow
   code in controllers and Application services.
3. **Do not use exceptions casually for normal control flow** — represent common
   expected outcomes explicitly when that improves clarity.
4. **Do not force a Result pattern** — adopt `Result<T>` or typed application
   outcomes only when the project deliberately chooses that model.
5. **Validate at the correct boundary** — transport validation, application
   rules, and domain invariants are different concerns.
6. **Do not expose internal failure details** to API clients.

## Unexpected Exceptions

Unexpected exceptions should normally propagate to a global API exception
handler.

Examples include:

- programming defects;
- unexpected database failures;
- unexpected external-service failures;
- violated assumptions that the application cannot handle locally.

Do not add broad `try/catch` blocks only to log and rethrow the same exception.

## Global Exception Handler

Use ASP.NET Core `IExceptionHandler` for centralized handling.

Treat client-aborted requests separately from unexpected application failures.
A cancellation caused by `HttpContext.RequestAborted` should not be logged as an
unhandled application error or translated into a generic `500` response.

```csharp
internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException &&
            httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode =
                StatusCodes.Status499ClientClosedRequest;

            return true;
        }

        logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        await Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "An unexpected error occurred")
            .ExecuteAsync(httpContext);

        return true;
    }
}
```

Register it in API composition:

```csharp
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

app.UseExceptionHandler();
```

Do not return stack traces, SQL details, connection strings, internal file paths,
or other sensitive exception details to clients.

Development diagnostics may expose additional information through development
tooling, but public API contracts should remain safe.

## Expected Application Outcomes

Expected outcomes are conditions the application understands and can represent
without treating them as system failures.

Examples include:

- requested entity does not exist;
- requested transition is not allowed;
- duplicate resource;
- optimistic concurrency conflict;
- business operation rejected by a known rule.

Choose the simplest representation that fits the use case.

For a simple query, `null` may be sufficient:

```csharp
public Task<OrderDto?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken);
```

For a command with multiple meaningful outcomes, an explicit typed outcome may
be clearer.

Do not introduce a generic `Result<T>` library merely because a method can fail.

If the project adopts a Result pattern, use it consistently and define typed
errors rather than passing arbitrary strings through every layer.

## Controller Mapping

Controllers translate Application outcomes into HTTP semantics.

```csharp
[ApiController]
[Route("api/orders")]
public sealed class OrdersController(
    IOrderService orders)
    : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(
            id,
            cancellationToken);

        if (order is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Order not found");
        }

        return Ok(order);
    }
}
```

Do not make Application return `IActionResult`, `ProblemDetails`, HTTP status
codes, or other ASP.NET Core transport types.

## ProblemDetails

Use `ProblemDetails` for non-validation HTTP failures.

Use `ValidationProblemDetails` for request validation failures.

Keep error responses stable enough for clients to consume.

A useful error contract may contain:

- HTTP status;
- stable machine-readable error code when clients need one;
- human-readable title;
- safe detail;
- correlation or trace identifier when appropriate.

Do not make human-readable exception messages the machine-readable API
contract.

## Request Validation

With `[ApiController]`, ASP.NET Core handles transport-level concerns such as
model-binding failures and can produce validation problem responses.

FluentValidation is the project's standard validation library for explicit
request and use-case validation.

Use FluentValidation when validation rules need to be expressed for inputs such
as:

- required business/application input;
- string or collection constraints;
- ranges;
- cross-property validation;
- conditional validation;
- other request/use-case rules that are clearer as explicit validators.

Keep validation at the layer that owns the rule.

For example:

```text
API transport/request-contract concern
    -> API boundary validation where appropriate

Application use-case input rule
    -> Application validator

Domain invariant
    -> Domain model
```

Do not move Domain invariants into FluentValidation merely because the library
is available.

Do not duplicate the same rule mechanically in API, Application, and Domain.

Use FluentValidation.DependencyInjectionExtensions for validator registration
where appropriate.

Do not introduce deprecated or legacy ASP.NET automatic-validation integration
packages merely to connect FluentValidation to the request pipeline.

Follow the project's established validation invocation pattern rather than
adding a second competing mechanism.	

## Application and Domain Validation

API validation does not replace business rules.

A request can be syntactically valid but still violate an application or domain
rule.

Keep meaningful domain invariants in Domain when they naturally belong there.

Keep orchestration/use-case rules in Application when they depend on the
operation being performed.

Do not duplicate the same rule independently in several layers unless each layer
has a distinct reason to enforce it.

## Exceptions for Domain or Application Rules

Do not establish custom business exceptions as the default architecture.

A project may deliberately use typed exceptions for selected cases, but this
should be a consistent project-level decision.

Do not throw and catch exceptions merely to jump between normal branches of a
use case.

## External Services

Catch an external-service exception locally only when the current layer can do
something meaningful with it, for example:

- translate it into an Application-level outcome;
- add useful context;
- perform a defined fallback;
- apply an appropriate retry policy at the infrastructure boundary.

Otherwise allow it to propagate to the centralized failure handling path.

Do not swallow external-service failures.

## Error Logging

Unexpected exceptions should normally be logged once at the boundary that
handles them.

Do not log the same exception repeatedly in Repository, Application,
Controller, and the global exception handler.

Expected outcomes such as `NotFound`, validation failures, or rejected business
operations normally should not be logged as application errors.

Follow the project's logging guidance for log levels, structured logging,
message templates, correlation, and sensitive-data handling.

## Cancellation

Propagate `CancellationToken` through asynchronous operations.

A request cancelled by the client is not the same as an internal server error.

Do not deliberately convert normal request cancellation into a generic 500
response or log it as an application failure unless there is a concrete reason.

## Catching Exceptions

Catch an exception when at least one of these is true:

- you can recover;
- you can translate it into a meaningful abstraction-level outcome;
- you must perform cleanup that cannot be expressed otherwise;
- you need to add useful context and preserve the original exception.

Do not catch `Exception` simply to continue execution.

Do not write:

```csharp
try
{
    await service.ProcessAsync(cancellationToken);
}
catch (Exception)
{
}
```

Do not write catch/log/rethrow boilerplate at every layer:

```csharp
catch (Exception ex)
{
    logger.LogError(ex, "Failed");
    throw;
}
```

when the global handler already owns that responsibility.

## Result Pattern

`Result<T>` is optional.

Consider it when:

- an operation has several expected failure modes;
- callers benefit from explicit typed outcomes;
- exception-based control flow would otherwise be common;
- the project is willing to use the pattern consistently.

Avoid it when:

- `null`, `bool`, or a simple typed response communicates the outcome clearly;
- it creates wrapper noise around straightforward CRUD;
- it causes every method in the codebase to return `Result<T>` without a real
  need.

If introduced, prefer typed errors:

```csharp
public sealed record AppError(
    string Code,
    string Message);
```

Do not make arbitrary string arrays the application's error type.

## HTTP Status Mapping

HTTP status codes belong to the API layer.

Use the `http-api` skill as the canonical guidance for HTTP response semantics,
including success responses, validation, authentication/authorization,
conflicts, preconditions, rate limiting, and upstream failures.

This skill owns exception and failure handling. It should not duplicate the full
HTTP status-code policy.

## Anti-Patterns

Avoid:

- returning raw exception messages to clients;
- returning inconsistent anonymous error objects;
- making Application depend on HTTP status codes;
- forcing every method to return `Result<T>`;
- throwing exceptions for every normal negative branch;
- swallowing exceptions;
- catch/log/rethrow at every layer;
- logging the same failure repeatedly;
- leaking sensitive details through `ProblemDetails`;
- putting business rules only in request validators;
- coupling validation to Minimal API endpoint filters when Controllers are the
  project's HTTP model.

## Decision Guide

| Scenario | Default |
|---|---|
| Invalid HTTP input | Validation problem response |
| Simple query not found | Explicit Application outcome, often `null`; API maps to `404` |
| Multiple expected business outcomes | Consider typed outcome / Result |
| Unexpected exception | Global `IExceptionHandler` |
| API failure contract | `ProblemDetails` |
| Domain invariant | Domain enforcement |
| Transport validation | API/request validation |
| Recoverable external failure | Handle or translate at the appropriate boundary |
| Unrecoverable external failure | Propagate to centralized handling |
| Client/request cancellation | Propagate cancellation; do not treat as generic 500 |