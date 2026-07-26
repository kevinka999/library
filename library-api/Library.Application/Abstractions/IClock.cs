namespace Library.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
