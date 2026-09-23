# TaskBoard

Single-page application for managing tasks across projects and users, implemented from
[`TASKBOARD-SPEC.md`](TASKBOARD-SPEC.md).

- **Backend:** ASP.NET Core 10 Controllers, Clean Architecture (`Domain` → `Application` →
  `Infrastructure` / `Api`), EF Core 10 + SQL Server, FluentValidation, Serilog.
- **Frontend:** Angular 22 (standalone, zoneless, signals) + Angular Material.
- **Tests:** NUnit (domain, validators, API integration tests against SQL Server) and Vitest
  (Angular components and data access).

```text
src/
  TaskBoard.Domain/              entities and workflow rules
  TaskBoard.Application/         use-case services, DTOs, validators, persistence abstractions
  TaskBoard.Infrastructure/      EF Core DbContext, repositories, migrations, Development seeding
  TaskBoard.Api/                 controllers, ProblemDetails/exception handling, CORS, logging
  TaskBoard.Domain.Tests/
  TaskBoard.Application.Tests/
  TaskBoard.IntegrationTests/    WebApplicationFactory + isolated SQL Server database
frontend/
  TaskBoard.Web/                 Angular dashboard
```

## Prerequisites

- .NET SDK 10.0.4xx (pinned in `global.json`)
- Node.js 24.15+ with npm
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`) or another SQL Server instance

## Run locally

```bash
dotnet tool restore
```

Create or upgrade the Development database (`TaskBoardGoal` on LocalDB):

```bash
dotnet ef database update --project src/TaskBoard.Infrastructure --startup-project src/TaskBoard.Api
```

Start the API on http://localhost:5180. In the Development environment it fills an empty,
migrated database with demo data on startup:

```bash
dotnet run --project src/TaskBoard.Api --launch-profile http
```

Start the dashboard on http://localhost:4200:

```bash
cd frontend/TaskBoard.Web
npm ci
npm start
```

The API allows cross-origin calls only from the origins in `Cors:AllowedOrigins`
(`http://localhost:4200` in `appsettings.Development.json`). The OpenAPI document is served at
`/openapi/v1.json` in Development.

To start again from fresh demo data, drop the database and repeat the update step:

```bash
dotnet ef database drop --force --project src/TaskBoard.Infrastructure --startup-project src/TaskBoard.Api
```

## Tests

```bash
dotnet test
```

```bash
cd frontend/TaskBoard.Web
npm run test:ci
```

Integration tests create a uniquely named database on LocalDB from the migrations and drop it at
the end. To use another SQL Server instance, set `TASKBOARD_TEST_SQLSERVER` to a connection string;
the database name is always replaced with a test-owned one. Tests never use the Development
database or its seed data.

## Implementation decisions

The specification leaves these details open; they are implemented as follows.

- **Deleted User** has the well-known ID `00000000-0000-0000-0000-000000000001` and is inserted by
  the initial migration, so it exists in every environment independently of demo seeding. Its
  email uses the reserved `.invalid` domain. It is not an ordinary user: `GET`, `PUT` and `DELETE`
  on `/api/users/{id}` answer `404` for it, and supplying its ID as `assigneeId` is `422`, even
  when the task already references it. The name "Deleted User" is reserved: creating or renaming
  an ordinary user to it is `422` (`user.reserved_name`).
- **Uniqueness:** user emails and project names are unique case-insensitively; values are trimmed.
- **Timestamps:** `UpdatedAt` changes with ordinary edits and status transitions. The system
  reassignment to Deleted User and related-task changes do not modify a task's core data, so they
  leave `UpdatedAt` unchanged.
- **Validation (400):** missing or malformed fields, invalid emails, over-long values
  (names/titles 200, email 254, project description 2000, task description 4000 characters),
  unknown enum values and unknown JSON properties. For example, sending `projectId` or `status` to
  `PUT /api/tasks/{id}`, or `status` to `POST /api/tasks`, is rejected rather than ignored.
- **Semantic errors (422)** and **state conflicts (409)** follow the specification tables. Errors
  use `ProblemDetails` with a stable `code` extension such as `task.invalid_transition`. A
  uniqueness or reference conflict caused by a concurrent request also answers `409`
  (`persistence.conflict`).
- **Unexpected errors** are logged once by the global exception handler and answered with a
  generic `500` ProblemDetails. Each request's completion is logged through Serilog request
  logging.
