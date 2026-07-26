using Library.Domain.Books;

namespace Library.Application.Abstractions;

public interface IBookChangeRepository
{
    void AddRange(IReadOnlyCollection<BookChange> changes);

    Task<BookHistoryPage> ReadHistoryAsync(
        BookHistoryCriteria criteria,
        CancellationToken cancellationToken);
}

public sealed record BookHistoryCriteria(
    long BookId,
    IReadOnlySet<BookField> ChangedFields,
    DateTimeOffset? ChangedFrom,
    DateTimeOffset? ChangedBefore,
    HistorySortDirection SortDirection,
    int Limit,
    BookHistoryPosition? After);

public sealed record BookHistoryPosition(
    DateTimeOffset ChangedAt,
    Guid ChangeSetId);

public sealed record BookHistoryPage(
    IReadOnlyList<BookHistoryChangeSet> Items,
    bool HasMore);

public sealed record BookHistoryChangeSet(
    Guid ChangeSetId,
    DateTimeOffset ChangedAt,
    IReadOnlyList<BookHistoryChange> Changes);

public sealed record BookHistoryChange(
    long Id,
    BookField ChangedField,
    object? OldValue,
    object NewValue);

public enum HistorySortDirection
{
    Ascending,
    Descending
}
