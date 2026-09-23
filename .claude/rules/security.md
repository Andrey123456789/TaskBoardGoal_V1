---
paths:
  - "src/**/*"
  - "frontend/**/*"
  - "**/*.json"
  - "**/*.yml"
  - "**/*.yaml"
  - "**/*.csproj"
  - "**/*.props"
  - "**/*.targets"
  - "**/*.config"
  - "**/.env*"
  - "Dockerfile*"
  - "**/Dockerfile*"
---

# Security Rules

## Secrets

Never hardcode real secrets, credentials, private keys, access tokens, or
production connection strings in source-controlled files.

For local development, use an appropriate local secret mechanism such as
`dotnet user-secrets`.

For deployed environments, use environment-specific secret injection or a
managed secret store appropriate to the hosting platform.

Do not assume a specific cloud provider unless the project has chosen one.

Never commit real secrets to example configuration files.

## Input Boundaries

Treat data from outside the trusted application boundary as untrusted.

Validate inputs where appropriate, including:

- HTTP requests;
- uploaded files;
- external-service responses when assumptions matter;
- message/event payloads;
- user-provided identifiers and filters.

Use the validation mechanism appropriate to the owning layer.

FluentValidation is the project's standard library for explicit request and
Application use-case validation.

Do not move Domain invariants into FluentValidation merely because the library
is available.

## SQL and Persistence

Use parameterized database access.

EF Core LINQ normally parameterizes values automatically.

For raw SQL, Dapper, or ADO.NET, use supported parameterization mechanisms.
Never construct SQL by concatenating untrusted input.

## Authentication and Authorization

Default to explicit, reviewable authorization behavior.

A project may secure endpoints using:

- global/fallback authorization policies;
- controller/action authorization attributes;
- endpoint authorization configuration.

Do not require redundant `[Authorize]` attributes when a secure global policy
already defines the intended behavior.

Anonymous access should be deliberate and visible.

Authorization must be enforced on the server even if the client also hides or
disables UI actions.

## HTTPS and Transport Security

Production traffic carrying credentials or sensitive data must use TLS.

Respect the actual hosting topology. TLS may terminate in the application, a
reverse proxy, ingress controller, gateway, or hosting platform.

Do not disable certificate validation to make an integration work.

Do not require HTTP-to-HTTPS redirection when the deployment architecture
already provides an appropriate secure transport boundary and redirection is not
desired.

## Data Protection and Encryption

Do not implement custom cryptography when a well-reviewed platform mechanism is
appropriate.

ASP.NET Core Data Protection is suitable for application-generated protected
payloads such as cookies or temporary protected values.

Do not treat ASP.NET Core Data Protection as a universal database encryption
solution.

Use storage/database/platform encryption mechanisms according to the actual data
protection requirement.

## CORS

Configure CORS according to the actual browser-client requirements.

Prefer the narrowest policy that satisfies the application.

Do not combine credentialed cross-origin requests with an unrestricted origin
policy.

A public unauthenticated API may legitimately have different CORS requirements
from a private browser application.

## Logging and Sensitive Data

Never log:

- passwords;
- access or refresh tokens;
- private keys;
- authentication secrets;
- full payment credentials.

Avoid logging PII unless there is a concrete operational requirement and the
project's privacy/security policy permits it.

Changing the log level does not make sensitive data safe.

Prefer stable technical identifiers over personal data in operational logs.

## Error Responses

Do not expose stack traces, SQL statements, connection strings, internal paths,
secret values, or implementation details in public error responses.

Use the project's centralized error-handling policy.

## Dependencies

Dependency selection, versioning, licensing, and vulnerability checks are owned
by `.claude/rules/dependencies.md`.

Security-sensitive dependency changes must not bypass that policy.