using Library.Application.Abstractions;
using Library.Domain.Books;
using Library.Infrastructure.Persistence.Records;

namespace Library.Infrastructure.Persistence.Repositories;

internal sealed class BookRepository(LibraryDbContext database) : IBookRepository
{
    private readonly Dictionary<Book, BookRecord> _recordsByDomain =
        new(ReferenceEqualityComparer.Instance);

    private readonly Dictionary<BookRecord, Book> _domainByRecord =
        new(ReferenceEqualityComparer.Instance);

    public void Add(Book book)
    {
        var record = ToPersistence(book);
        Track(book, record);
        database.Books.Add(record);
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

    private void Track(Book book, BookRecord record)
    {
        _recordsByDomain[book] = record;
        _domainByRecord[record] = book;
    }
}
