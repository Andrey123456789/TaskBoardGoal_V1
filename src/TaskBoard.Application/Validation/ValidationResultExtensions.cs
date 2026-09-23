using FluentValidation.Results;
using TaskBoard.Application.Results;

namespace TaskBoard.Application.Validation;

internal static class ValidationResultExtensions
{
    /// <summary>
    /// Converts failures into a validation <see cref="Error"/> keyed by camel-cased property path so
    /// that keys match the JSON request contract.
    /// </summary>
    public static Error ToError(this ValidationResult result)
    {
        var errors = result.Errors
            .GroupBy(failure => ToCamelCase(failure.PropertyName))
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

        return Error.Validation(errors);
    }

    private static string ToCamelCase(string propertyPath) =>
        string.IsNullOrEmpty(propertyPath) || char.IsLower(propertyPath[0])
            ? propertyPath
            : char.ToLowerInvariant(propertyPath[0]) + propertyPath[1..];
}
