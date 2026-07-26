using System.ComponentModel;
using Library.Api.Errors;
using Library.Application.Abstractions;
using Library.Application.Common;
using Library.Application.Handlers.GetBookHistory;
using Library.Domain.Books;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers.GetBookHistory;

[ApiController]
[Route("api/books/{id:long}/history")]
public sealed class GetBookHistoryController(GetBookHistoryHandler handler)
    : ControllerBase
{
    /// <summary>Returns cursor-paged complete Change Sets for a Book.</summary>
    /// <remarks>
    /// Cursors are opaque and may only be reused with the same normalized
    /// Changed Field, time, and sort filters. A Changed Field match includes
    /// every change in its containing Change Set.
    /// </remarks>
    /// <response code="200">Returns at most the requested number of complete Change Sets.</response>
    /// <response code="400">A filter, query parameter, time range, or cursor is invalid.</response>
    /// <response code="404">The requested Book does not exist.</response>
    [HttpGet]
    [ProducesResponseType<GetBookHistoryOutputDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(HttpValidationProblemDetails),
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    public async Task<ActionResult<GetBookHistoryOutputDto>> Handle(
        long id,
        [FromQuery] GetBookHistoryInputDto input,
        CancellationToken cancellationToken)
    {
        var queryParameterError = GetBookHistoryQueryParameters.Validate(
            Request.Query.Select(parameter =>
                KeyValuePair.Create(parameter.Key, parameter.Value.Count)));
        if (queryParameterError is not null)
        {
            return queryParameterError.ToActionResult();
        }

        var result = await handler.HandleAsync(
            new GetBookHistoryQuery(
                id,
                input.ChangedField,
                input.ChangedFrom,
                input.ChangedBefore,
                input.SortDirection,
                input.Limit,
                input.After),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error!.ToActionResult();
        }

        return Ok(GetBookHistoryOutputDto.FromResult(result.Value!));
    }
}

public sealed class GetBookHistoryInputDto
{
    [FromQuery(Name = "changedField")]
    [Description(
        "Optional repeatable filter: title, shortDescription, publishDate, or authors. Repeated values use any-match semantics.")]
    public string[] ChangedField { get; init; } = [];

    [FromQuery(Name = "changedFrom")]
    [Description("Optional inclusive lower UTC instant.")]
    public DateTimeOffset? ChangedFrom { get; init; }

    [FromQuery(Name = "changedBefore")]
    [Description("Optional exclusive upper UTC instant.")]
    public DateTimeOffset? ChangedBefore { get; init; }

    [FromQuery(Name = "sortDirection")]
    [DefaultValue("descending")]
    [Description("Chronological direction: ascending or descending. Defaults to descending.")]
    public string? SortDirection { get; init; }

    [FromQuery(Name = "limit")]
    [DefaultValue(GetBookHistoryHandler.DefaultLimit)]
    [Description("Complete Change Sets per page, from 1 through 100. Defaults to 20.")]
    public int Limit { get; init; } = GetBookHistoryHandler.DefaultLimit;

    [FromQuery(Name = "after")]
    [Description("Opaque cursor returned as nextCursor by a compatible prior request.")]
    public string? After { get; init; }
}

public sealed record GetBookHistoryOutputDto(
    IReadOnlyList<BookHistoryItemDto> Items,
    string? NextCursor,
    bool HasMore)
{
    public static GetBookHistoryOutputDto FromResult(GetBookHistoryResult result) =>
        new(
            result.Items.Select(BookHistoryItemDto.FromResult).ToArray(),
            result.NextCursor,
            result.HasMore);
}

public sealed record BookHistoryItemDto(
    Guid ChangeSetId,
    DateTimeOffset ChangedAt,
    IReadOnlyList<BookHistoryChangeDto> Changes)
{
    public static BookHistoryItemDto FromResult(BookHistoryChangeSet changeSet) =>
        new(
            changeSet.ChangeSetId,
            changeSet.ChangedAt,
            changeSet.Changes.Select(BookHistoryChangeDto.FromResult).ToArray());
}

public sealed record BookHistoryChangeDto(
    long Id,
    string ChangedField,
    object? OldValue,
    object NewValue)
{
    public static BookHistoryChangeDto FromResult(BookHistoryChange change) =>
        new(
            change.Id,
            ToContractName(change.ChangedField),
            change.OldValue,
            change.NewValue);

    private static string ToContractName(BookField field) =>
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
}

public static class GetBookHistoryQueryParameters
{
    private static readonly HashSet<string> Allowed =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "changedField",
            "changedFrom",
            "changedBefore",
            "sortDirection",
            "limit",
            "after"
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
            else if (!parameterName.Equals(
                    "changedField",
                    StringComparison.OrdinalIgnoreCase)
                && valueCount != 1)
            {
                errors.TryAdd(
                    parameterName,
                    ["The query parameter must be specified at most once."]);
            }
        }

        return errors.Count == 0
            ? null
            : ApplicationError.Validation(
                "book.history_validation_failed",
                "One or more Book history parameters are invalid.",
                errors);
    }
}
