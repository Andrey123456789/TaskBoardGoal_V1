---
name: logging
description: >
  General application logging guidance for .NET using ILogger. Covers structured
  message templates, log levels, scopes, correlation, exception logging,
  sensitive data, and logging boundaries. Use when adding, reviewing, or
  troubleshooting application logging. For Serilog-specific configuration,
  sinks, enrichers, and bootstrap setup use the serilog skill.
---

# Logging

## Core Principles

1. Use `ILogger<T>` in application code.
2. Prefer structured message templates over string interpolation.
3. Log information that helps diagnose or operate the system.
4. Avoid duplicate logging of the same failure across multiple layers.
5. Never treat logs as a safe place for secrets or sensitive payloads.
6. Choose log levels by operational meaning, not by how unusual an event feels.

## Structured Logging

```csharp
logger.LogInformation(
    "Order {OrderId} created for customer {CustomerId}",
    orderId,
    customerId);
```

Avoid:

```csharp
logger.LogInformation(
    $"Order {orderId} created for customer {customerId}");
```

Use stable property names so logs remain queryable.

## Log Levels

### Trace / Debug

Detailed diagnostic information primarily useful during troubleshooting.

Do not assume sensitive data is safe simply because the level is Debug or Trace.

### Information

Normal meaningful application events.

Examples:

- a background job completed;
- an important business operation completed;
- a deployment/runtime lifecycle event occurred.

Avoid logging every internal method call.

### Warning

An unexpected or degraded condition from which the application recovered.

Examples:

- fallback used;
- retry sequence exhausted but another path succeeded;
- optional dependency unavailable;
- unusual but handled state.

### Error

An operation failed and requires investigation or meaningful attention.

Expected negative business outcomes such as `NotFound` or validation rejection
are normally not application errors.

### Critical

The application or a major subsystem cannot continue safely.

## Exception Logging

Log an exception at the boundary that actually handles or terminates the failure.

Do not log the same exception as Error in Repository, Application Service,
Controller, and global exception handler.

When rethrowing without meaningful handling, normally do not add duplicate logs.

Follow the `error-handling` skill for exception-flow policy.

## Scopes

Use `ILogger.BeginScope` when several log entries need shared contextual data.

```csharp
using (logger.BeginScope(
    new Dictionary<string, object?>
    {
        ["OrderId"] = orderId
    }))
{
    await ProcessOrderAsync(orderId, cancellationToken);
}
```

Logging providers may enrich these scopes differently.

For provider-specific mechanisms such as Serilog `LogContext`, use the provider's
dedicated skill.

## Correlation

Prefer platform/runtime trace identifiers when they already provide adequate
request correlation.

Add an explicit correlation identifier only when an integration or operational
requirement needs one.

When a correlation identifier is accepted from an external client, validate or
normalize it before propagating it.

Propagate correlation/tracing context across outbound calls using the project's
chosen tracing or HTTP infrastructure.

## Sensitive Data

Never log credentials or authentication secrets.

Avoid logging:

- access/refresh tokens;
- passwords;
- private keys;
- authorization headers;
- full request/response bodies containing sensitive data.

Prefer stable technical identifiers over personal information.

Do not rely on log level as a privacy control.

## Payload Logging

Do not serialize arbitrary domain objects or request bodies into logs by default.

Log the specific properties needed for operation and diagnosis.

This reduces:

- accidental secret/PII exposure;
- huge log events;
- serialization cost;
- fragile log schemas.

## Performance

Normal `ILogger` structured logging is sufficient for most code.

For measured high-volume logging hot paths, consider source-generated
`LoggerMessage` APIs.

Do not add source-generated logging everywhere merely to avoid theoretical
allocations.

## Provider-Specific Configuration

Application code should normally depend on `ILogger<T>`, not directly on Serilog
or another provider.

When Serilog is chosen, use the `serilog` skill for:

- bootstrap configuration;
- sinks;
- enrichers;
- request logging;
- provider-specific filtering;
- `LogContext`.

## Anti-Patterns

Avoid:

- string interpolation in structured logs;
- duplicate exception logging;
- logging expected business outcomes as errors;
- logging credentials or secrets;
- dumping full objects without review;
- putting provider-specific APIs throughout Application code;
- using logs as an audit database unless a dedicated audit design has been adopted.

## Decision Guide

| Scenario | Default |
|---|---|
| Application logging API | `ILogger<T>` |
| Context shared across logs | `BeginScope` |
| Unexpected handled exception | Log once at handling boundary |
| Expected business rejection | Usually no Error log |
| Provider-specific Serilog setup | `serilog` skill |
| High-volume measured hot path | Consider `LoggerMessage` |