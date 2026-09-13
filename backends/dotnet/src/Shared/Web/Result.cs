namespace StackBraid.Shared.Web;

/// <summary>
/// A handler's outcome: exactly one of a value or an <see cref="AppError"/>.
/// Endpoints translate a failure into the contract's Problem envelope
/// (see <c>ProblemDetailsMapper</c>) and a success into the response schema
/// directly — the contract has no generic success wrapper, only DTOs.
/// </summary>
public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public AppError? Error { get; }

    private Result(bool isSuccess, T? value, AppError? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(AppError error) => new(false, default, error);

    public static implicit operator Result<T>(AppError error) => Failure(error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<AppError, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error!);
}

/// <summary>Non-generic form for a use case that returns nothing on success.</summary>
public readonly struct Result
{
    public bool IsSuccess { get; }
    public AppError? Error { get; }

    private Result(bool isSuccess, AppError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(AppError error) => new(false, error);

    public static implicit operator Result(AppError error) => Failure(error);

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<AppError, TResult> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(Error!);
}
