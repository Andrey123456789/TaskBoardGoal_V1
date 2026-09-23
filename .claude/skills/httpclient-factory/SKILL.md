---
name: httpclient-factory
description: >
  Outbound HTTP client guidance for .NET using IHttpClientFactory. Covers named,
  typed and keyed clients, Infrastructure adapters, configuration, timeouts,
  DelegatingHandlers, cancellation, response mapping, resilience integration,
  and testing. Use when implementing or reviewing calls to external HTTP APIs.
---

# HttpClient Factory

## Core Principles

1. Do not create and dispose a new `HttpClient` for every application request.
2. Use `IHttpClientFactory` or an appropriately managed long-lived client.
3. Named, typed, and keyed clients are all valid; choose the simplest model for
   the consumer and lifetime.
4. Outbound API clients normally belong in Infrastructure.
5. Propagate `CancellationToken`.
6. Configure finite time limits for external calls.
7. Add retry/circuit-breaker behavior only when its semantics are appropriate.
8. Do not leak `HttpResponseMessage` or HTTP-specific transport concerns into
   Application contracts unless the Application explicitly models HTTP.

## Architectural Boundary

Application may define an abstraction:

```csharp
public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(
        PaymentRequest request,
        CancellationToken cancellationToken);
}
```

Infrastructure implements it with `HttpClient`.

```csharp
internal sealed class PaymentGateway(HttpClient httpClient)
    : IPaymentGateway
{
    ...
}
```

Application does not need to know the external service uses HTTP.

## Typed Clients

Typed clients are useful when one class owns communication with one remote API.

```csharp
services.AddHttpClient<PaymentGateway>(client =>
{
    client.BaseAddress = new Uri(configuration["Payments:BaseUrl"]!);
});
```

Do not capture a transient typed client inside a singleton without understanding
its lifetime implications.

## Named Clients

Named clients are useful when:

- a factory creates clients dynamically;
- several clients share one usage model;
- a singleton needs to create clients per operation.

```csharp
services.AddHttpClient("catalog", client =>
{
    client.BaseAddress = new Uri(configuration["Catalog:BaseUrl"]!);
});
```

## Keyed Clients

Keyed clients are useful when keyed DI makes consumption clearer.

Do not choose keyed clients automatically simply because the framework supports
them.

Prefer whichever model makes ownership and lifetime easiest to understand.

## Delegating Handlers

Use `DelegatingHandler` for genuine HTTP pipeline concerns such as:

- authorization header acquisition;
- correlation/context propagation;
- specialized request signing.

Do not hide business decisions inside handlers.

```csharp
internal sealed class AccessTokenHandler(
    IAccessTokenProvider tokens)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await tokens.GetAsync(cancellationToken);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
```

## Cancellation

Always propagate meaningful cancellation to outbound HTTP calls.

```csharp
await httpClient.SendAsync(
    request,
    cancellationToken);
```

Do not replace request cancellation with arbitrary `CancellationToken.None`
unless the operation intentionally must outlive the incoming request.

## Timeouts

External calls require a bounded execution time.

Choose timeouts according to the remote operation and the application's latency
budget.

When using resilience pipelines, understand the difference between:

- per-attempt timeout;
- total-operation timeout.

Avoid overlapping timeout mechanisms whose combined behavior is unclear.

## Response Mapping

Infrastructure should translate remote HTTP responses into the abstraction
consumed by Application.

Do not blindly use `EnsureSuccessStatusCode()` when specific remote statuses
have meaningful contract semantics.

Example:

```csharp
if (response.StatusCode == HttpStatusCode.NotFound)
{
    return null;
}

response.EnsureSuccessStatusCode();
```

For richer failure semantics, translate responses into explicit Infrastructure /
Application outcomes.

Do not simply expose every upstream status code to your own API.

## 429 and Retry-After

When a remote service returns `429 Too Many Requests`, respect `Retry-After`
when the selected retry policy supports it.

Do not retry indefinitely.

The outbound client's retry behavior and the inbound HTTP response your own API
returns are separate decisions.

## Resilience

Use the `resilience` skill when configuring retries, circuit breakers, hedging,
fallback, or resilience pipelines.

A timeout is normally required for external I/O.

Retry and circuit breaker are not mandatory for every external call.

Before enabling retry, determine:

- whether the failure is transient;
- whether the operation is safe to repeat;
- whether the remote API supports idempotency;
- whether retry may duplicate side effects.

## Unsafe HTTP Methods

POST/PATCH and other state-changing operations must not be retried automatically
unless their semantics make retry safe.

Possible mechanisms include:

- API-defined idempotency keys;
- naturally idempotent operation semantics;
- explicit deduplication.

Do not assume every PUT or DELETE implementation is operationally safe merely
because the HTTP method is defined as idempotent.

## Headers

Stable headers such as media types may be configured on the client.

Per-request values such as dynamic access tokens should normally be applied to
the specific request or through an appropriate handler.

Do not mutate shared default headers concurrently for request-specific values.

## Serialization

Prefer `System.Net.Http.Json` / `System.Text.Json` where sufficient.

Use explicit serialization options when the external API contract differs from
the application's defaults.

Do not reuse internal Domain entities as remote API contracts merely to avoid
mapping.

## Testing

For an outbound adapter, a controlled `HttpMessageHandler` can verify:

- request method/path;
- headers;
- serialized payload;
- response mapping;
- cancellation/failure behavior.

```csharp
var client = new HttpClient(testHandler)
{
    BaseAddress = new Uri("https://example.test/")
};
```

Using `new HttpClient(testHandler)` in a focused test is fine; the prohibition on
per-request construction concerns production lifetime management.

## Anti-Patterns

Avoid:

- creating/disposing `HttpClient` for every request;
- making typed/keyed/named clients mandatory universally;
- retries on unsafe operations without idempotency analysis;
- infinite or poorly bounded retries;
- leaking `HttpResponseMessage` across architectural boundaries;
- ignoring cancellation;
- dynamic auth tokens in shared `DefaultRequestHeaders`;
- mapping every upstream failure mechanically to the same status in your own API;
- adding resilience pipelines without understanding their behavior.