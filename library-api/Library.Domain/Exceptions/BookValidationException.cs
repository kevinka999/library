namespace Library.Domain.Exceptions;

public sealed class BookValidationException(
    IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more Book fields are invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
