---
name: resilience
description: >
  Resilience guidance for .NET external dependencies using
  Microsoft.Extensions.Resilience and Microsoft.Extensions.Http.Resilience.
  Covers timeouts, retries, circuit breakers, fallback, hedging, rate limiting,
  idempotency, Retry-After, telemetry, and provider-specific retry behavior.
  Use when designing or reviewing transient-failure handling.
---

# Resilience

## Core Principles

1. Resilience policies must match failure and operation semantics.
2. A retry is safe only when repeating the operation is safe.
3. External I/O should have a bounded execution time.
4. Circuit breakers are useful when repeated dependency failures would otherwise
   consume resources or amplify an outage.
5. Fallback is valid only when the fallback result is semantically acceptable.
6. Do not add resilience mechanisms merely because they are available.
7. Observe retries/timeouts/circuit behavior when they materially affect operations.

## Package Choice

For new .NET applications, prefer the modern Microsoft resilience integration:

- use `Microsoft.Extensions.Http.Resilience` for `HttpClient` resilience;
- use `Microsoft.Extensions.Resilience` for general resilience pipelines when needed.

Do not add `Microsoft.Extensions.Http.Polly` or `Polly.Extensions.Http`; these
integration packages are deprecated.

The modern Microsoft resilience packages are implemented on top of Polly v8,
but application guidance should normally use the Microsoft.Extensions
resilience APIs rather than introducing direct Polly configuration.

Do not add a direct Polly package reference or configure Polly APIs in
application code by default.

Treat direct Polly usage as an explicit exception. Use it only when a concrete
requirement cannot be expressed adequately through `Microsoft.Extensions.Resilience`
or `Microsoft.Extensions.Http.Resilience`, and document why the lower-level API
is required.

## HTTP Resilience

For `HttpClient`, `Microsoft.Extensions.Http.Resilience` provides standard
resilience integration.

```csharp
services.AddHttpClient<CatalogClient>(client =>
{
    client.BaseAddress = catalogUri;
})
.AddStandardResilienceHandler();
```

The standard handler is useful when its behavior fits the remote API.

It is not mandatory for every HTTP client.

Review retry behavior carefully for state-changing requests.

Detailed HttpClient lifetime/configuration guidance belongs to the
`httpclient-factory` skill.

## Timeouts

A dependency call should not wait indefinitely.

Distinguish:

```text
attempt timeout
```

from:

```text
total operation timeout
```

especially when retries are enabled.

Choose values from actual latency requirements rather than copying arbitrary
numbers from an example.

## Retry

Retry only failures that are plausibly transient.

Possible examples:

- selected network failures;
- temporary service unavailability;
- rate limiting when retry is allowed;
- transient provider-specific database failures.

Do not retry:

- validation errors;
- authentication/authorization failures;
- permanent `404` outcomes;
- deterministic business rejection;
- non-idempotent operations without a duplicate-prevention strategy.

## Retry-After

When a remote endpoint returns `Retry-After`, prefer respecting the server's
retry guidance when the client policy supports it.

Do not hammer a rate-limited dependency using an independent aggressive retry
schedule.

## Idempotency

Before retrying a write ask:

```text
If the first attempt succeeded remotely but the response was lost,
what happens when we send the operation again?
```

If the answer is "duplicate side effect", automatic retry is unsafe.

Use an API-supported idempotency mechanism when appropriate.

Do not invent a client-side `Idempotency-Key` unless the remote service actually
supports that contract.

## Circuit Breaker

Use a circuit breaker when repeated calls to a failing dependency would create
additional load or latency.

Do not add a circuit breaker to a dependency that has no meaningful repeated
failure pattern.

A circuit breaker should have operational visibility when it is important in
production.

## Fallback

Fallback must preserve acceptable business semantics.

Valid examples may include:

- stale cached read data where staleness is explicitly acceptable;
- optional enrichment omitted when its service is unavailable.

Bad fallback:

```text
payment provider unavailable
→ pretend payment succeeded
```

Do not hide correctness failures behind fallback values.

## Hedging

Hedging issues parallel or delayed duplicate attempts.

Use it only for operations that are safe to execute multiple times, typically
idempotent reads against interchangeable endpoints.

Do not hedge state-changing operations by default.

## Database Resilience

Do not wrap normal EF Core operations in a generic application-level retry or
resilience pipeline by default.

When a database provider supports transient retry through EF Core execution
strategies/provider configuration, prefer that mechanism.

Database transaction/retry interaction must be handled according to the
provider's EF Core guidance.

Do not assume an HTTP retry policy is appropriate for database operations.

## Message Publishing

Retrying message publication requires understanding the broker and delivery
semantics.

A retry may produce duplicate delivery.

Use idempotent consumers, broker-supported deduplication, outbox patterns, or
other mechanisms when the business requirement needs them.

Do not claim exactly-once delivery merely because a retry policy exists.

## Inbound Rate Limiting

ASP.NET Core rate limiting protects this API from excessive incoming traffic.

It is conceptually different from outbound retry/resilience.

When a request is rejected because of rate limiting:

```text
429 Too Many Requests
```

is the normal HTTP response.

Include `Retry-After` when a useful retry delay is known.

Detailed HTTP semantics belong to the `http-api` skill.

## Cancellation

Cancellation from the caller is not a transient failure that should be retried.

Propagate cancellation through resilience pipelines.

Do not convert client cancellation into repeated outbound attempts.

## Telemetry

Observe resilience behavior when it matters operationally.

Useful signals include:

- retry counts;
- timeout frequency;
- circuit state;
- rate-limit rejection;
- dependency latency.

Do not require OpenTelemetry solely because resilience pipelines or handlers
are present.

Use the telemetry stack adopted by the project.

## Anti-Patterns

Avoid:

- retrying every exception;
- retrying every HTTP status;
- retrying unsafe writes without idempotency analysis;
- retrying client cancellation;
- circuit breakers added without a failure scenario;
- fallback values that conceal incorrect business results;
- hedging writes;
- generic retry/resilience wrappers around EF Core without understanding
  provider behavior;
- stacked resilience handlers with overlapping strategies;
- unbounded retry/timeout combinations.

## Decision Guide

| Scenario | Default consideration |
|---|---|
| External I/O | Bound execution time |
| Safe transient HTTP failure | Consider retry |
| Unsafe write | No retry unless duplicate-safe |
| `429` from dependency | Respect `Retry-After` where appropriate |
| Repeated dependency outage | Consider circuit breaker |
| Optional data unavailable | Consider explicit fallback |
| Latency-sensitive idempotent read | Consider hedging after measurement |
| EF transient faults | Prefer provider/EF execution strategy |
| Incoming request rate limiting | ASP.NET Core rate limiter |