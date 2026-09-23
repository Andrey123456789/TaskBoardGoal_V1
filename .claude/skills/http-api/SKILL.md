---
name: http-api
description: >
  HTTP API semantics for ASP.NET Core Controllers. Covers HTTP status codes,
  REST-style response semantics, Created/Accepted/NoContent responses,
  validation and conflict responses, authentication/authorization responses,
  conditional requests, ETags, idempotency, rate limiting, upstream failures,
  ProblemDetails, Location and Retry-After headers.
  Use when designing or reviewing Controllers, API responses, status codes,
  ActionResult results, REST endpoints, idempotency, ETags, conditional requests,
  or HTTP error mapping.
---

# HTTP API Semantics

## Core Principles

1. HTTP status codes belong to the API layer.
2. Choose a status code based on the actual HTTP semantics of the outcome.
3. Application and Domain layers must not return ASP.NET Core status codes or
   `IActionResult`.
4. Use `ProblemDetails` / `ValidationProblemDetails` for structured HTTP errors.
5. Keep status-code behavior consistent across similar endpoints.
6. Do not return `200 OK` for every outcome merely because the request reached
   the application successfully.

## Successful Responses

### 200 OK

Use `200 OK` when the operation completed successfully and a response
representation is returned.

Typical cases:

- successful `GET`;
- successful `PUT` or `PATCH` returning the updated representation;
- `POST` that performs an operation but does not create a new resource;
- successful query returning data.

```csharp
return Ok(response);
```

An empty collection is still a successful collection result:

```http
200 OK

[]
```

Do not return `404` merely because a collection contains zero items.

Normally do not return `204` for an empty collection. `200` with `[]` preserves
the resource representation and is clearer for clients.

### 201 Created

Use `201 Created` when the request creates a new resource.

Include a `Location` header identifying the created resource when a stable
resource URI exists.

```csharp
return CreatedAtAction(
    nameof(GetById),
    new { id = created.Id },
    created);
```

`PUT` may also return `201` when the request creates a resource at a previously
non-existing target URI.

Do not return `201` merely because a database row was inserted internally.
The important question is whether the HTTP operation created a new API resource.

### 202 Accepted

Use `202 Accepted` when processing has been accepted but is not complete when
the response is sent.

Typical examples:

- long-running operation;
- background job;
- asynchronous import;
- queued processing.

When practical, return an operation identifier or a `Location` pointing to a
resource where the client can observe progress.

```csharp
return AcceptedAtAction(
    nameof(GetOperation),
    new { id = operationId },
    response);
```

Do not return `202` for work that has already completed synchronously.

### 204 No Content

Use `204 No Content` when the operation completed successfully and no response
body is needed.

Common cases:

- successful `DELETE`;
- successful update where returning the updated representation provides no value.

```csharp
return NoContent();
```

A `204` response must not contain a response body.

Choose consistently whether repeated `DELETE` of an already absent resource
returns:

- `204` because the desired final state already holds; or
- `404` because the addressed resource does not exist.

Either convention can be valid. Do not mix them arbitrarily between equivalent
resources.

## Conditional and Cache Responses

### 304 Not Modified

Use `304 Not Modified` only for a conditional request when the client's cached
representation is still valid.

Typical mechanisms include:

- `If-None-Match` / ETag;
- `If-Modified-Since`.

A `304` response does not contain the normal resource representation.

Do not use `304` as a generic "nothing changed" business response.

## Client Request Errors

### 400 Bad Request

Use `400 Bad Request` when the HTTP request itself is invalid.

Typical examples:

- malformed JSON;
- invalid request shape;
- model-binding failure;
- invalid formatting;
- missing required transport-level input;
- request cannot be interpreted according to the API contract.

With `[ApiController]`, many binding and validation failures are handled
automatically.

Do not use `400` for authentication failure.

### 401 Unauthorized

Despite the name, `401` means authentication is missing or failed.

Examples:

- no required credentials;
- invalid token;
- expired token;
- authentication challenge required.

Authentication middleware should normally produce this response.

Do not return `403` merely because a user has not authenticated.

### 403 Forbidden

Use `403 Forbidden` when authentication succeeded but the authenticated
principal is not allowed to perform the operation.

Examples:

- missing required permission;
- insufficient role;
- resource operation prohibited by authorization policy.

The client generally cannot fix `403` merely by resending the same credentials.

For APIs that deliberately hide the existence of protected resources, a project
may return `404` instead of `403`. This must be a deliberate and consistent
security policy.

### 404 Not Found

Use `404 Not Found` when the addressed resource does not exist or when the API
deliberately hides its existence.

For collection queries:

```http
GET /api/orders?customerId=...
```

zero matches normally produce:

```http
200 OK
[]
```

not `404`.

### 406 Not Acceptable

Use `406 Not Acceptable` when the server cannot produce a representation
acceptable according to the client's content negotiation requirements.

This concerns the response representation.

Do not confuse it with `415 Unsupported Media Type`.

### 409 Conflict

Use `409 Conflict` when the request is valid but conflicts with the current
state of the resource or system.

Typical cases include:

- duplicate resource where uniqueness is part of the resource state;
- state transition incompatible with the current state;
- conflicting concurrent operation;
- an idempotency key reused with a different request payload.

Example:

```text
Order already completed
    + request asks to cancel it
    → 409 Conflict
```

Do not use `409` for malformed request syntax.

### 412 Precondition Failed

Use `412 Precondition Failed` when the client supplied a conditional precondition
and that precondition is false.

A common optimistic-concurrency example:

```http
If-Match: "version-7"
```

but the current resource version is `"version-8"`.

