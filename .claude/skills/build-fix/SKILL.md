---
name: build-fix
description: >
  Diagnose and fix broken .NET or Angular builds and failing automated tests
  through a bounded, evidence-driven iteration loop. Covers backend, frontend,
  full-stack contract failures, package/API upgrades, toolchain issues, and
  regression repair. Use when compilation/build fails, tests are broken after
  a change, package or framework upgrades introduce errors, or the user
  explicitly asks to make the repository green.
---

# Build Fix

## Goal

Restore the affected repository scope to a green build/test state without hiding
failures, weakening behavior, or introducing unrelated changes.

`build-fix` is a repair workflow.

Use:

    code-review
        -> to inspect completed changes for defects

    verify
        -> to prove a completed change is ready

    build-fix
        -> when a build/test failure already exists and must be repaired

## Principles

- Diagnose before editing.
- Reproduce the failure before guessing at a fix.
- Fix root causes before downstream symptoms.
- Keep each iteration small.
- Re-run the narrowest failing verification after each meaningful fix.
- Expand verification only after the focused failure is fixed.
- Do not weaken tests merely to make them pass.
- Do not suppress compiler, TypeScript, lint, or analyzer errors without
  understanding them.
- Preserve architecture and project conventions while fixing the failure.
- Stop when progress has clearly stalled rather than making speculative edits.

## Determine the Affected Scope

Before running commands, determine whether the failure concerns:

    .NET backend
    Angular frontend
    backend + frontend
    API contract/client generation
    package/dependency changes
    repository/toolchain configuration

Do not run unrelated build systems merely because they exist in the repository.

Examples:

    backend-only compile failure
        -> start with the affected .NET project

    Angular template/TypeScript failure
        -> start with the Angular workspace

    changed API contract consumed by Angular
        -> inspect and verify both sides

    documentation-only failure
        -> do not run application builds without a reason

## Reproduce the Failure

Start with the narrowest command that reliably reproduces the problem.

### .NET

Typical commands include:

    dotnet build src/MyApp.Api/MyApp.Api.csproj

or, when broader scope is required:

    dotnet build

For a failing test project:

    dotnet test src/MyApp.Application.Tests/MyApp.Application.Tests.csproj

Use the repository's actual `.slnx`, project path, and configuration where
appropriate.

### Angular

First inspect the Angular workspace's `package.json`.

Use scripts that actually exist.

Typical examples may include:

    npm run build
    npm test

Do not invent script names.

Do not assume a particular test runner merely because Angular is in use.

For this template, Vitest is the normal baseline for newly scaffolded Angular
projects, but an existing project may deliberately use another configured
runner.

## Capture the Actual Error

Read the complete relevant failure output.

Do not fix only the first visible line when the surrounding diagnostic provides
important context.

Group related errors by likely root cause.

Common .NET categories include:

    missing project/package reference
    namespace/type resolution
    changed API signature
    nullability
    generic/type mismatch
    interface contract change
    package version/API change
    source-generation problem
    project/MSBuild configuration
    target framework or SDK mismatch

Common Angular categories include:

    TypeScript type error
    template compilation error
    dependency injection/provider error
    invalid standalone import
    route configuration error
    signal/RxJS typing issue
    missing package/module
    Angular/TypeScript peer-version mismatch
    generated API client mismatch
    build configuration problem

One root cause may produce many secondary failures.

Fix the highest-leverage cause first.

## Backend Build-Fix Loop

For .NET failures:

1. Reproduce the narrowest failure.
2. Identify the probable root cause.
3. Inspect the relevant source, project references, and package configuration.
4. Make the smallest correct change.
5. Re-run the same failing build/test.
6. Compare the new failure set to the previous one.
7. Expand verification only after the focused failure is resolved.

Do not solve architectural dependency problems by adding invalid project
references.

For example, do not make Application reference Infrastructure merely because
that makes a missing type compile.

Follow `architecture.md`.

## Frontend Build-Fix Loop

For Angular failures:

1. Inspect `package.json` and relevant Angular configuration.
2. Reproduce the failing build/test command.
3. Read the TypeScript/Angular diagnostic in context.
4. Inspect the affected component, service, route, template, or configuration.
5. Make the smallest correct change.
6. Re-run the same command.
7. Expand to broader frontend verification after the focused failure is fixed.

Follow the `angular` skill for frontend conventions.

Do not:

    convert code to `any` merely to silence TypeScript
    disable strict mode to make code compile
    suppress template diagnostics without understanding them
    add NgModules merely to work around a standalone configuration problem
    replace framework APIs speculatively
    introduce another state-management or HTTP library just to bypass a local issue

## Full-Stack and Contract Failures

Some failures cross the backend/frontend boundary.

Examples:

    response contract changed
    generated TypeScript client no longer matches OpenAPI
    enum/value semantics changed
    endpoint path or HTTP method changed
    nullability changed
    authentication contract changed

Determine which contract is authoritative.

Do not independently patch both sides into inconsistent shapes merely to make
both compile.

When OpenAPI/client generation is used:

1. verify the backend contract;
2. regenerate the client using the project's normal workflow;
3. review generated changes;
4. fix handwritten frontend code against the resulting contract.

Do not manually patch generated files unless the project's generation workflow
explicitly requires that.

Follow `openapi` and `http-api` where relevant.

## Test-Fix Flow

When the build succeeds but tests fail:

