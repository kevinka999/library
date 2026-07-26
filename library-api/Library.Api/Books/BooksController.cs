using Library.Api.Errors;
using Library.Application.Books.CreateBook;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Books;

[ApiController]
[Route("api/books")]
public sealed class BooksController(CreateBookHandler createBookHandler) : ControllerBase
{
    /// <summary>Creates a Book and its complete initial Change Set.</summary>
    /// <response code="201">Returns the normalized Book with Location and ETag headers.</response>
    /// <response code="400">Returns all detected validation errors.</response>
    [HttpPost]
    [ProducesResponseType<BookResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(HttpValidationProblemDetails),
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    public async Task<ActionResult<BookResponse>> Create(
        CreateBookRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createBookHandler.HandleAsync(
            new CreateBookCommand(
                request.Title,
                request.ShortDescription,
                request.PublishDate,
                request.Authors),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error!.ToActionResult();
        }

        var response = BookHttpResponseMapper.ToResponse(result.Value!);
        Response.Headers.ETag = BookHttpResponseMapper.ToETag(response.Version);

        return Created($"/api/books/{response.Id}", response);
    }
}
