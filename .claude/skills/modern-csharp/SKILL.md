---
name: modern-csharp
description: >
  Guidance for deliberately applying modern C# 14 features such as primary
  constructors, collection expressions, records, pattern matching, field-backed
  properties, extension members, required members, and raw strings.
  Use when explicitly modernizing C# code or when a task specifically involves
  recent C# language features.
---

# Modern C#

## Principle

Use modern language features when they make the code clearer, safer, or simpler.

Do not modernize code merely to demonstrate newer syntax.

Follow `coding-style.md` and the conventions already established by the project.

## Primary Constructors

Primary constructors are useful for simple dependency injection:

```csharp
internal sealed class OrderService(
    IOrderRepository orders,
    IUnitOfWork unitOfWork)
{
}
```

Prefer a normal constructor when initialization, validation, field naming, or
debugging becomes clearer that way.

Do not convert existing constructors mechanically.

## Collection Expressions

Use collection expressions when the target type is clear:

```csharp
string[] names = ["Alice", "Bob"];

List<int> ids = [1, 2, 3];
```

Do not replace every existing collection initializer solely for modernization.

## Records

Records are useful when value-oriented semantics are intentional.

Good candidates can include:

- immutable transport DTOs;
- genuine Value Objects;
- immutable messages.

Do not make every DTO or Domain entity a record automatically.

Entities with identity and lifecycle normally remain classes unless the model
genuinely benefits from record semantics.

## Required Members

Use `required` when initialization of a property is part of the type's valid
construction contract:

```csharp
public required string Name { get; init; }
```

Do not use `required` as a substitute for meaningful constructor/domain
validation where invariants must be protected.

## Pattern Matching

Pattern matching is useful when it makes branching clearer:

```csharp
return status switch
{
    OrderStatus.New => ...,
    OrderStatus.Completed => ...,
    _ => ...
};
```

Avoid deeply nested patterns that are harder to understand than straightforward
conditions.

## `field` Keyword

C# 14 supports field-backed properties using `field`.

```csharp
public string Name
{
    get;
    set => field =
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Name is required.")
            : value.Trim();
}
```

Use it when it removes an otherwise trivial backing field.

Do not rewrite manual backing fields that already contain clear or substantial
logic solely to use the new keyword.

## Extension Members

C# 14 supports extension-member blocks.

They are useful when extension properties or grouped extension behavior
genuinely improve an API.

```csharp
public static class OrderExtensions
{
    extension(Order order)
    {
        public bool IsFinished =>
            order.Status == OrderStatus.Completed;
    }
}
```

Traditional extension methods remain valid.

Do not convert ordinary extension methods solely because the newer syntax
exists.

## Raw String Literals

Raw string literals are useful for readable embedded text:

```csharp
var json = """
{
  "enabled": true
}
""";
```

They are especially useful for:

- test JSON;
- templates;
- SQL that genuinely belongs in code.

Do not use raw SQL merely because raw string literals make it convenient.

## `var`

Use `var` when the resulting type is obvious or not important to understanding
the code.

Use an explicit type when it improves readability.

Follow the existing project's convention.

## Immutability

Prefer immutability when it represents the model naturally.

Do not force immutable representations onto framework models or workflows where
controlled mutation is simpler and clearer.

## Span and Allocation-Oriented APIs

`Span<T>`, `ReadOnlySpan<T>`, stack allocation, pooling, and similar features are
performance tools.

Use ordinary arrays, strings, and collections by default.

Introduce allocation-oriented techniques when profiling or a clearly hot path
justifies the added complexity.

Follow `performance.md`.

## Compatibility

Before using a language feature, confirm that the project's configured language
version supports it.

Do not silently change `LangVersion` or target framework merely to use newer
syntax.

## Modernization Tasks

When explicitly asked to modernize code:

1. preserve behavior;
2. keep the change focused;
3. prefer readability improvements;
4. build and test after the change;
5. do not mix unrelated architecture refactoring into syntax modernization.

## Anti-Patterns

Avoid:

- newest syntax for its own sake;
- converting every class to a record;
- `record struct` merely to avoid allocations;
- `Span<T>` without a performance reason;
- deeply nested patterns;
- mass constructor conversion;
- changing language/framework versions without approval.