using Library.Application.Abstractions;
using Library.Application.Common;

namespace Library.Application.Handlers.SearchBooks;

public sealed record SearchBooksQuery(
    string? Search = null,
    int Page = SearchBooksHandler.DefaultPage,
    int PageSize = SearchBooksHandler.DefaultPageSize);

public sealed record SearchBooksResult(
    IReadOnlyList<BookResult> Items,
    int Page,
    int PageSize,
    long TotalCount,
    long TotalPages);

public sealed class SearchBooksHandler(IBookRepository bookRepository)
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    public async Task<Result<SearchBooksResult>> HandleAsync(
        SearchBooksQuery query,
        CancellationToken cancellationToken = default)
    {
        var errors = Validate(query);
        if (errors.Count > 0)
        {
            return Result<SearchBooksResult>.Failure(ApplicationError.Validation(
                "books.search_validation_failed",
                "One or more Book search parameters are invalid.",
                errors));
        }

        var normalizedSearch = string.IsNullOrWhiteSpace(query.Search)
            ? null
            : query.Search.Trim();

        var page = await bookRepository.SearchAsync(
            normalizedSearch,
            query.Page,
            query.PageSize,
            cancellationToken);
        var totalPages = page.TotalCount == 0
            ? 0
            : page.TotalCount / query.PageSize
                + (page.TotalCount % query.PageSize == 0 ? 0 : 1);

        return Result<SearchBooksResult>.Success(new SearchBooksResult(
            page.Items.Select(BookResult.FromDomain).ToArray(),
            query.Page,
            query.PageSize,
            page.TotalCount,
            totalPages));
    }

    private static Dictionary<string, string[]> Validate(SearchBooksQuery query)
    {
        var errors = new Dictionary<string, string[]>();

        if (query.Page < 1)
        {
            errors["page"] = ["Page must be at least 1."];
        }

        if (query.PageSize is < 1 or > MaximumPageSize)
        {
            errors["pageSize"] =
            [
                $"Page size must be between 1 and {MaximumPageSize}."
            ];
        }

        if (query.Page >= 1
            && query.PageSize is >= 1 and <= MaximumPageSize
            && ((long)query.Page - 1) * query.PageSize > int.MaxValue)
        {
            errors["page"] = ["The requested page is too large."];
        }

        return errors;
    }
}
