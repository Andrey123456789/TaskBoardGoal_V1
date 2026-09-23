---
paths:
  - "src/**/*.cs"
---

# Performance Rules

## General Principle

Prefer clear, correct code first.

Do not introduce specialized performance optimizations without a concrete reason.
For non-trivial optimizations, prefer measurement or profiling evidence over assumptions.

## Async and Cancellation

- Use async APIs for asynchronous I/O.
- Do not block asynchronous code with `.Result`, `.Wait()`, or similar sync-over-async patterns.
- Propagate `CancellationToken` through asynchronous operations when cancellation is meaningful.
- Do not add cancellation parameters to purely synchronous code without a use case.

## Time

Use `TimeProvider` for application logic whose behavior depends on the current time
and needs to be testable.

Do not mechanically replace every use of `DateTime` or `DateTimeOffset`.
Values representing stored timestamps or explicit dates are not clock dependencies.

## HTTP

Do not create and dispose a new `HttpClient` for every request.

Use `IHttpClientFactory` or another appropriate long-lived client strategy for
outbound HTTP communication.

Detailed HTTP client and resilience guidance belongs in the relevant skills.

## Database Access

Keep database work efficient by default:

- filter and project in the database when practical;
- avoid unnecessary round trips;
- avoid loading significantly more data than required;
- watch for N+1 query patterns;
- use pagination for potentially large result sets.

Detailed EF Core guidance belongs in the `ef-core` skill.

## Caching

Caching is an optimization, not a default architectural requirement.

Introduce caching when there is a clear reason, such as:

- measured expensive repeated work;
- high read volume;
- expensive remote calls;
- acceptable staleness semantics.

Choose the cache implementation according to the deployment and consistency
requirements.

Do not introduce caching merely because a value is read frequently.

## Measured Optimizations

The following techniques are valid but should normally be introduced only when
a measured or clearly demonstrated need exists:

- `EF.CompileQuery` / `EF.CompileAsyncQuery`;
- `ValueTask<T>`;
- `ArrayPool<T>` / `MemoryPool<T>`;
- manual object pooling;
- custom serialization optimizations;
- aggressive caching;
- raw SQL for performance.

Do not sacrifice maintainability for speculative micro-optimization.

## Allocation-Sensitive Code

Prefer ordinary clear .NET code by default.

For demonstrated allocation-heavy hot paths, consider appropriate tools such as
spans, pooling, source-generated serialization, or preallocated buffers.

Verify that the optimization materially improves the relevant workload.

## Performance Changes

When making a performance-motivated change:

1. Identify the actual bottleneck or cost.
2. Preserve observable behavior.
3. Prefer the simplest optimization that addresses the problem.
4. Add or preserve tests for behavior affected by the optimization.
5. Benchmark or profile when the optimization is non-obvious or complex.