---
name: ci-cd
description: >
  CI/CD guidance for .NET backend and Angular frontend projects. Covers GitHub
  Actions and equivalent CI systems, restore/build/test flows, artifacts,
  frontend validation, dependency/security checks, container publishing,
  deployment separation, secrets, and production migration handling.
  Use when creating or reviewing build, test, release, or deployment pipelines.
---

# CI/CD

## Core Principles

1. Keep pipeline definitions in source control.
2. CI should reproduce the repository's supported build and test commands.
3. Build/test failures must remain visible.
4. Do not hardcode environment secrets.
5. Build deployable artifacts reproducibly.
6. Keep deployment/environment configuration outside compiled artifacts where
   practical.
7. Production database migration is an explicit deployment/operational step.
8. Add pipeline stages according to the actual project, not generic boilerplate.

## Repository Discovery

Before writing a pipeline, inspect the repository.

Determine whether it contains:

```text
.NET backend
Angular frontend
test projects
Dockerfiles
database/provider-specific integration tests
generated clients
deployment artifacts
```

Do not assume PostgreSQL, Redis, Docker, or frontend tooling exists.

## Backend CI

Typical .NET flow:

```bash
dotnet restore
dotnet build --no-restore --configuration Release
dotnet test --no-build --configuration Release
```

Run formatting/analyzers when the repository has adopted them as enforced checks.

For example:

```bash
dotnet format --verify-no-changes --no-restore
```

Do not turn an optional local formatter into a mandatory CI gate without the
project adopting that policy.

## Frontend CI

When an Angular frontend exists, use the lockfile-based package installation:

```bash
npm ci
```

Then run the scripts defined by that frontend project, normally covering:

```text
build
unit tests when configured
lint when configured
```

Do not invent an npm script that does not exist.

Do not use `npm install` in CI when a maintained lockfile allows `npm ci`.

Follow the `angular` skill for frontend-specific guidance.

## GitHub Actions Shape

A typical workflow can separate backend and frontend verification:

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  backend:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v5

      - uses: actions/setup-dotnet@v5
        with:
          dotnet-version: 10.0.x

      - run: dotnet restore
      - run: dotnet build --no-restore --configuration Release
      - run: dotnet test --no-build --configuration Release

  frontend:
    runs-on: ubuntu-latest

    defaults:
      run:
        working-directory: frontend/MyApp.Web

    steps:
      - uses: actions/checkout@v5

      - uses: actions/setup-node@v4
        with:
          node-version: 22
          cache: npm
          cache-dependency-path: frontend/MyApp.Web/package-lock.json

      - run: npm ci
      - run: npm run build
```

Adapt action and runtime versions to the repository's supported versions.

Add frontend test/lint commands only when the project defines them.

## Integration-Test Infrastructure

Provide external services in CI only when tests actually require them.

Examples may include:

```text
SQL Server
PostgreSQL
Redis
message broker
```

Do not hardcode one database provider into the generic template.

The integration test environment must be isolated from Development and
Production data.

Follow the `testing` skill.

## Dependency Security

For .NET 10, package vulnerability inspection may use:

```bash
dotnet package list --vulnerable --include-transitive
```

NuGet auditing is also integrated into restore for modern .NET SDKs.

Treat dependency findings according to the project's security policy.

Do not automatically upgrade packages in CI unless the project has explicitly
adopted automated dependency updates.

## Build Artifacts

When a deployment artifact is required, produce it deliberately:

```bash
dotnet publish \
  src/MyApp.Api/MyApp.Api.csproj \
  --configuration Release \
  --output ./artifacts/api \
  --no-build
```

For Angular:

```bash
npm run build
```

Store or publish only artifacts required by later stages.

## Docker

When the project deploys containers, follow the `docker` skill.

Build and publish the image from CI only when container deployment is part of
the project.

Do not introduce Docker into the pipeline merely because the skill supports it.

## Build Once, Promote

For a deployment model that supports artifact promotion, prefer promoting the
same tested artifact/image through environments rather than recompiling
different production code for every environment.

Environment-specific behavior should normally come from runtime configuration.

## Secrets

Use the CI platform's secret/identity mechanism.

Do not place real secrets directly in pipeline YAML.

Prefer workload/federated identity over long-lived deployment credentials when
the chosen platform supports it.

## Database Migrations

Production migrations must not run automatically from normal application
startup.

If deployment performs migrations:

- make the migration step explicit;
- use the project's approved credentials;
- run it at a controlled point in deployment;
- fail visibly on migration failure;
- do not seed Development data in Production.

Migration scripts/artifacts may be generated earlier in CI and executed during
deployment.

## Deployment

CI and CD may be separate workflows.

A successful CI build does not automatically imply that every branch should
deploy.

Use environment protection/approval where appropriate to the actual hosting
platform.

## Verification

Before considering a pipeline complete, verify that it:

```text
restores from a clean environment
builds backend
builds frontend when present
runs configured tests
does not depend on developer-machine state
does not expose secrets
produces expected artifacts
```

## Anti-Patterns

Avoid:

- project-specific database infrastructure in a generic pipeline;
- deployment secrets in YAML;
- rebuilding different source for every environment without a reason;
- suppressing failing tests to permit deployment;
- automatic production seeding;
- automatic production migrations from application startup;
- assuming Docker deployment;
- assuming frontend scripts that the repository does not define.