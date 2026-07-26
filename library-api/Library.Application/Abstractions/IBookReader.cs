using Library.Application.Handlers;

namespace Library.Application.Abstractions;

public interface IBookReader
{
    Task<BookResult?> GetByIdAsync(long id, CancellationToken cancellationToken);
}
