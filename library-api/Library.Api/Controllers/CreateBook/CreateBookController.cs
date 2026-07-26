using Library.Api.Errors;
using Library.Api.OpenApi;
using Library.Application.Handlers;
using Library.Application.Handlers.CreateBook;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers.CreateBook;

[ApiController]
[Route("api/books")]
public sealed class CreateBookController(CreateBookHandler handler) : ControllerBase
{
    /// <summary>Creates a Book and its complete initial Change Set.</summary>
    /// <response code="201">Returns the normalized Book with Location and ETag headers.</response>
    /// <response code="400">Returns all detected validation errors.</response>
    [HttpPost]
    [ResponseHeader(
        StatusCodes.Status201Created,
        "ETag",
        "The strong ETag representing the current Book version.")]
    [ProducesResponseType<CreateBookOutputDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(HttpValidationProblemDetails),
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    public async Task<ActionResult<CreateBookOutputDto>> Handle(
        CreateBookInputDto input,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateBookCommand(
                input.Title,
                input.ShortDescription,
                input.PublishDate,
                input.Authors),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error!.ToActionResult();
        }

        var output = CreateBookOutputDto.FromResult(result.Value!);
        Response.Headers.ETag = $"\"{output.Version}\"";

        return Created($"/api/books/{output.Id}", output);
    }
}

public sealed class CreateBookInputDto
{
    public required string? Title { get; init; }

    public required string? ShortDescription { get; init; }

    public required DateOnly? PublishDate { get; init; }

    public required IReadOnlyCollection<string?>? Authors { get; init; }
}

public sealed record CreateBookOutputDto(
    long Id,
    string Title,
    string ShortDescription,
    DateOnly PublishDate,
    IReadOnlyList<string> Authors,
    long Version)
{
    public static CreateBookOutputDto FromResult(BookResult book) =>
        new(
            book.Id,
            book.Title,
            book.ShortDescription,
            book.PublishDate,
            book.Authors,
            book.Version);
}
