---
name: project-structure
description: >
  Create or reorganize the physical structure of an ASP.NET Core + Angular
  Clean Architecture solution. Covers solution scaffolding, backend projects,
  project references, test projects, frontend placement, shared build files,
  baseline technology selection, and structural verification.
  Use when creating a new solution, adding/removing projects, or reorganizing
  repository-level structure.
---

# Project Structure

## Purpose

Use this skill when creating a new solution or changing its physical project
structure.

Architectural policy is defined in `.claude/rules/architecture.md`.

Do not redefine or override architecture rules here.

Detailed implementation guidance belongs to the relevant specialized skills.

## References

Read supporting references according to the task.

### `references/solution-layout.md`

Read when:

- creating a new repository/solution;
- reorganizing top-level directories;
- adding frontend/tests/docs;
- deciding where shared repository files belong.

### `references/backend-layout.md`

Read when:

- creating or reorganizing Domain, Application, Infrastructure, or API projects;
- adding project references;
- deciding where backend files/directories belong.

### `references/technology-stack.md`

Read when:

- scaffolding a new application;
- selecting baseline packages;
- configuring SQL Server/local Development settings;
- choosing approved optional libraries;
- adding initial Angular dependencies.

Do not load every reference merely because this skill was triggered.

## Goals

When scaffolding or reorganizing a solution:

1. Keep project boundaries explicit.
2. Enforce dependency direction through project references.
3. Keep production projects, test projects, frontend, and documentation clearly
   organized according to the repository layout.
4. Use the repository's approved technology stack.
5. Avoid speculative projects, abstractions, and directories.
6. Keep the generated solution immediately buildable.
7. Verify the resulting structure before completion.

## Default Shape

The normal solution contains:

```text
src/
    ProjectName.Domain/
    ProjectName.Domain.Tests/
    ProjectName.Application/
    ProjectName.Application.Tests/
    ProjectName.Infrastructure/
    ProjectName.Api/
    ProjectName.IntegrationTests/

frontend/
    ProjectName.Web/

docs/
```

This is a target organization, not a requirement to create every optional
directory or test project immediately.

Read `references/solution-layout.md` for the complete repository shape.

## Scaffolding Procedure

### 1. Determine Scope

Before creating files, determine what the requested project actually needs:

```text
backend
Angular frontend
test projects
documentation
external integrations
mapping
containerization
```

Do not scaffold optional infrastructure merely because it may be useful later.

### 2. Read the Technology Stack

For a new project, read:

```text
references/technology-stack.md
```

Use the baseline stack defined there.

Do not silently substitute alternative frameworks or libraries.

Conditional technologies such as Mapster or resilience packages should be added
only when the project actually requires their capability.

### 3. Create Repository-Level Structure

Read:

```text
references/solution-layout.md
```

Create only the required top-level directories and repository-wide configuration
files.

### 4. Create Backend Projects

Read:

```text
references/backend-layout.md
```

The normal backend consists of:

```text
ProjectName.Domain
ProjectName.Application
ProjectName.Infrastructure
ProjectName.Api
```

Use the current approved .NET target from `technology-stack.md`.

### 5. Add Project References

Add only the references described in:

```text
references/backend-layout.md
```

Project references must enforce the dependency direction defined by
`architecture.md`.

Do not add an outward dependency merely to make compilation easier.

### 6. Add Packages

Use the approved baseline and conditional dependencies from:

```text
references/technology-stack.md
```

Resolve package versions at implementation time.

Do not copy stale versions from examples or historical template content.

Before installing or updating a dependency, follow
`.claude/rules/dependencies.md`.

`technology-stack.md` defines the approved technologies; `dependencies.md`
defines package version, security, and license policy.

### 7. Add Test Projects

Create only useful test projects.

Backend .NET test projects live beside the production projects under `src/`.

Typical locations are:

```text
src/
    ProjectName.Domain.Tests/
    ProjectName.Application.Tests/
    ProjectName.IntegrationTests/
```

Add ProjectName.Infrastructure.Tests or another focused backend test project
only when a concrete testing need justifies it.

Use NUnit for .NET tests according to the repository testing policy.

Angular unit/component tests remain inside the Angular workspace under
frontend/ProjectName.Web/, normally colocated with the frontend code according
to current Angular conventions.

Frontend E2E tests, when used, also remain under the frontend workspace, for
example:

```text
frontend/ProjectName.Web/e2e/
```

Detailed testing strategy belongs to the testing and angular skills.

### 8. Create Angular Application

When frontend scope exists, create the Angular workspace under:

```text
frontend/ProjectName.Web/
```

Use the baseline Angular configuration from `technology-stack.md`.

Detailed Angular architecture belongs to the `angular` skill.

Do not duplicate Angular feature-layout rules here.

### 9. Add Minimal Internal Directories

Inside each project, create directories required by the initial implementation.

Do not generate empty directory trees for hypothetical future capabilities.

### 10. Configure Development Persistence

For SQL Server projects, create the normal Development configuration described
in `technology-stack.md`.

Development data seeding follows the `ef-core` skill.

Do not add Production credentials or Production migration automation.

### 11. Verify

Before declaring scaffolding complete:

```text
restore backend dependencies
build the .NET solution
build the Angular application when present
run relevant tests when present
inspect project references
inspect the final repository tree
confirm Development configuration is valid
```

Use the `verify` skill for the final completion workflow.

Do not report successful scaffolding when the generated solution does not build.

## Adding a Project Later

When adding a project to an existing solution:

1. Determine whether a new assembly is actually justified.
2. Identify its architectural/technical responsibility.
3. Add only the minimum required references.
4. Preserve dependency direction.
5. Update shared build/package configuration when necessary.
6. Build and test the affected solution.

Do not split code into additional projects merely to make the architecture look
more sophisticated.

## Reorganizing an Existing Solution

Before reorganizing:

1. Inspect the current tree and project references.
2. Identify a concrete structural problem.
3. Preserve behavior.
4. Move code in small reviewable steps.
5. Update namespaces/references consistently.
6. Avoid unrelated feature work.
7. Verify after restructuring.

Do not force an established project to match this template when doing so provides
no concrete value.

## Do Not Introduce by Default

Do not scaffold additional architecture or infrastructure without an actual
requirement.

Examples include:

```text
MediatR
CQRS handler trees
Vertical Slice Architecture
Domain Events
Specifications
SharedKernel
BuildingBlocks
EventBus
Messaging
Microservices
Kubernetes
cloud-specific projects
state-management frameworks
```

Docker is optional and should be introduced only when the project needs
containerization.

Likewise, do not create generic `Common`, `Helpers`, or `Utils` directories
without a cohesive responsibility.

## Completion

A project-structure task is complete when:

- the physical structure matches the requested scope;
- project references respect architectural boundaries;
- approved baseline technology is used;
- no unnecessary projects or speculative directories were introduced;
- backend builds;
- frontend builds when present;
- relevant tests pass;
- local Development configuration is usable;
- no unrelated code was reorganized.