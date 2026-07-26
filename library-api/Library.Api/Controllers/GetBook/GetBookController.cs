using Library.Api.Errors;
using Library.Application.Handlers;
using Library.Application.Handlers.GetBook;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers.GetBook;

[ApiController]
[Route("api/books/{id:long}")]
public sealed class GetBookController(GetBookHandler handler) : ControllerBase
{
    /// <summary>Retrieves the complete current representation of a Book.</summary>
    /// <response code="200">Returns the current Book with its ETag.</response>
    /// <response code="404">The requested Book does not exist.</response>
    [HttpGet]
    [ProducesResponseType<GetBookOutputDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    public async Task<ActionResult<GetBookOutputDto>> Handle(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetBookQuery(id),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error!.ToActionResult();
        }

        var output = GetBookOutputDto.FromResult(result.Value!);
        Response.Headers.ETag = $"\"{output.Version}\"";

        return Ok(output);
    }
}

public sealed record GetBookOutputDto(
    long Id,
    string Title,
    string ShortDescription,
    DateOnly PublishDate,
    IReadOnlyList<string> Authors,
    long Version)
{
    public static GetBookOutputDto FromResult(BookResult book) =>
        new(
            book.Id,
            book.Title,
            book.ShortDescription,
            book.PublishDate,
            book.Authors,
            book.Version);
}
