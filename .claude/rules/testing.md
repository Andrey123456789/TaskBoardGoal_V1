---
paths:
  - "src/**/*Tests/**/*.cs"
  - "src/**/*Tests/*.csproj"
---

# Testing Rules

## Framework

Use NUnit as the default .NET test framework.

Follow existing repository conventions when working in an established project
that deliberately uses another framework.

Do not introduce a second test framework without a concrete reason.

## Test Level

Choose the narrowest test level that meaningfully verifies the behavior.

- Use unit tests for isolated business/domain/application logic.
- Use integration tests when framework wiring, HTTP, EF Core, persistence,
  serialization, authentication, or component interaction is what matters.
- Do not require integration tests for every behavior.
- Do not require unit tests for behavior that is naturally verified through an
  integration boundary.

There is no required unit/integration test ratio.

## Behavior Over Implementation

Test observable behavior rather than private implementation details.

Do not make tests depend unnecessarily on:

- private methods;
- exact internal call sequences;
- specific implementation classes;
- incidental refactoring details.

Interaction verification is appropriate when the interaction itself is part of
the behavior being specified.

## Test Doubles

Use fakes, stubs, or mocks when they make an isolated test clearer.

Do not mock every dependency mechanically.

Do not forbid mocking project-owned abstractions when isolation is useful.

When important behavior depends on real EF Core/provider/HTTP semantics, prefer
an integration test instead of reproducing that behavior through mocks.

## Database Tests

Do not use EF Core InMemory while claiming to verify relational database
behavior.

Use the actual provider when provider fidelity matters.

Testcontainers is a valid option for isolated real-provider tests, but it is
not mandatory for every project or integration test.

Never connect automated tests to Production data.

## Isolation

Tests must be deterministic and independent of execution order.

Do not rely on:

- shared mutable state between unrelated tests;
- wall-clock timing when `TimeProvider` can make time deterministic;
- uncontrolled external services;
- Development or Production seed data.

Tests own their own setup.

## Behavioral Verification

If a temporary behavioral probe created during implementation establishes that
a feature works, that behavior must remain represented by maintained automated
test coverage unless equivalent coverage already exists.

The temporary mechanism itself does not need to remain.

Detailed guidance belongs to the `testing` skill.

## Naming

Use descriptive test names that communicate scenario and expected behavior.

A useful default is:

```text
MethodOrScenario_Condition_ExpectedBehavior