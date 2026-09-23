# Backend Layout

This reference defines the default physical structure of the ASP.NET Core
backend.

Architectural behavior is governed by `.claude/rules/architecture.md`.

# Domain

```text
src/
└── ProjectName.Domain/
    ├── Entities/
    └── Enums/
```

Create additional directories only when the Domain actually needs them.

Possible later additions include:

```text
ValueObjects/
DomainServices/
Events/
Exceptions/
```

Do not create DDD-oriented directories by default.

Domain must not depend on Application, Infrastructure, or API.

# Application

```text
src/
└── ProjectName.Application/
    ├── Abstractions/
    │   ├── Persistence/
    │   └── ExternalServices/
    ├── Services/
    ├── DTOs/
    ├── Validation/
    └── DependencyInjection.cs
```

Create only directories that contain real code.

## `Abstractions/Persistence`

Contains inward-facing persistence contracts:

```text
ITaskRepository.cs
IOrderRepository.cs
IUnitOfWork.cs
```

Do not expose EF Core types here.

## `Abstractions/ExternalServices`

Contains Application-owned abstractions implemented by outer layers when needed:

```text
IEmailSender.cs
ICurrentUser.cs
IFileStorage.cs
IPaymentGateway.cs
```

Do not create this directory before such dependencies exist.

## `Services`

Contains Application Services and useful public service contracts:

```text
ITaskService.cs
TaskService.cs
```

Application Services are the default use-case orchestration mechanism.

Do not create handler/CQRS/MediatR structures by default.

## `DTOs`

Contains Application-level operation/data models when useful.

Do not make this a generic dumping ground.

## `Validation`

Contains FluentValidation validators for Application-level input/use-case
validation when such validators are required.

Do not move Domain invariants into FluentValidation merely because the library
is part of the approved stack.

## `DependencyInjection.cs`

Registers Application-owned services and Application-level validation
infrastructure.

Do not register concrete Infrastructure implementations here.

# Infrastructure

```text
src/
└── ProjectName.Infrastructure/
    ├── Persistence/
    │   ├── Configurations/
    │   ├── Repositories/
    │   ├── Migrations/
    │   └── Seeding/
    ├── Services/
    └── DependencyInjection.cs
```

## Persistence

Typical structure:

```text
Persistence/
├── AppDbContext.cs
├── UnitOfWork.cs
├── Configurations/
├── Repositories/
├── Migrations/
└── Seeding/
    ├── DbSeeder.cs
    └── DatabaseInitializationExtensions.cs
```

`Configurations` contains EF Core entity mappings.

`Repositories` contains concrete repository implementations.

An Infrastructure-only generic repository base is allowed when it removes real
duplication.

`Migrations` contains EF Core migrations.

`Seeding` contains Development database initialization according to the
`ef-core` skill.

Do not put Development seed construction in `Program.cs`.

## Services

Contains technical/external implementations of Application abstractions.

Examples:

```text
EmailSender.cs
FileStorage.cs
PaymentGateway.cs
```

Create capability-specific subdirectories when they improve cohesion.

## `DependencyInjection.cs`

Registers Infrastructure-owned implementations and technical dependencies such
as:

```text
AppDbContext
repository implementations
IUnitOfWork
external adapters
SQL Server provider
HttpClient integrations
resilience handlers when used
```

# API

```text
src/
└── ProjectName.Api/
    ├── Controllers/
    ├── Contracts/
    │   ├── Requests/
    │   └── Responses/
    ├── Middleware/
    ├── Extensions/
    ├── Program.cs
    ├── appsettings.json
    └── appsettings.Development.json
```

Create optional directories only when needed.

## Controllers

ASP.NET Core Controllers are the default HTTP endpoint mechanism.

Keep Controllers thin.

Do not create Minimal API endpoint groups unless explicitly requested.

## Contracts

Use transport-specific request/response contracts when separation from
Application models provides value.

Do not duplicate equivalent DTOs across layers without a reason.

## Middleware

Create only when custom middleware actually exists.

## Extensions

Use for cohesive API/composition extension methods.

Do not move arbitrary code out of `Program.cs` solely to make `Program.cs`
shorter.

## Program.cs

API is the composition root.

Typical composition includes:

```csharp
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
```

The API may reference Infrastructure for composition, but Controllers must not
consume Infrastructure implementation types directly.

# Backend Test Projects

Backend tests live under `src/` alongside production projects.

Typical structure:

```text
src/
├── ProjectName.Domain/
├── ProjectName.Domain.Tests/
├── ProjectName.Application/
├── ProjectName.Application.Tests/
├── ProjectName.Infrastructure/
├── ProjectName.Api/
└── ProjectName.IntegrationTests/
```
Create only useful test projects.

ProjectName.Infrastructure.Tests may be added when Infrastructure contains
non-trivial behavior that benefits from a dedicated test assembly.

Do not create a separate top-level tests/ directory.

# Project References

Use this dependency graph:

```text
ProjectName.Domain
    -> no application project references

ProjectName.Application
    -> ProjectName.Domain

ProjectName.Infrastructure
    -> ProjectName.Application
    -> ProjectName.Domain

ProjectName.Api
    -> ProjectName.Application
    -> ProjectName.Infrastructure
```

Never add:

```text
Domain -> Application
Domain -> Infrastructure
Application -> Infrastructure
```

If a feature seems to require one of those references, reconsider the abstraction
boundary instead of adding the reference.

# Test References

Typical references are:

```text
ProjectName.Domain.Tests
    -> ProjectName.Domain

ProjectName.Application.Tests
    -> ProjectName.Application
    -> ProjectName.Domain as needed

ProjectName.IntegrationTests
    -> ProjectName.Api

ProjectName.Infrastructure.Tests       # optional
    -> ProjectName.Infrastructure
    -> narrower references as required
```

Integration tests may transitively exercise Application, Infrastructure, EF Core,
routing, DI, serialization, and persistence through the API host.

Do not give test projects broader production references than their test scope
requires.