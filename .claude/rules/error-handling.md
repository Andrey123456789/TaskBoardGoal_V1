---
paths:
  - "src/**/*.cs"
---

# Error Handling Rules

## Expected Outcomes

Do not use exceptions casually for normal expected control flow.

Represent expected outcomes using the simplest contract that communicates the
use case clearly.

Depending on the situation this may be:

- `null`;
- `bool`;
- an explicit typed outcome;
- a deliberately adopted `Result<T>` pattern.

`Result<T>` is not mandatory.

Do not introduce a Result abstraction throughout the project solely because an
operation can fail.

## Unexpected Exceptions

Unexpected failures should normally propagate to an appropriate outer boundary.

In ASP.NET Core APIs, use centralized exception handling such as
`IExceptionHandler`.

Do not add broad `try/catch` blocks throughout Controllers, Application Services,
or repositories merely to log and rethrow the same failure.

Catch an exception when the current layer can meaningfully:

- recover;
- translate it into its own abstraction-level outcome;
- perform a defined fallback;
- add useful context while preserving the original exception;
- perform required cleanup.

## Logging

Unexpected exceptions should normally be logged once by the boundary that
handles them.

Do not log the same exception repeatedly across Infrastructure, Application,
Controller, and the global exception handler.

Detailed logging policy belongs to the `logging` skill.

## HTTP Errors

HTTP error semantics belong to the API layer.

Use `ProblemDetails` / `ValidationProblemDetails` as the normal structured HTTP
error contracts.

Do not expose raw exception messages, stack traces, SQL, internal paths, or
other implementation details to clients.

Use the `http-api` skill as the canonical source for HTTP status-code semantics.

## Cancellation

Propagate `CancellationToken` through asynchronous operations when cancellation
is meaningful.

Normal client-request cancellation is not an unexpected server failure.

Do not deliberately:

- turn client cancellation into a generic `500`;
- log normal client cancellation as an unhandled application error;
- retry an operation merely because its caller cancelled it.

## Validation Boundaries

Validate concerns at the layer that owns them.

- API validates transport/request concerns.
- Application enforces use-case rules.
- Domain protects meaningful domain invariants.
- Infrastructure validates assumptions made about external systems when needed.

Do not duplicate the same validation mechanically at every layer.

Detailed implementation guidance belongs to the `error-handling` skill.