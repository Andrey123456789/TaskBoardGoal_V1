---
name: testing
description: >
  Testing guidance for .NET applications using NUnit. Covers unit tests,
  ASP.NET Core integration tests with WebApplicationFactory, persistence tests,
  isolated test databases, Testcontainers when provider fidelity matters,
  TimeProvider testing, external dependency stubs, and test maintenance.
  Use when writing, reviewing, debugging, or organizing automated tests.
---

# Testing

## Core Principles

1. **Use NUnit by default** for .NET automated tests.
2. **Choose the narrowest useful test level** — use unit tests for isolated logic and integration tests when interaction between real application components is what matters.
3. **Test behavior rather than implementation details** — tests should normally survive internal refactoring.
4. **Keep tests deterministic and isolated** — tests must not depend on production data, execution order, wall-clock time, or uncontrolled external systems.
5. **Do not build test infrastructure without a concrete testing need** — a small project does not require an enterprise-sized test harness.
6. **Preserve verification as tests** — Any new behavioral check used to prove that an implementation works must be represented by a maintained automated test, unless existing test coverage already verifies the same behavior.

No fixed ratio of unit, integration, and end-to-end tests is required.

Test the risks that matter for the current project.

## NUnit Basics

Use normal NUnit conventions.

```csharp
[TestFixture]
public sealed class PriceCalculatorTests
{
    [TestCase(2, 10, 20)]
    [TestCase(3, 5, 15)]
    public void Calculate_WithValidInput_ReturnsExpectedTotal(
        int quantity,
        decimal price,
        decimal expected)
    {
        // Arrange
        var calculator = new PriceCalculator();

        // Act
        var result = calculator.Calculate(quantity, price);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }
}
```

Arrange / Act / Assert is a useful default structure, but comments are not
mandatory when the phases are already obvious.

## Unit Tests

Use unit tests for behavior that can be meaningfully exercised without
infrastructure.

Good candidates include:

- domain invariants;
- calculations;
- state transitions;
- validation logic;
- application orchestration with simple fakes or mocks when isolation provides
  useful feedback.

Do not mock every collaborator mechanically.

Mock or fake architectural boundaries when doing so makes the behavior under
test clearer.

Do not test that a private method was called or that implementation details
happen in a particular sequence unless that sequence is itself observable
behavior.

## Application Service Tests

Application services may be unit-tested by replacing repository and external
service abstractions with controlled test doubles.

Focus assertions on the use-case result and meaningful side effects.

A mock verification such as "repository method X was called once" is useful only
when that interaction is itself part of the contract. Do not make interaction
verification the default assertion style.

If a service's important behavior depends heavily on EF Core semantics,
transaction behavior, DI wiring, serialization, or HTTP, prefer an integration
test instead of reproducing the whole stack with mocks.

## ASP.NET Core Integration Tests

Use `WebApplicationFactory<Program>` when the test should exercise the real HTTP
pipeline.

This can cover:

- routing;
- model binding;
- serialization;
- middleware;
- authentication/authorization wiring;
- dependency injection;
- Application services;
- repositories;
- EF Core;
- database persistence.

Example NUnit test fixture:

```csharp
[TestFixture]
public sealed class OrdersApiTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
            });

        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task CreateOrder_WithValidRequest_ReturnsCreated()
    {
        var request = new CreateOrderRequest(
            "customer-1");

        var response = await _client.PostAsJsonAsync(
            "/api/orders",
            request);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));
    }
}
```

The `Testing` environment must be configured to use isolated test resources.

Never allow an integration test to connect to a development or production
database accidentally.

If top-level statements make `Program` inaccessible to the test project, expose
it as required by `WebApplicationFactory`, for example with a public partial
`Program` declaration.

## Test Database Strategy

Use the database strategy that matches what the test is intended to prove.

### Provider fidelity matters

Use the actual database provider when the test covers behavior such as:

- provider-specific SQL;
- constraints;
- transactions;
- locking or concurrency;
- indexes;
- raw SQL;
- provider-specific mappings;
- migrations.

Testcontainers is a good option when an isolated real database is useful.

It is not mandatory for every integration test or every project.

### Provider fidelity does not matter

A lighter isolated relational database may be acceptable for tests whose purpose
is primarily application/API integration.

Be explicit about the trade-off.

Do not claim that a test verifies SQL Server or PostgreSQL behavior when it runs
against a different provider.

### EF Core InMemory provider

Do not use EF Core InMemory as a substitute for relational database behavior.

It may be acceptable for a narrowly scoped test where relational semantics are
irrelevant, but a fake repository is often clearer for a true unit test.

## Database State

Tests must start from a known state.

Choose a simple strategy appropriate to the suite:

- recreate or reset the isolated test database;
- use transactions when their semantics fit;
- seed only data required by the test;
- use unique identifiers to prevent collisions.

Do not make tests depend on the order in which other tests execute.

