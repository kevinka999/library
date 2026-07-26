namespace Library.Application.Common;

public enum ApplicationErrorType
{
    Validation,
    NotFound,
    PreconditionFailed,
    PreconditionRequired
}

public sealed record ApplicationError(
    ApplicationErrorType Type,
    string Code,
    string Title,
    string Detail,
    IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public static ApplicationError Validation(
        string code,
        string detail,
        IReadOnlyDictionary<string, string[]> errors) =>
        new(ApplicationErrorType.Validation, code, "Validation failed", detail, errors);

    public static ApplicationError NotFound(string code, string detail) =>
        new(ApplicationErrorType.NotFound, code, "Resource not found", detail);

    public static ApplicationError PreconditionFailed(string code, string detail) =>
        new(ApplicationErrorType.PreconditionFailed, code, "Precondition failed", detail);

    public static ApplicationError PreconditionRequired(string code, string detail) =>
        new(ApplicationErrorType.PreconditionRequired, code, "Precondition required", detail);
}
