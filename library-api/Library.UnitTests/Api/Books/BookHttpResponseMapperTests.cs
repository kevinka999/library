using Library.Api.Books;
using Library.Application.Books;

namespace Library.UnitTests.Api.Books;

public sealed class BookHttpResponseMapperTests
{
    [Fact]
    public void ToResponseAndETag_MapCompleteCreatedBook()
    {
        var book = new BookDto(
            42,
            "The Dispossessed",
            "An ambiguous utopia.",
            new DateOnly(1974, 5, 1),
            ["Ursula K. Le Guin"],
            1);

        var response = BookHttpResponseMapper.ToResponse(book);

        Assert.Equal(book.Id, response.Id);
        Assert.Equal(book.Title, response.Title);
        Assert.Equal(book.ShortDescription, response.ShortDescription);
        Assert.Equal(book.PublishDate, response.PublishDate);
        Assert.Equal(book.Authors, response.Authors);
        Assert.Equal(book.Version, response.Version);
        Assert.Equal("\"1\"", BookHttpResponseMapper.ToETag(response.Version));
    }
}
