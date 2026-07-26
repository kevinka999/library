using Library.Domain.Books;

namespace Library.Application.Abstractions;

public interface IBookRepository
{
    Task<Book?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<PagedBooks> SearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(Book book);

    void Update(Book book, long expectedVersion);
}

public sealed record PagedBooks(
    IReadOnlyList<Book> Items,
    long TotalCount);
