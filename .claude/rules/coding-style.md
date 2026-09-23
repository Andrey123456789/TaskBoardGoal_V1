---
paths:
  - "src/**/*.cs"
---

# C# Coding Style

## General Principle

Follow the repository's existing conventions and `.editorconfig`.

Prefer modern C# features when they improve clarity, but do not rewrite working
code solely to use newer syntax.

Consistency and readability are more important than maximizing use of language
features.

## Files and Namespaces

- Prefer file-scoped namespaces for new files when consistent with the project.
- Normally keep one primary top-level type per file.
- The file name should normally match the primary type name.
- Small private or nested supporting types may remain with the owning type when
  that improves locality.

Do not reorganize an existing file merely to satisfy a preferred member ordering.

## Constructors

Primary constructors are appropriate when dependency injection or simple
initialization remains clear.

```csharp
public sealed class OrderService(
    IOrderRepository orders,
    IUnitOfWork unitOfWork)
{
}
```

Use a regular constructor when:

- validation or initialization is non-trivial;
- explicit fields improve readability;
- the existing codebase uses that style consistently;
- primary-constructor parameter capture would make the class harder to understand.

Do not convert constructors mechanically.

## Records and Classes

Use records when value semantics and immutability are desirable, for example
many DTOs and genuine Value Objects.

Use classes when identity, mutable lifecycle, inheritance, or framework behavior
makes class semantics clearer.

Do not turn every DTO or Domain type into a record automatically.

## Visibility and Inheritance

Keep implementation details non-public when they do not need to form part of
the project's public API.

Use `sealed` when the type is deliberately not intended for inheritance and the
modifier communicates useful design intent.

Do not add `sealed` merely as a speculative JIT optimization.

## Modern Syntax

Use collection expressions, pattern matching, switch expressions, target-typed
construction, and other modern C# features when they make the code easier to
read.

Do not prefer newer syntax when the resulting code is less obvious.

## Type Inference

Use `var` when the type is obvious from the right-hand side or the exact type is
not important to understanding the code.

Use an explicit type when it improves clarity.

## Async Naming

Methods returning `Task` or `ValueTask` should normally use the `Async` suffix,
following standard .NET conventions and existing interface contracts.

Do not rename framework implementations or established APIs solely to add the
suffix when doing so would break an existing contract.

## Naming

Use standard .NET naming conventions:

- PascalCase for types, methods, properties, and public members;
- camelCase for parameters and local variables;
- meaningful names over abbreviations.

Private-field naming should follow the repository's established convention.

## Scope Discipline

Do not perform style-only rewrites outside the requested change unless explicitly
asked.

Avoid mixing large formatting or modernization changes with functional work.