using Library.Api.Controllers.GetBook;
using Library.Application.Handlers;

namespace Library.UnitTests.Api.Controllers;

public sealed class GetBookControllerTests
{
    [Fact]
    public void FromResult_MapsCompleteBook()
    {
        var book = CreateBook();

        var output = GetBookOutputDto.FromResult(book);

        Assert.Equal(book.Id, output.Id);
        Assert.Equal(book.Title, output.Title);
        Assert.Equal(book.ShortDescription, output.ShortDescription);
        Assert.Equal(book.PublishDate, output.PublishDate);
        Assert.Equal(book.Authors, output.Authors);
        Assert.Equal(book.Version, output.Version);
    }

    private static BookResult CreateBook() =>
        new(
            42,
            "The Dispossessed",
            "An ambiguous utopia.",
            new DateOnly(1974, 5, 1),
            ["Ursula K. Le Guin"],
            1);
}
