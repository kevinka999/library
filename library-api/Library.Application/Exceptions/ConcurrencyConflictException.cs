namespace Library.Application.Exceptions;

public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(Exception innerException)
        : base("A concurrent modification was detected.", innerException)
    {
    }
}
