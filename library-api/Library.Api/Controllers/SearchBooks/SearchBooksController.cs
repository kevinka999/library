using System.ComponentModel;
using Library.Api.Errors;
using Library.Application.Common;
using Library.Application.Handlers;
using Library.Application.Handlers.SearchBooks;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers.SearchBooks;

[ApiController]
[Route("api/books")]
public sealed class SearchBooksController(SearchBooksHandler handler) : ControllerBase
{
    /// <summary>Returns a deterministic numbered page of Books.</summary>
    /// <remarks>
    /// Search is case-insensitive across title, short description, and every
    /// Author Name. Results are always ordered by title and then Book ID.
    /// </remarks>
    /// <response code="200">Returns the requested page and collection totals.</response>
    /// <response code="400">A paging value or query parameter is invalid.</response>
    [HttpGet]
    [ProducesResponseType<SearchBooksOutputDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(HttpValidationProblemDetails),
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    public async Task<ActionResult<SearchBooksOutputDto>> Handle(
        [FromQuery] SearchBooksInputDto input,
        CancellationToken cancellationToken)
    {
        var queryParameterError = SearchBooksQueryParameters.Validate(
            Request.Query.Select(parameter =>
                KeyValuePair.Create(parameter.Key, parameter.Value.Count)));
        if (queryParameterError is not null)
        {
            return queryParameterError.ToActionResult();
        }

        var result = await handler.HandleAsync(
            new SearchBooksQuery(input.Search, input.Page, input.PageSize),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error!.ToActionResult();
        }

        return Ok(SearchBooksOutputDto.FromResult(result.Value!));
    }
}

public sealed class SearchBooksInputDto
{
    [FromQuery(Name = "search")]
    [Description(
        "Optional case-insensitive text matched literally within title, short description, or any Author Name.")]
    public string? Search { get; init; }

    [FromQuery(Name = "page")]
    [DefaultValue(SearchBooksHandler.DefaultPage)]
    [Description("One-based page number. Defaults to 1.")]
    public int Page { get; init; } = SearchBooksHandler.DefaultPage;

    [FromQuery(Name = "pageSize")]
    [DefaultValue(SearchBooksHandler.DefaultPageSize)]
    [Description("Books per page, from 1 through 100. Defaults to 20.")]
    public int PageSize { get; init; } = SearchBooksHandler.DefaultPageSize;
}

public sealed record SearchBooksOutputDto(
    IReadOnlyList<SearchBooksItemDto> Items,
    int Page,
    int PageSize,
    long TotalCount,
    long TotalPages)
{
    public static SearchBooksOutputDto FromResult(SearchBooksResult result) =>
        new(
            result.Items.Select(SearchBooksItemDto.FromResult).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);
}

public sealed record SearchBooksItemDto(
    long Id,
    string Title,
    string ShortDescription,
    DateOnly PublishDate,
    IReadOnlyList<string> Authors,
    long Version)
{
    public static SearchBooksItemDto FromResult(BookResult book) =>
        new(
            book.Id,
            book.Title,
            book.ShortDescription,
            book.PublishDate,
            book.Authors,
            book.Version);
}

public static class SearchBooksQueryParameters
{
    private static readonly HashSet<string> Allowed =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "search",
            "page",
            "pageSize"
        };

    public static ApplicationError? Validate(
        IEnumerable<KeyValuePair<string, int>> parameters)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var (parameterName, valueCount) in parameters)
        {
            if (!Allowed.Contains(parameterName))
            {
                errors.TryAdd(
                    parameterName,
                    ["The query parameter is not supported."]);
            }
            else if (valueCount != 1)
            {
                errors.TryAdd(
                    parameterName,
                    ["The query parameter must be specified at most once."]);
            }
        }

        return errors.Count == 0
            ? null
            : ApplicationError.Validation(
                "books.search_validation_failed",
                "One or more Book search parameters are invalid.",
                errors);
    }
}
