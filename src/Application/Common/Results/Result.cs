namespace Application.Common.Results;

/// <summary>
/// Discriminated union representing either a successful value of type <typeparamref name="T"/>
/// or an <see cref="Error"/>. Implicit operators allow handlers to return the raw value or
/// an <see cref="Error"/> without explicit wrapping.
/// </summary>
public sealed class Result<T>
{
    private Result(T value)     { Value = value; Error = Error.None; IsSuccess = true; }
    private Result(Error error) { Value = default; Error = error; IsSuccess = false; }

    /// <summary>The successful value; only valid when <see cref="IsSuccess"/> is <c>true</c>.</summary>
    public T? Value { get; }

    /// <summary>The error detail; only valid when <see cref="IsFailure"/> is <c>true</c>.</summary>
    public Error Error { get; }

    /// <summary><c>true</c> when the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary><c>true</c> when the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Implicitly wraps a value into a successful result.</summary>
    public static implicit operator Result<T>(T value)     => new(value);

    /// <summary>Implicitly wraps an error into a failed result.</summary>
    public static implicit operator Result<T>(Error error) => new(error);
}

/// <summary>
/// Discriminated union for void operations — represents either success or an <see cref="Error"/>.
/// </summary>
public sealed class Result
{
    private static readonly Result SuccessInstance = new(true, Error.None);

    private Result(bool isSuccess, Error error) { IsSuccess = isSuccess; Error = error; }

    /// <summary>The error detail; only valid when <see cref="IsFailure"/> is <c>true</c>.</summary>
    public Error Error { get; }

    /// <summary><c>true</c> when the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary><c>true</c> when the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Returns the shared success singleton.</summary>
    public static Result Success() => SuccessInstance;

    /// <summary>Creates a failed result carrying the given <paramref name="error"/>.</summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>Implicitly wraps an error into a failed result.</summary>
    public static implicit operator Result(Error error) => Failure(error);
}
