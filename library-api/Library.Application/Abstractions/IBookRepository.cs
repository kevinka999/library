using Library.Domain.Books;

namespace Library.Application.Abstractions;

public interface IBookRepository
{
    Task<Book?> GetByIdAsync(long id, CancellationToken cancellationToken);

    void Add(Book book);

    void Update(Book book, long expectedVersion);
}
