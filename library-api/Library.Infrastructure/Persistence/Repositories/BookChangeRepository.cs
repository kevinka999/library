using System.Globalization;
using System.Text.Json;
using Library.Application.Abstractions;
using Library.Domain.Books;
using Library.Infrastructure.Persistence.Records;

namespace Library.Infrastructure.Persistence.Repositories;

internal sealed class BookChangeRepository(
    LibraryDbContext database,
    BookRepository bookRepository)
    : IBookChangeRepository
{
    public void AddRange(IReadOnlyCollection<BookChange> changes)
    {
        database.BookChanges.AddRange(changes.Select(ToPersistence));
    }

    private BookChangeRecord ToPersistence(BookChange change)
    {
        var changedField = change.ChangedField switch
        {
            BookField.Title => "title",
            BookField.ShortDescription => "shortDescription",
            BookField.PublishDate => "publishDate",
            BookField.Authors => "authors",
            _ => throw new ArgumentOutOfRangeException(
                nameof(change),
                change.ChangedField,
                "Unknown Book field.")
        };

        var oldValue = change.OldValue is null
            ? null
            : JsonSerializer.SerializeToDocument(
                change.OldValue,
                change.OldValue.GetType());

        var newValue = JsonSerializer.SerializeToDocument(
            change.NewValue,
            change.NewValue.GetType());

        return new BookChangeRecord(
            bookRepository.GetPersistenceRecord(change.Book),
            change.ChangeSetId,
            changedField,
            oldValue,
            newValue,
            change.ChangedAt);
    }

    private BookChange ToDomain(BookChangeRecord record)
    {
        var changedField = record.ChangedField switch
        {
            "title" => BookField.Title,
            "shortDescription" => BookField.ShortDescription,
            "publishDate" => BookField.PublishDate,
            "authors" => BookField.Authors,
            _ => throw new ArgumentException(
                $"'{record.ChangedField}' is not a valid persisted Book field.",
                nameof(record))
        };

        object? oldValue = record.OldValue is null
            ? null
            : changedField switch
            {
                BookField.Title or BookField.ShortDescription =>
                    record.OldValue.RootElement.GetString()!,
                BookField.PublishDate => DateOnly.ParseExact(
                    record.OldValue.RootElement.GetString()!,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture),
                BookField.Authors => record.OldValue.Deserialize<string[]>()!,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(record),
                    changedField,
                    "Unknown Book field.")
            };

        object newValue = changedField switch
        {
            BookField.Title or BookField.ShortDescription =>
                record.NewValue.RootElement.GetString()!,
            BookField.PublishDate => DateOnly.ParseExact(
                record.NewValue.RootElement.GetString()!,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture),
            BookField.Authors => record.NewValue.Deserialize<string[]>()!,
            _ => throw new ArgumentOutOfRangeException(
                nameof(record),
                changedField,
                "Unknown Book field.")
        };

        return new BookChange(
            bookRepository.GetDomain(record.Book),
            record.ChangeSetId,
            changedField,
            oldValue,
            newValue,
            record.ChangedAt,
            record.Id);
    }
}