That is normally `412`, not a generic `409`, because an explicit HTTP
precondition failed.

### 415 Unsupported Media Type

Use `415 Unsupported Media Type` when the server does not support the request
representation.

Example:

```http
Content-Type: application/xml
```

when the endpoint accepts only JSON.

`415` concerns the request body format.

`406` concerns the response formats the client is willing to accept.

### 422 Unprocessable Content

Use `422 Unprocessable Content` when the request is syntactically and
structurally valid but cannot be processed because its semantic or business
content is invalid.

Examples:

- valid request shape but impossible business values;
- business rule rejection that is not a state conflict;
- semantically invalid combination of otherwise valid fields.

Example:

```text
startDate > endDate
```

may be `422` when both values are syntactically valid but semantically
incompatible.

Some APIs deliberately use `400` for both transport validation and semantic
validation. Follow an established project convention when one exists.

For new APIs that intentionally distinguish the two:

```text
400 → invalid HTTP/request contract
422 → structurally valid request, invalid semantics/business input
```

### 428 Precondition Required

Use `428 Precondition Required` when the API contract requires a request
precondition but the client did not provide one.

Typical example:

```http
PUT /api/orders/123
```

requires:

```http
If-Match: "..."
```

but the header is absent.

Then:

```text
missing required If-Match → 428
provided but stale If-Match → 412
```

Use `428` for other mandatory preconditions only when the API contract explicitly
defines them as such.

### 429 Too Many Requests

Use `429 Too Many Requests` when rate limiting rejects the request.

When the server knows when another attempt is appropriate, include
`Retry-After`.

Do not represent application-level validation failure as rate limiting.

## Server and Dependency Failures

### 500 Internal Server Error

Use `500 Internal Server Error` for unexpected failure inside the application
when a more specific controlled outcome is not available.

Do not expose:

- stack traces;
- SQL;
- internal paths;
- credentials;
- exception implementation details.

Use centralized exception handling and `ProblemDetails`.

### 502 Bad Gateway

Use `502 Bad Gateway` when this API is acting as an intermediary and receives an
invalid or unusable response from an upstream service.

Do not mechanically convert every dependency failure into `502`.

### 503 Service Unavailable

Use `503 Service Unavailable` when the service is temporarily unable to handle
the request.

Examples may include:

- intentional maintenance;
- critical dependency unavailable and this operation cannot function;
- temporary overload when the service chooses `503` semantics.

Include `Retry-After` when the server can provide a meaningful retry time.

### 504 Gateway Timeout

Use `504 Gateway Timeout` when the service is acting as an intermediary and an
upstream service does not respond within the required time.

Do not return `504` for every internal `TaskCanceledException`.

Distinguish:

- client cancelled the incoming request;
- our own operation timed out;
- an upstream dependency timed out.

The correct external response depends on which occurred and what role this API
plays.

## Idempotency

HTTP method semantics matter when designing retries and duplicate handling.

Typically:

- `GET` is safe and idempotent;
- `PUT` is idempotent;
- `DELETE` is idempotent in intended state semantics;
- `POST` is generally not idempotent.

For operations where a repeated `POST` must not create duplicate effects,
consider an idempotency key.

```http
Idempotency-Key: ...
```

The server should associate the key with the request/result according to the API
contract.

If the same key is reused with a materially different request payload, `409
Conflict` is a reasonable response.

Do not claim idempotency merely because duplicate requests are unlikely.

## Optimistic Concurrency

When HTTP-level optimistic concurrency is part of the API contract, prefer
standard conditional request semantics.

Typical flow:

```text
GET
→ ETag: "v7"

PUT/PATCH
→ If-Match: "v7"

current is still v7
→ update succeeds

current changed to v8
→ 412 Precondition Failed

If-Match required but absent
→ 428 Precondition Required
```

Do not replace well-defined HTTP precondition semantics with arbitrary custom
status codes.

## Controller Responsibility

Controllers translate Application outcomes into HTTP outcomes.

```csharp
[HttpPost]
public async Task<ActionResult<OrderResponse>> Create(
    CreateOrderRequest request,
    CancellationToken cancellationToken)
{
    var result = await orders.CreateAsync(
        request,
        cancellationToken);

    return CreatedAtAction(
        nameof(GetById),
        new { id = result.Id },
        result);
}
```

Application should express meaningful outcomes without depending on
`IActionResult`, `ProblemDetails`, or HTTP status codes.

## ProblemDetails

Use `ProblemDetails` for structured HTTP errors.

Use `ValidationProblemDetails` for request validation failures when appropriate.

Machine-readable clients should rely on stable error codes or contract fields,
not arbitrary human-readable exception messages.

## Decision Guide

| Situation | Default response |
|---|---|
| Successful query with representation | `200` |
| Empty collection | `200` + `[]` |
| Resource created | `201` + `Location` when available |
| Processing accepted but incomplete | `202` |
| Successful operation with no body | `204` |
| Conditional cache hit | `304` |
| Malformed/contract-invalid request | `400` |
| Missing/invalid authentication | `401` |
| Authenticated but forbidden | `403` |
| Resource absent | `404` |
| Cannot produce acceptable response representation | `406` |
| Current-state conflict | `409` |
| Supplied precondition failed | `412` |
| Unsupported request content type | `415` |
| Valid request structure but invalid semantics | `422` |
| Required precondition absent | `428` |
| Rate limited | `429` + `Retry-After` when known |
| Unexpected internal failure | `500` |
| Invalid/failed upstream response while acting as intermediary | `502` |
| Temporarily unavailable | `503` |
| Upstream timeout while acting as intermediary | `504` |