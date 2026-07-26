namespace Library.Application.Common;

public sealed record Result<T>
{
    private Result(T? value, ApplicationError? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public ApplicationError? Error { get; }

    public bool IsSuccess => Error is null;

    public static Result<T> Success(T value) => new(value, null);

    public static Result<T> Failure(ApplicationError error) => new(default, error);
}
