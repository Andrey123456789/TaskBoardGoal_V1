namespace TaskBoard.Application.Results;

/// <summary>
/// An expected use-case failure with a stable machine-readable <see cref="Code"/>.
/// </summary>
public sealed record Error(ErrorType Type, string Code, string Message)
{
    private static readonly IReadOnlyDictionary<string, string[]> NoValidationErrors =
        new Dictionary<string, string[]>();

    /// <summary>Field-level messages, populated only for <see cref="ErrorType.Validation"/> errors.</summary>
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; init; } = NoValidationErrors;

    public static Error Validation(IReadOnlyDictionary<string, string[]> errors) =>
        new(ErrorType.Validation, "validation_failed", "One or more validation errors occurred.")
        {
            ValidationErrors = errors,
        };

    public static Error NotFound(string code, string message) => new(ErrorType.NotFound, code, message);

    public static Error Conflict(string code, string message) => new(ErrorType.Conflict, code, message);

    public static Error Unprocessable(string code, string message) => new(ErrorType.Unprocessable, code, message);
}
