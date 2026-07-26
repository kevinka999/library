using System.ComponentModel;
using System.Globalization;
using Library.Api.Errors;
using Library.Api.OpenApi;
using Library.Application.Common;
using Library.Application.Handlers;
using Library.Application.Handlers.UpdateBook;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers.UpdateBook;

[ApiController]
[Route("api/books/{id:long}")]
public sealed class UpdateBookController(UpdateBookHandler handler) : ControllerBase
{
    /// <summary>Replaces every editable field of a Book using optimistic concurrency.</summary>
    /// <response code="200">Returns the current Book and ETag after an update or no-op.</response>
    /// <response code="400">The request body or If-Match value is invalid.</response>
    /// <response code="404">The requested Book does not exist.</response>
    /// <response code="412">The supplied ETag is stale.</response>
    /// <response code="428">The If-Match header is absent.</response>
    [HttpPut]
    [ResponseHeader(
        StatusCodes.Status200OK,
        "ETag",
        "The strong ETag representing the current Book version.")]
    [ProducesResponseType<UpdateBookOutputDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(HttpValidationProblemDetails),
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status412PreconditionFailed,
        "application/problem+json")]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status428PreconditionRequired,
        "application/problem+json")]
    public async Task<ActionResult<UpdateBookOutputDto>> Handle(
        [Description("The database-generated Book identifier.")]
        long id,
        [FromBody]
        [Description("The complete replacement state for every editable Book field.")]
        UpdateBookInputDto input,
        [FromHeader(Name = "If-Match")]
        [Description("The current strong Book ETag, for example \"1\".")]
        string? ifMatch,
        CancellationToken cancellationToken)
    {
        long? expectedVersion = null;
        if (ifMatch is not null)
        {
            if (!TryParseVersion(ifMatch, out var parsedVersion))
            {
                return ApplicationError.Validation(
                        "book.invalid_if_match",
                        "The If-Match header must contain exactly one strong ETag with a positive decimal Book version.",
                        new Dictionary<string, string[]>
                        {
                            ["ifMatch"] =
                            [
                                "Use the strong ETag returned by the most recent GET, POST, or PUT response."
                            ]
                        })
                    .ToActionResult();
            }

            expectedVersion = parsedVersion;
        }

        var result = await handler.HandleAsync(
            new UpdateBookCommand(
                id,
                expectedVersion,
                input.Title,
                input.ShortDescription,
                input.PublishDate,
                input.Authors),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error!.ToActionResult();
        }

        var output = UpdateBookOutputDto.FromResult(result.Value!.Book);
        Response.Headers.ETag = $"\"{output.Version}\"";

        return Ok(output);
    }

    private static bool TryParseVersion(string etag, out long version)
    {
        version = default;

        if (etag.Length < 3
            || etag[0] != '"'
            || etag[^1] != '"')
        {
            return false;
        }

        var versionText = etag[1..^1];
        return long.TryParse(
                versionText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out version)
            && version > 0
            && versionText == version.ToString(CultureInfo.InvariantCulture);
    }
}

public sealed class UpdateBookInputDto
{
    public required string? Title { get; init; }

    public required string? ShortDescription { get; init; }

    public required DateOnly? PublishDate { get; init; }

    public required IReadOnlyCollection<string?>? Authors { get; init; }
}

public sealed record UpdateBookOutputDto(
    long Id,
    string Title,
    string ShortDescription,
    DateOnly PublishDate,
    IReadOnlyList<string> Authors,
    long Version)
{
    public static UpdateBookOutputDto FromResult(BookResult book) =>
        new(
            book.Id,
            book.Title,
            book.ShortDescription,
            book.PublishDate,
            book.Authors,
            book.Version);
}
