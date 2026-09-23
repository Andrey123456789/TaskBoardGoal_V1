using System.Diagnostics.CodeAnalysis;

namespace TaskBoard.Application.Results;

/// <summary>
/// Outcome of a use case that either succeeds or fails with an expected <see cref="Results.Error"/>.
/// </summary>
public class Result
{
    private static readonly Result SuccessResult = new(null);

    protected Result(Error? error)
    {
        Error = error;
    }

    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public static Result Success() => SuccessResult;

    public static implicit operator Result(Error error) => new(error);
}

/// <summary>
/// Outcome of a use case that produces a value on success.
/// </summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value)
        : base(null)
    {
        _value = value;
    }

    private Result(Error error)
        : base(error)
    {
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    public static Result<T> Success(T value) => new(value);

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => new(error);
}
