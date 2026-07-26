using Library.Application.Abstractions;
using Library.Domain.Books;
using Library.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

internal sealed class BookRepository(LibraryDbContext database) : IBookRepository
{
    private readonly Dictionary<Book, BookRecord> _recordsByDomain =
        new(ReferenceEqualityComparer.Instance);

    private readonly Dictionary<BookRecord, Book> _domainByRecord =
        new(ReferenceEqualityComparer.Instance);

    public async Task<Book?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var record = await database.Books
            .SingleOrDefaultAsync(book => book.Id == id, cancellationToken);

        return record is null ? null : GetDomain(record);
    }

    public async Task<PagedBooks> SearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var books = database.Books.AsNoTracking();

        if (search is not null)
        {
            var pattern = CreateLiteralContainsPattern(search);
            books = books.Where(book =>
                EF.Functions.ILike(book.Title, pattern, "\\")
                || EF.Functions.ILike(book.ShortDescription, pattern, "\\")
                || book.Authors.Any(author =>
                    EF.Functions.ILike(author, pattern, "\\")));
        }

        var totalCount = await books.LongCountAsync(cancellationToken);
        var records = await books
            .OrderBy(book => book.Title)
            .ThenBy(book => book.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedBooks(
            records.Select(ToDomain).ToArray(),
            totalCount);
    }

    public void Add(Book book)
    {
        var record = ToPersistence(book);
        Track(book, record);
        database.Books.Add(record);
    }

    public void Update(Book book, long expectedVersion)
    {
        var record = GetPersistenceRecord(book);
        var entry = database.Entry(record);

        entry.Property(persisted => persisted.Title).CurrentValue = book.Title;
        entry.Property(persisted => persisted.ShortDescription).CurrentValue =
            book.ShortDescription;
        entry.Property(persisted => persisted.PublishDate).CurrentValue = book.PublishDate;
        entry.Property(persisted => persisted.Authors).CurrentValue = book.Authors.ToArray();
        entry.Property(persisted => persisted.Version).OriginalValue = expectedVersion;
        entry.Property(persisted => persisted.Version).CurrentValue = book.Version;
    }

    internal BookRecord GetPersistenceRecord(Book book) =>
        _recordsByDomain.TryGetValue(book, out var record)
            ? record
            : throw new InvalidOperationException(
                "The Book must be staged through IBookRepository before its changes.");

    internal Book GetDomain(BookRecord record)
    {
        if (_domainByRecord.TryGetValue(record, out var book))
        {
            return book;
        }

        book = ToDomain(record);
        Track(book, record);
        return book;
    }

    internal void SynchronizeGeneratedIds()
    {
        foreach (var (book, record) in _recordsByDomain)
        {
            book.AssignId(record.Id);
        }
    }

    private static BookRecord ToPersistence(Book book) =>
        new(
            book.Title,
            book.ShortDescription,
            book.PublishDate,
            book.Authors.ToArray(),
            book.Version);

    private static Book ToDomain(BookRecord record) =>
        new(
            record.Id,
            record.Title,
            record.ShortDescription,
            record.PublishDate,
            record.Authors,
            record.Version);

    private static string CreateLiteralContainsPattern(string search) =>
        $"%{search
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)}%";

    private void Track(Book book, BookRecord record)
    {
        _recordsByDomain[book] = record;
        _domainByRecord[record] = book;
    }
}
