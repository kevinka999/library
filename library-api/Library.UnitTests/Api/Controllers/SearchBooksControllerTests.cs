using Library.Api.Controllers.SearchBooks;
using Library.Application.Handlers;
using Library.Application.Handlers.SearchBooks;

namespace Library.UnitTests.Api.Controllers;

public sealed class SearchBooksControllerTests
{
    [Fact]
    public void InputDto_UsesDocumentedPagingDefaults()
    {
        var input = new SearchBooksInputDto();

        Assert.Equal(1, input.Page);
        Assert.Equal(20, input.PageSize);
    }

    [Fact]
    public void FromResult_MapsPageAndCompleteBooks()
    {
        var book = CreateBook();
        var result = new SearchBooksResult([book], 2, 10, 11, 2);

        var output = SearchBooksOutputDto.FromResult(result);

        Assert.Equal(2, output.Page);
        Assert.Equal(10, output.PageSize);
        Assert.Equal(11, output.TotalCount);
        Assert.Equal(2, output.TotalPages);
        var item = Assert.Single(output.Items);
        Assert.Equal(book.Id, item.Id);
        Assert.Equal(book.Title, item.Title);
        Assert.Equal(book.ShortDescription, item.ShortDescription);
        Assert.Equal(book.PublishDate, item.PublishDate);
        Assert.Equal(book.Authors, item.Authors);
        Assert.Equal(book.Version, item.Version);
    }

    [Fact]
    public void QueryParameterValidation_AcceptsOnlyDocumentedNames()
    {
        var error = SearchBooksQueryParameters.Validate(
        [
            KeyValuePair.Create("SEARCH", 1),
            KeyValuePair.Create("page", 1),
            KeyValuePair.Create("PageSize", 1)
        ]);

        Assert.Null(error);
    }

    [Fact]
    public void QueryParameterValidation_ReportsEveryUnknownName()
    {
        var error = SearchBooksQueryParameters.Validate(
        [
            KeyValuePair.Create("page", 1),
            KeyValuePair.Create("sort", 1),
            KeyValuePair.Create("filter", 1)
        ]);

        Assert.NotNull(error);
        Assert.Equal("books.search_validation_failed", error.Code);
        Assert.Equal(["filter", "sort"], error.Errors!.Keys.Order());
    }

    [Fact]
    public void QueryParameterValidation_RejectsRepeatedScalarParameters()
    {
        var error = SearchBooksQueryParameters.Validate(
        [
            KeyValuePair.Create("search", 2),
            KeyValuePair.Create("page", 2),
            KeyValuePair.Create("pageSize", 1)
        ]);

        Assert.NotNull(error);
        Assert.Equal(["page", "search"], error.Errors!.Keys.Order());
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
