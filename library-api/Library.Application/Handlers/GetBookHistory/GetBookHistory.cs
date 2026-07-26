using Library.Application.Abstractions;
using Library.Application.Common;
using Library.Domain.Books;

namespace Library.Application.Handlers.GetBookHistory;

public sealed record GetBookHistoryQuery(
    long BookId,
    IReadOnlyCollection<string>? ChangedFields = null,
    DateTimeOffset? ChangedFrom = null,
    DateTimeOffset? ChangedBefore = null,
    string? SortDirection = null,
    int Limit = GetBookHistoryHandler.DefaultLimit,
    string? After = null);

public sealed record GetBookHistoryResult(
    IReadOnlyList<BookHistoryChangeSet> Items,
    string? NextCursor,
    bool HasMore);

public sealed class GetBookHistoryHandler(
    IBookRepository bookRepository,
    IBookChangeRepository bookChangeRepository,
    BookHistoryCursorCodec cursorCodec)
{
    public const int DefaultLimit = 20;
    public const int MaximumLimit = 100;

    public async Task<Result<GetBookHistoryResult>> HandleAsync(
        GetBookHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalization = Normalize(query);
        if (normalization.Error is not null)
        {
            return Result<GetBookHistoryResult>.Failure(normalization.Error);
        }

        var normalized = normalization.Query!;
        var book = await bookRepository.GetByIdAsync(query.BookId, cancellationToken);
        if (book is null)
        {
            return Result<GetBookHistoryResult>.Failure(ApplicationError.NotFound(
                "book.not_found",
                $"Book {query.BookId} was not found."));
        }

        var page = await bookChangeRepository.ReadHistoryAsync(
            new BookHistoryCriteria(
                query.BookId,
                normalized.ChangedFields.ToHashSet(),
                normalized.ChangedFrom,
                normalized.ChangedBefore,
                normalized.SortDirection,
                normalized.Limit,
                normalized.After?.Position),
            cancellationToken);

        var nextCursor = page.HasMore && page.Items.Count > 0
            ? cursorCodec.Encode(new BookHistoryCursor(
                normalized.SortDirection,
                normalized.ChangedFields,
                normalized.ChangedFrom,
                normalized.ChangedBefore,
                new BookHistoryPosition(
                    page.Items[^1].ChangedAt,
                    page.Items[^1].ChangeSetId)))
            : null;

        return Result<GetBookHistoryResult>.Success(new GetBookHistoryResult(
            page.Items,
            nextCursor,
            page.HasMore));
    }

    private NormalizationResult Normalize(GetBookHistoryQuery query)
    {
        var errors = new Dictionary<string, List<string>>();

        if (query.Limit is < 1 or > MaximumLimit)
        {
            AddError(
                errors,
                "limit",
                $"Limit must be between 1 and {MaximumLimit}.");
        }

        var direction = ParseDirection(query.SortDirection);
        if (direction is null)
        {
            AddError(
                errors,
                "sortDirection",
                "Sort direction must be 'ascending' or 'descending'.");
        }

        var fields = new HashSet<BookField>();
        foreach (var suppliedField in query.ChangedFields ?? [])
        {
            if (!TryParseField(suppliedField, out var field))
            {
                AddError(
                    errors,
                    "changedField",
                    $"'{suppliedField}' is not a supported Changed Field.");
                continue;
            }

            fields.Add(field);
        }

        var changedFrom = query.ChangedFrom?.ToUniversalTime();
        var changedBefore = query.ChangedBefore?.ToUniversalTime();
        if (changedFrom >= changedBefore)
        {
            AddError(
                errors,
                "changedBefore",
                "Changed before must be later than changed from.");
        }

        BookHistoryCursor? cursor = null;
        if (query.After is not null
            && !cursorCodec.TryDecode(query.After, out cursor))
        {
            AddError(errors, "after", "The history cursor is invalid.");
        }

        var orderedFields = fields.Order().ToArray();
        if (cursor is not null
            && (cursor.SortDirection != direction
                || !cursor.ChangedFields.SequenceEqual(orderedFields)
                || cursor.ChangedFrom != changedFrom
                || cursor.ChangedBefore != changedBefore))
        {
            AddError(
                errors,
                "after",
                "The history cursor is not compatible with the current filters.");
        }

        if (errors.Count > 0)
        {
            return new NormalizationResult(
                null,
                ApplicationError.Validation(
                    "book.history_validation_failed",
                    "One or more Book history parameters are invalid.",
                    errors.ToDictionary(
                        pair => pair.Key,
                        pair => pair.Value.ToArray())));
        }

        return new NormalizationResult(
            new NormalizedQuery(
                orderedFields,
                changedFrom,
                changedBefore,
                direction!.Value,
                query.Limit,
                cursor),
            null);
    }

    private static HistorySortDirection? ParseDirection(string? direction) =>
        direction?.Trim().ToLowerInvariant() switch
        {
            null or "" or "descending" => HistorySortDirection.Descending,
            "ascending" => HistorySortDirection.Ascending,
            _ => null
        };

    private static bool TryParseField(string supplied, out BookField field)
    {
        field = supplied.Trim().ToLowerInvariant() switch
        {
            "title" => BookField.Title,
            "shortdescription" => BookField.ShortDescription,
            "publishdate" => BookField.PublishDate,
            "authors" => BookField.Authors,
            _ => (BookField)(-1)
        };

        return Enum.IsDefined(field);
    }

    private static void AddError(
        IDictionary<string, List<string>> errors,
        string key,
        string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = [];
            errors[key] = messages;
        }

        messages.Add(message);
    }

    private sealed record NormalizedQuery(
        IReadOnlyList<BookField> ChangedFields,
        DateTimeOffset? ChangedFrom,
        DateTimeOffset? ChangedBefore,
        HistorySortDirection SortDirection,
        int Limit,
        BookHistoryCursor? After);

    private sealed record NormalizationResult(
        NormalizedQuery? Query,
        ApplicationError? Error);
}
