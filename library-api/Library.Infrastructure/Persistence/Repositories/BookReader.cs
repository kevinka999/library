using Library.Application.Abstractions;
using Library.Application.Handlers;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

internal sealed class BookReader(LibraryDbContext database) : IBookReader
{
    public Task<BookResult?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        database.Books
            .AsNoTracking()
            .Where(book => book.Id == id)
            .Select(book => new BookResult(
                book.Id,
                book.Title,
                book.ShortDescription,
                book.PublishDate,
                book.Authors,
                book.Version))
            .SingleOrDefaultAsync(cancellationToken);
}
