using System.Globalization;
using System.Text.Json;
using Library.Application.Abstractions;
using Library.Domain.Books;
using Library.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

internal sealed class BookChangeRepository(
    LibraryDbContext database,
    BookRepository bookRepository)
    : IBookChangeRepository
{
    public async Task<BookHistoryPage> ReadHistoryAsync(
        BookHistoryCriteria criteria,
        CancellationToken cancellationToken)
    {
        var matchingChanges = database.BookChanges
            .AsNoTracking()
            .Where(change => change.BookId == criteria.BookId);

        if (criteria.ChangedFields.Count > 0)
        {
            var fields = criteria.ChangedFields
                .Select(ToPersistence)
                .ToArray();
            matchingChanges = matchingChanges.Where(change =>
                fields.Contains(change.ChangedField));
        }

        if (criteria.ChangedFrom is not null)
        {
            matchingChanges = matchingChanges.Where(change =>
                change.ChangedAt >= criteria.ChangedFrom.Value);
        }

        if (criteria.ChangedBefore is not null)
        {
            matchingChanges = matchingChanges.Where(change =>
                change.ChangedAt < criteria.ChangedBefore.Value);
        }

        var matchingChangeSets = matchingChanges
            .Select(change => new
            {
                change.ChangedAt,
                change.ChangeSetId
            })
            .Distinct();

        if (criteria.After is not null)
        {
            var after = criteria.After;
            matchingChangeSets = criteria.SortDirection == HistorySortDirection.Ascending
                ? matchingChangeSets.Where(changeSet =>
                    changeSet.ChangedAt > after.ChangedAt
                    || (changeSet.ChangedAt == after.ChangedAt
                        && changeSet.ChangeSetId.CompareTo(after.ChangeSetId) > 0))
                : matchingChangeSets.Where(changeSet =>
                    changeSet.ChangedAt < after.ChangedAt
                    || (changeSet.ChangedAt == after.ChangedAt
                        && changeSet.ChangeSetId.CompareTo(after.ChangeSetId) < 0));
        }

        var orderedChangeSets = criteria.SortDirection == HistorySortDirection.Ascending
            ? matchingChangeSets
                .OrderBy(changeSet => changeSet.ChangedAt)
                .ThenBy(changeSet => changeSet.ChangeSetId)
            : matchingChangeSets
                .OrderByDescending(changeSet => changeSet.ChangedAt)
                .ThenByDescending(changeSet => changeSet.ChangeSetId);

        var keys = await orderedChangeSets
            .Take(criteria.Limit + 1)
            .ToArrayAsync(cancellationToken);
        var hasMore = keys.Length > criteria.Limit;
        var pageKeys = keys.Take(criteria.Limit).ToArray();

        if (pageKeys.Length == 0)
        {
            return new BookHistoryPage([], hasMore);
        }

        var changeSetIds = pageKeys
            .Select(key => key.ChangeSetId)
            .ToArray();
        var changes = await database.BookChanges
            .AsNoTracking()
            .Where(change =>
                change.BookId == criteria.BookId
                && changeSetIds.Contains(change.ChangeSetId))
            .OrderBy(change => change.Id)
            .ToArrayAsync(cancellationToken);
        var changesBySet = changes.ToLookup(change => change.ChangeSetId);

        var items = pageKeys
            .Select(key => new BookHistoryChangeSet(
                key.ChangeSetId,
                key.ChangedAt,
                changesBySet[key.ChangeSetId]
                    .Select(ToHistoryChange)
                    .ToArray()))
            .ToArray();

        return new BookHistoryPage(items, hasMore);
    }

    public void AddRange(IReadOnlyCollection<BookChange> changes)
    {
        database.BookChanges.AddRange(changes.Select(ToPersistence));
    }

    private static BookHistoryChange ToHistoryChange(BookChangeRecord record)
    {
        var field = FromPersistence(record.ChangedField);

        return new BookHistoryChange(
            record.Id,
            field,
            DeserializeValue(field, record.OldValue?.RootElement),
            DeserializeValue(field, record.NewValue.RootElement)!);
    }

    private static object? DeserializeValue(
        BookField field,
        JsonElement? value)
    {
        if (value is null)
        {
            return null;
        }

        return field switch
        {
            BookField.Title or BookField.ShortDescription => value.Value.GetString()!,
            BookField.PublishDate => DateOnly.ParseExact(
                value.Value.GetString()!,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture),
            BookField.Authors => value.Value
                .EnumerateArray()
                .Select(item => item.GetString()!)
                .ToArray(),
            _ => throw new ArgumentOutOfRangeException(
                nameof(field),
                field,
                "Unknown Book field.")
        };
    }

    private static string ToPersistence(BookField field) =>
        field switch
        {
            BookField.Title => "title",
            BookField.ShortDescription => "shortDescription",
            BookField.PublishDate => "publishDate",
            BookField.Authors => "authors",
            _ => throw new ArgumentOutOfRangeException(
                nameof(field),
                field,
                "Unknown Book field.")
        };

    private static BookField FromPersistence(string field) =>
        field switch
        {
            "title" => BookField.Title,
            "shortDescription" => BookField.ShortDescription,
            "publishDate" => BookField.PublishDate,
            "authors" => BookField.Authors,
            _ => throw new ArgumentException(
                $"'{field}' is not a valid persisted Book field.",
                nameof(field))
        };

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
