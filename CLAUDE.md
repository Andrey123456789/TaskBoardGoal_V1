# Project Instructions

This repository implements the TaskBoard application.

The project uses ASP.NET Core + Angular and follows the architecture, coding,
testing, security, dependency, and workflow guidance defined under `.claude/`.

## Product Specification

`TASKBOARD-SPEC.md` is the authoritative source for TaskBoard functional
requirements and public behavior.

Before planning or implementing a TaskBoard feature, read the relevant parts of
`TASKBOARD-SPEC.md`.

Do not silently change, weaken, reinterpret, or extend the specification.

When implementation details are not prescribed by the specification, follow the
repository's architecture, rules, skills, and established code conventions.

If a consequential product requirement is genuinely ambiguous or internally
inconsistent, ask before choosing behavior.

Do not add product features that are explicitly out of scope in
`TASKBOARD-SPEC.md`.

## Core Defaults

- Use Clean Architecture as the default architectural style.
- Do not change the architectural style or introduce major architectural
  patterns without explicit approval.
- Prefer simple, maintainable solutions over unnecessary abstractions.
- Do not introduce frameworks, libraries, infrastructure, or architectural
  patterns without a concrete need.
- Keep changes focused on the requested scope.
- Preserve existing behavior unless the task or `TASKBOARD-SPEC.md` explicitly
  requires changing it.

## Working Agreement

- Inspect the relevant existing code before making non-trivial changes.
- Follow applicable rules from `.claude/rules/`.
- Use relevant skills from `.claude/skills/` for specialized procedures and
  technical guidance.
- Before adding or updating any NuGet or npm dependency, follow
  `.claude/rules/dependencies.md`.
- Before declaring implementation work complete, apply the `verify` workflow.
- Do not perform commit, push, merge, rebase, reset, force-push, branch deletion,
  or other repository-changing Git operations unless explicitly requested.
- If a consequential architectural or technical decision is genuinely ambiguous,
  ask before implementing.