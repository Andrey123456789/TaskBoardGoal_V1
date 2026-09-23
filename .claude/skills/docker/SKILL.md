---
name: docker
description: >
  Docker guidance for .NET web applications and Angular frontends. Covers
  multi-stage images, restore-layer caching, non-root execution, .dockerignore,
  Docker Compose for development, configuration/secrets, health probes, and
  container build verification. Use when adding or reviewing Dockerfiles,
  Compose files, or container deployment configuration.
---

# Docker

## Core Principles

1. Keep build tooling out of production runtime images.
2. Run production containers as a non-root user where supported.
3. Keep images reproducible and configuration external.
4. Never bake real secrets into images.
5. Keep Docker concerns separate from application architecture.
6. Use the deployment platform's health-probe capabilities when practical.
7. Do not add Docker merely because the project can be containerized.

## ASP.NET Core Dockerfile

Use a multi-stage build:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY Directory.Packages.props ./

COPY src/MyApp.Domain/MyApp.Domain.csproj src/MyApp.Domain/
COPY src/MyApp.Application/MyApp.Application.csproj src/MyApp.Application/
COPY src/MyApp.Infrastructure/MyApp.Infrastructure.csproj src/MyApp.Infrastructure/
COPY src/MyApp.Api/MyApp.Api.csproj src/MyApp.Api/

RUN dotnet restore src/MyApp.Api/MyApp.Api.csproj

COPY . .

RUN dotnet publish src/MyApp.Api/MyApp.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

USER app

EXPOSE 8080

ENTRYPOINT ["dotnet", "MyApp.Api.dll"]
```

The example assumes these repository-level build files exist.

Copy only repository-level build files that are actually present and required by
restore. Do not add `Directory.Build.props` or `Directory.Packages.props` merely
to satisfy the Dockerfile example.

Explicitly copy project files required for restore.

Do not use fragile wildcard tricks that flatten or accidentally rearrange
multi-project directory structure.

## Restore Caching

Copy project/build dependency files before the complete source tree so source
changes do not unnecessarily invalidate NuGet restore layers.

When the solution structure changes, update the Dockerfile's project-copy
section.

Do not optimize Docker layer caching at the cost of a Dockerfile that no longer
restores the actual project graph correctly.

## Runtime Image

Use the ASP.NET runtime image for ASP.NET Core applications.

Use an appropriate .NET runtime image for workers that do not require ASP.NET
Core.

Do not use the SDK image as the production runtime solely for convenience.

## Non-Root

Use the built-in non-root user supported by modern .NET container images when it
fits the image and deployment environment:

```dockerfile
USER app
```

Ensure directories the application writes to have appropriate ownership and
permissions.

Do not switch to root merely to solve a filesystem problem without understanding
why permissions are incorrect.

## Configuration and Secrets

Pass environment-specific configuration at runtime.

Examples include:

```text
ASPNETCORE_ENVIRONMENT
ConnectionStrings__Default
Authentication__Authority
```

Never put real credentials in:

- Dockerfile;
- committed Compose files;
- build arguments that remain visible in image metadata;
- copied appsettings files containing production secrets.

Use platform/container secret mechanisms where appropriate.

## Docker Compose Development

Compose is useful for local infrastructure such as databases or Redis when the
project benefits from it.

Example shape:

```yaml
services:
  api:
    build:
      context: .
      dockerfile: src/MyApp.Api/Dockerfile
    ports:
      - "5000:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__Default: ${DB_CONNECTION_STRING}
```

Add only the infrastructure the project actually uses.

Do not add PostgreSQL, Redis, RabbitMQ, or another service merely because it is
common in examples.

Do not commit real local passwords when a local `.env` or another ignored
developer configuration mechanism is more appropriate.

## Health Probes

Application health behavior belongs to the `health-check` skill.

Expose appropriate liveness/readiness endpoints through ASP.NET Core health
checks.

Prefer native HTTP probes supplied by the orchestrator when available.

Do not assume the runtime image contains:

```text
curl
wget
shell utilities
```

If Docker Compose itself must execute an in-container health command, verify the
chosen runtime image actually provides the required executable or explicitly add
an appropriate probe mechanism.

Do not install large debugging packages into a production image solely for a
health check without considering the trade-off.

## Angular Container

When the Angular frontend is deployed as a static application, build it in a
Node build stage and serve the generated assets using the selected hosting
strategy.

Do not assume Nginx is mandatory; the deployment platform may provide static
hosting directly.

Keep API base URLs and environment configuration compatible with the selected
deployment strategy.

Frontend architecture belongs to the `angular` skill.

## `.dockerignore`

Exclude files that do not belong in the build context, for example:

```text
.git
.vs
**/bin
**/obj
**/node_modules
TestResults
coverage
```

Do not blindly exclude tests if the Docker build or CI process intentionally
runs tests inside the image build.

Do not exclude files required by restore/build.

## Image Optimization

Start with standard supported .NET runtime images.

Consider Alpine, chiseled images, trimming, Native AOT, or other image-size
optimizations only when compatibility and deployment requirements justify them.

Do not make trimming or Alpine the default merely to minimize image size.

Verify globalization, native dependencies, reflection, diagnostics, and other
runtime behavior when using specialized images.

## Database Migrations

Do not automatically apply production migrations from normal application
startup.

Container deployment does not change the database migration policy defined by
the project.

Development seeding remains Development-only according to the `ef-core` skill.

## Verification

For Docker changes:

```text
build the image
verify application startup
verify required port/configuration
verify non-root behavior when relevant
verify health probe integration
review image for accidental secrets/files
```

Run meaningful application tests outside or as part of the selected build
pipeline.

## Anti-Patterns

Avoid:

- SDK image as production runtime;
- production root execution without a concrete reason;
- secrets baked into images;
- arbitrary infrastructure services in Compose;
- assuming curl/wget exists in runtime images;
- automatic production DB migration;
- specialized image variants without compatibility verification;
- Docker-specific changes leaking into Domain/Application code.