---
name: code-review
description: >
  Review completed .NET and Angular changes for correctness, architecture
  boundaries, security, persistence/API risks, tests, maintainability, and
  credible performance issues. Use for code reviews, PR reviews, reviewing a
  diff, or an independent defect-focused review of completed implementation.
---

# Code Review

## Goal

Review the actual changed behavior and identify concrete defects or risks.

Prioritize:

1. correctness;
2. architectural boundary violations;
3. data integrity and security;
4. concurrency and integration behavior;
5. missing meaningful tests;
6. maintainability;
7. performance issues supported by evidence.

Do not bury important findings under style preferences.

Code review is not the completion checklist. Use `verify` to run the final
build/test/diff verification.

## Step 1: Establish Scope

Determine what is being reviewed:

- explicit files supplied by the user;
- current working-tree diff;
- branch/PR diff;
- a named feature or module.

Inspect the changed code and enough surrounding code to understand its behavior.

Do not review isolated lines without understanding their callers and dependencies.

## Step 2: Understand Intent

Identify:

- the behavior being added or changed;
- relevant architecture rules;
- expected error cases;
- persistence or external-system effects;
- tests intended to protect the behavior.

Read relevant project rules/skills when they govern the changed area.

## Step 3: Review Correctness

Look for concrete behavioral problems such as:

- wrong conditions or state transitions;
- null/empty edge cases;
- incorrect async behavior;
- incorrect cancellation handling;
- incorrect error mapping;
- resource lifetime problems;
- unintended side effects;
- race conditions where shared state exists.

Do not invent hypothetical failures without a plausible execution path.

## Step 4: Architecture

For this template, verify relevant Clean Architecture boundaries:

- Domain does not depend on Application, Infrastructure, or API.
- Application does not depend on Infrastructure or EF Core.
- Controllers remain thin.
- Application Services orchestrate use cases.
- persistence is accessed through specific repository abstractions.
- `DbContext`, `DbSet`, `IQueryable`, and EF-specific APIs do not leak from Infrastructure.
- repositories do not independently commit normal use-case changes.
- `IUnitOfWork` remains the Application-facing commit boundary.

Do not report a violation merely because the implementation differs from a
pattern not adopted by this project.

## Frontend Changes

When Angular code is in scope, apply the `angular` skill.

Review relevant concerns such as:

- feature boundaries and component responsibility;
- HTTP calls being kept behind an appropriate data-access boundary;
- subscription/reactive-state lifetime;
- frontend error handling;
- route guards not being treated as server-side authorization;
- unnecessary global/shared state;
- meaningful frontend test coverage.

Do not apply backend Clean Architecture project-boundary rules mechanically to
Angular source code.

## Step 5: Data and Security

Review changed data-access and security-sensitive code carefully.

Check, when relevant:

- transaction/commit behavior;
- destructive data changes;
- raw SQL parameterization;
- authorization;
- input trust boundaries;
- secrets;
- sensitive logging;
- concurrency;
- migrations;
- development seed-data consistency when Domain/persistence changes affect seeded entities;
- cache consistency.

When a migration or Domain-model change affects entities represented by
development seed data, verify that `DbSeeder` was reviewed and updated when
necessary.

## Step 6: Integrations

For external calls, check:

- cancellation propagation;
- timeout strategy;
- idempotency where relevant;
- retry safety;
- failure translation;
- disposal/lifetime behavior.

Do not demand retries for operations that are not safe to retry.

## Step 7: Tests

Determine whether changed behavior has useful automated coverage.

Focus on behavior rather than line count.

For bugs, look for a regression test when recurrence is plausible.

For behavioral probes created during implementation, follow the `testing` skill:
the verified behavior should remain represented in maintained automated tests.

Do not request duplicate tests when equivalent coverage already exists.

## Step 8: Performance

Report performance concerns when they are credible, for example:

- obvious N+1 database access;
- unbounded materialization;
- repeated expensive external calls;
- blocking async I/O;
- obvious high-volume allocation in a demonstrated hot path.

Do not recommend compiled queries, pooling, caching, `ValueTask`, or similar
micro-optimizations without a reason.

## Optional Tooling

Use compiler diagnostics, analyzers, IDE/Roslyn tooling, or repository-specific
analysis tools when available.

Tool output is evidence, not authority.

Do not require an MCP server or a particular analyzer for the review to work.

Read the code before turning a tool warning into a review finding.

## Output

For each finding provide:

- severity;
- file/location;
- what is wrong;
- why it matters;
- the smallest reasonable fix.

Suggested severity:

### Critical

Data loss, security vulnerability, reliably broken core behavior, or another
issue that should block use/merge immediately.

### High

Likely defect or architectural/data-integrity issue that should normally be
fixed before merge.

### Medium

Real maintainability, robustness, or test gap worth addressing, but not a likely
immediate production failure.

### Low

Non-blocking improvement.

Do not manufacture findings to populate every severity.

If no meaningful problem is found, say so.

## Review Discipline

Do not:

- rewrite working code during a review unless asked;
- report formatting already owned by tooling as a defect;
- demand personal style preferences;
- assume every warning is a bug;
- require patterns the project has intentionally not adopted;
- praise filler solely to balance criticism.