Do not modify production data.

## Persistence Tests

Test repository implementations or EF mappings when there is meaningful
persistence behavior to verify.

Examples include:

- custom mappings;
- query filters;
- projections;
- non-trivial queries;
- relationships;
- concurrency behavior;
- provider-specific behavior.

Do not write one repository test for every trivial `Add` and `Get` method merely
to increase coverage numbers.

## Time-Dependent Code

Use `TimeProvider` rather than reading the system clock directly from logic that
needs deterministic tests.

Use `FakeTimeProvider` when controlling time is useful.

```csharp
var time = new FakeTimeProvider(
    new DateTimeOffset(
        2026, 1, 1,
        0, 0, 0,
        TimeSpan.Zero));

time.Advance(TimeSpan.FromDays(1));
```

## External Services

Do not call uncontrolled external services from normal automated tests.

At the relevant boundary, use one of:

- a fake or mock abstraction for unit tests;
- a stub `HttpMessageHandler`;
- WireMock or another local fake server for HTTP integration;
- a dedicated sandbox only when the test explicitly targets that integration.

Keep external-system tests separate from ordinary fast test runs when they are
slow or unreliable.

## Test Data

Keep test data explicit and minimal.

Introduce test builders, fixtures, or factories when repeated setup becomes
hard to read.

Do not introduce a generic test-data framework for a few simple objects.

## Test Naming

Use descriptive names that state behavior.

A useful convention is:

```text
MethodOrScenario_StateOrCondition_ExpectedBehavior
```

For example:

```csharp
CreateOrder_WithValidRequest_ReturnsCreated
CreateOrder_WithMissingCustomer_ReturnsValidationError
GetOrder_WithUnknownId_ReturnsNotFound
```

Follow an existing project naming convention if one is already established.

## Regression Tests

When fixing a meaningful bug, add a regression test when it provides useful
protection against recurrence.

The test should reproduce the failure before the fix and pass after it.

Do not add a regression test for a problem that cannot realistically recur or
is already covered by an existing test.

## Verification Performed During Implementation

Behavioral checks created or used during implementation must not exist only as
disposable verification.

If Claude creates or performs a new behavioral check to verify that an
implementation works, the behavior verified by that check must be represented
by a maintained automated test before the task is considered complete.

Examples include:

- temporary HTTP requests used to verify an endpoint;
- temporary PowerShell or shell scripts that exercise application behavior;
- ad-hoc test methods created only to validate the current implementation;
- manual database checks used to confirm persistence behavior;
- temporary regression probes used while fixing a bug.

The permanent test does not need to preserve the temporary verification
mechanism itself.

For example, an ad-hoc `curl`, PowerShell, or REST request may be replaced by an
NUnit integration test using `WebApplicationFactory`.

If an existing automated test already covers the same behavior adequately, do
not add a duplicate test. Verify that the existing test actually exercises the
behavior and keep the existing coverage.

Builds, compiler checks, static analysis, formatting, and running an existing
test suite are verification activities, but they do not by themselves require
new tests.

Do not declare an implementation complete based on a behavioral check that
exists only outside the maintained automated test suite.

## Smoke Tests

Use "smoke test" for a small set of fast checks that establish that a built or
deployed application is basically operational.

Do not use the term as a synonym for every integration test.

## Coverage

Coverage is diagnostic information, not a quality target by itself.

Do not generate low-value tests merely to increase a percentage.

Prioritize meaningful behavior, important edge cases, and risky integrations.

## Snapshot Testing

Snapshot testing may be useful for large stable outputs where explicit
assertions become noisy.

Do not introduce a snapshot library by default.

For small or important response contracts, explicit assertions are usually
clearer.

## Anti-Patterns

Avoid:

- testing private implementation details;
- mocking every class in the system;
- hitting production resources;
- shared mutable state between tests;
- assertions that merely repeat the implementation;
- assertion-free tests unless the behavior under test is explicitly "does not
  throw";
- arbitrary delays such as `Task.Delay` to make timing-sensitive tests pass;
- Testcontainers when no provider fidelity is required;
- EF InMemory while claiming relational behavior is covered;
- preserving temporary Claude probes as permanent tests without review.

## Decision Guide

| Scenario | Default approach |
|---|---|
| Pure business rule | NUnit unit test |
| Application orchestration | Unit test with controlled boundary doubles when useful |
| Full API pipeline | `WebApplicationFactory<Program>` |
| Important EF/provider behavior | Integration test using the actual provider |
| Simple API integration where provider specifics are irrelevant | Lightweight isolated relational test DB may be sufficient |
| Time-dependent behavior | `TimeProvider` / `FakeTimeProvider` |
| External HTTP dependency | Stub handler or local fake server |
| Reproduced bug | Focused regression test |
| Temporary Claude behavioral check | Convert to maintained coverage or confirm equivalent existing coverage |
| Deployment/basic health | Smoke test |