1. Read the failing test.
2. Read the relevant production behavior.
3. Determine whether the defect is in:
   - production code;
   - test setup;
   - test expectation;
   - environment/configuration;
   - intentionally changed contract.
4. Make the smallest correct change.
5. Re-run the affected test or test project.
6. Run broader relevant tests after the focused failure is fixed.

Do not change:

    strong assertion
    -> weak assertion

merely to obtain a green test.

Do not delete or skip a failing test solely because fixing it is inconvenient.

If expected behavior intentionally changed, update the test to express the new
contract and confirm that the behavioral change was actually requested.

Backend testing guidance belongs to the `testing` skill.

Frontend testing guidance belongs to the `angular` skill.

## Dependency and Package Failures

When a failure follows adding or upgrading a NuGet or npm dependency:

1. Inspect the exact installed/requested version.
2. Confirm framework/runtime compatibility.
3. Inspect peer dependencies where applicable.
4. Read the actual compiler/build diagnostics.
5. Check official migration/release documentation when necessary.
6. Update usage deliberately.

All dependency additions or version changes must follow:

    .claude/rules/dependencies.md

This includes:

    stable-compatible version selection
    license verification
    deprecation checks
    vulnerability checks
    commercial/license approval rules

Do not replace a package or add another dependency merely to make the build
green without applying the dependency policy.

### NuGet

Respect Central Package Management.

Versions normally belong in:

    Directory.Packages.props

Do not work around dependency problems by duplicating conflicting versions in
individual `.csproj` files.

### npm

Preserve the repository's lockfile and dependency model.

Do not use:

    --force
    --legacy-peer-deps

merely to suppress a peer-dependency conflict.

Determine and resolve the actual compatibility problem instead.

Do not delete `package-lock.json` reflexively as a troubleshooting technique.

## Framework/API Upgrades

When failures follow a .NET, Angular, TypeScript, EF Core, or other framework
upgrade:

- inspect the actual installed version;
- distinguish compilation errors from changed runtime behavior;
- check official migration documentation when needed;
- update obsolete API usage deliberately;
- preserve project architecture and behavior where the upgrade does not require
  change.

Do not fabricate replacement APIs from memory when they can be verified.

Do not perform an additional unrelated framework upgrade while repairing the
current one.

## Toolchain Problems

Determine whether the failure originates from source code or from the local
toolchain.

Relevant checks may include:

    dotnet --info
    dotnet --version
    node --version
    npm --version

Also inspect:

    global.json
    target framework
    Angular package versions
    package-lock.json
    CI/runtime version configuration

Do not change the repository's declared SDK, Node, framework, or language
version merely because the local machine has a different version installed.

If the required toolchain is unavailable and cannot be resolved safely within
the requested task, report that limitation instead of editing source code to
compensate for it.

## Generated Code

When the error occurs in generated output, identify the generator/input first.

Examples include:

    OpenAPI clients
    source generators
    EF Core migrations
    Angular generated build output

Prefer fixing the source configuration or input and regenerating.

Do not manually patch disposable generated output unless the repository treats
that output as maintained source.

## Iteration Limits

Use bounded iteration.

A reasonable default is several focused attempts, not an unlimited edit/build
loop.

Stop and reassess when:

- the same root failure survives repeated materially different fixes;
- fixes create increasing unrelated failures;
- the installed toolchain is incompatible;
- required external infrastructure is unavailable;
- package compatibility cannot be resolved safely;
- the correct behavior cannot be determined from the project or request.

Do not repeatedly apply variants of the same failed guess.

## Regressions

If a fix introduces new failures:

- determine whether they reveal a legitimate dependency of the change;
- otherwise revert or correct that fix before continuing.

Do not leave the repository knowingly worse than the state from which the repair
started.

Do not clean up unrelated pre-existing failures unless they block verification
of the requested repair or the user asks for them to be fixed.

## Architecture

A build fix must not bypass architecture merely to compile.

For backend code, follow `architecture.md`.

For Angular code, follow the `angular` skill.

Examples of invalid repair shortcuts include:

    Application -> Infrastructure reference
    DbContext leaking into Application
    business logic moved into Controller
    server authorization replaced by Angular-only guards
    HTTP calls scattered into components to bypass a data-access service

A green build obtained by violating project architecture is not a successful
repair.

## Tests Created During Diagnosis

If a new behavioral test or temporary probe is used to establish correct
behavior, follow the `testing` policy.

The behavior must remain represented by maintained automated coverage unless
equivalent coverage already exists.

Temporary diagnostics or probes may be removed only after any uniquely verified
behavior has been preserved appropriately.

Build/compiler/static-analysis commands themselves do not require creating new
tests.

## Completion

A build-fix task is complete when the relevant affected scope is green.

Depending on the task, this may mean:

    backend build passes
    frontend build passes
    affected tests pass
    cross-stack contract is consistent
    no known new regression remains
    final diff contains no accidental workaround

Do not require an unrelated frontend build for a backend-only repair or vice
versa.

After the focused repair succeeds, use the `verify` skill for the final
completion pass when appropriate.

## Reporting

Report what was actually repaired and verified.

Example:

    Initial failure:
    <failure>

    Root cause:
    <cause>

    Changes made:
    <changes>

    Backend build:
    PASS / NOT RUN / NOT APPLICABLE

    Frontend build:
    PASS / NOT RUN / NOT APPLICABLE

    Tests:
    PASS / NOT RUN / NOT APPLICABLE

    Remaining issues:
    None / <details>

Do not claim the problem is fixed while a known relevant failure remains.