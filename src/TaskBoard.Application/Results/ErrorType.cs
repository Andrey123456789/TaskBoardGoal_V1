namespace TaskBoard.Application.Results;

/// <summary>
/// Categories of expected use-case failures. The API layer maps each category to an HTTP status.
/// </summary>
public enum ErrorType
{
    /// <summary>The request does not satisfy the input contract (missing or malformed fields).</summary>
    Validation,

    /// <summary>The addressed resource does not exist.</summary>
    NotFound,

    /// <summary>The request conflicts with the current state of the data.</summary>
    Conflict,

    /// <summary>The request is well-formed but semantically invalid (for example, it references missing data).</summary>
    Unprocessable,
}
