# Technology Stack

This reference defines the default technology choices for newly scaffolded
projects.

Do not silently replace these technologies with alternatives.

Resolve concrete package versions at scaffolding/install time rather than
hardcoding historical versions in this document.

# Backend Platform

Use:

```text
.NET 10 LTS
C# 14
ASP.NET Core
ASP.NET Core Controllers
```

Use the latest stable servicing release of the selected .NET 10 line.

Do not move the project to a preview/RC .NET or C# release without explicit
approval.

# Persistence

Use:

```text
Entity Framework Core
Microsoft SQL Server
```

Primary provider:

```text
Microsoft.EntityFrameworkCore.SqlServer
```

Use EF Core normally inside Infrastructure.

Keep provider-specific concerns out of Application and Domain.

Persistence architecture follows `architecture.md` and the `ef-core` skill.

# Development Database Configuration

API configuration includes:

```text
appsettings.json
appsettings.Development.json
```

`appsettings.Development.json` should contain a usable local Development
connection string when that connection string contains no secret credentials.

A suitable Windows Development default is:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ProjectName;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Adapt the database name to the project.

Do not commit real usernames/passwords, tokens, production hosts, or production
connection strings.

When local SQL Server requires credentials, keep the non-secret structure in
configuration and override the secret value through user-secrets or another
approved local secret mechanism.

Development database seeding follows the `ef-core` skill:

```text
Development only
seed when empty
representative entity states
never automatic Production seeding
```

# Validation

Use FluentValidation as the standard validation library.

Baseline packages:

```text
FluentValidation
FluentValidation.DependencyInjectionExtensions
```

Use FluentValidation for request/use-case validation where validation is needed.

Do not move meaningful Domain invariants out of Domain entities merely because
FluentValidation is available.

Do not use a deprecated or legacy ASP.NET validation integration package unless
a concrete project requirement justifies it.

# Logging

Use:

```text
Microsoft.Extensions.Logging abstractions
Serilog as the logging provider
```

Baseline integration:

```text
Serilog.AspNetCore
```

Add sinks only when they are required.

A normal local application may use a Console sink.

Application code should normally depend on:

```text
ILogger<T>
```

rather than Serilog-specific APIs.

Detailed policy belongs to the `logging` and `serilog` skills.

# Mapping

Manual mapping is acceptable for small/simple mappings.

When mapping volume becomes repetitive or non-trivial, use Mapster.

Approved mapping stack:

```text
Mapster
Mapster.DependencyInjection
```

Do not add Mapster to a project that has no meaningful mapping requirement.

Do not introduce another mapping framework without explicit approval.

Do not put mapping-library concerns into Domain.

# Resilience

Use modern Microsoft resilience integration when resilience is required.

For outbound `HttpClient`:

```text
Microsoft.Extensions.Http.Resilience
```

For general non-HTTP resilience pipelines when needed:

```text
Microsoft.Extensions.Resilience
```

Do not install resilience packages merely because the project might eventually
call an external service.

Add them when a real integration requires timeout/retry/circuit-breaker or other
resilience behavior.

Do not add:

```text
Microsoft.Extensions.Http.Polly
Polly.Extensions.Http
```

Direct Polly usage is an explicit exception governed by the `resilience` skill.

# API Documentation

Use built-in ASP.NET Core OpenAPI support.

Do not add Swagger/OpenAPI packages merely out of habit when built-in framework
support satisfies the requirement.

Detailed API documentation guidance belongs to the `openapi` skill.

# Testing

Use NUnit for .NET automated tests.

Backend .NET test projects live under `src/` alongside the backend production
projects.

Typical test projects are:

```text
ProjectName.Domain.Tests
ProjectName.Application.Tests
ProjectName.IntegrationTests
```

Do not create a separate top-level tests/ directory.

Angular unit/component tests remain inside the Angular workspace under
frontend/ProjectName.Web/.

Use the Angular project's configured test runner; Vitest is the baseline for new
projects.

When frontend E2E coverage is required, Playwright is the approved default and
its tests should remain inside the frontend workspace.

For ASP.NET Core integration tests use:

```text
Microsoft.AspNetCore.Mvc.Testing
WebApplicationFactory<Program>
```

Use the actual SQL Server provider when provider fidelity is required.

Testcontainers is optional, not part of the baseline dependency set.

Detailed strategy belongs to the `testing` skill.

# Angular Frontend

Use the latest stable Angular release supported by the selected Node.js
environment at scaffolding time.

Create the application using the current Angular CLI.

Baseline frontend choices:

```text
Angular
TypeScript
standalone APIs
strict mode
routing
zoneless mode when supported by the selected stable Angular version
SCSS
RxJS
Angular Material
Angular CDK
Vitest
```

Let Angular CLI select mutually compatible Angular/TypeScript/RxJS versions.

Do not independently upgrade TypeScript beyond Angular's supported range merely
because a newer TypeScript package exists.

Detailed frontend architecture belongs to the `angular` skill.

# Angular Libraries

Use Angular Material as the default UI component library.

Use Angular CDK when lower-level Material/CDK capabilities are useful.

Do not add by default:

```text
NgRx
Axios
Lodash
Bootstrap
Tailwind
additional form frameworks
additional component libraries
additional state-management libraries
```

Introduce extra libraries only for a concrete requirement.

Use built-in Angular capabilities before adding overlapping dependencies.

For end-to-end testing, Playwright is an approved option when E2E coverage is
required, but it is not part of the minimal default scaffold.

# Dependency Policy

All NuGet and npm dependency additions or updates must follow:

```text
.claude/rules/dependencies.md
```

That rule owns:

- version selection;
- stable-versus-prerelease policy;
- deprecation and vulnerability checks;
- license verification;
- paid/commercial package approval;
- handling of copyleft/custom/unclear licenses;
- NuGet Central Package Management;
- npm lockfile and peer-dependency policy.

This document defines the project's approved technology stack.

It does not override the dependency-selection and license rules.

# Dependency Minimalism

The approved stack defines preferred technologies, not a requirement to install
every possible package immediately.

Baseline packages should support capabilities present in the initial project.

Conditional packages should be introduced only when their capability becomes
necessary.

Examples:

```text
Mapster
    -> when meaningful mapping appears

Microsoft.Extensions.Http.Resilience
    -> when outbound HTTP requires resilience behavior

Microsoft.Extensions.Resilience
    -> when a non-HTTP resilience pipeline is needed

Testcontainers
    -> when real-provider isolated tests justify it

Playwright
    -> when E2E testing is required
```

Do not install dependencies for hypothetical future use.