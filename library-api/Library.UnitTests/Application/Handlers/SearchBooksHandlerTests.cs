using Library.Application.Abstractions;
using Library.Application.Handlers.SearchBooks;
using Library.Domain.Books;

namespace Library.UnitTests.Application.Handlers;

public sealed class SearchBooksHandlerTests
{
    [Fact]
    public async Task HandleAsync_DefaultQueryUsesFirstPageAndDefaultPageSize()
    {
        var repository = new RecordingBookRepository(new PagedBooks([], 0));
        var handler = new SearchBooksHandler(repository);

        var result = await handler.HandleAsync(new SearchBooksQuery());

        Assert.True(result.IsSuccess);
        Assert.Null(repository.Search);
        Assert.Equal(1, repository.Page);
        Assert.Equal(20, repository.PageSize);
        Assert.Equal(1, repository.CallCount);
        Assert.Equal(1, result.Value!.Page);
        Assert.Equal(20, result.Value.PageSize);
    }

    [Fact]
    public async Task HandleAsync_ValidBoundaryValuesReachReader()
    {
        var repository = new RecordingBookRepository(new PagedBooks([], 0));
        var handler = new SearchBooksHandler(repository);

        var result = await handler.HandleAsync(new SearchBooksQuery(null, 1, 100));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.Page);
        Assert.Equal(100, repository.PageSize);
        Assert.Equal(1, repository.CallCount);
    }

    [Theory]
    [InlineData(0, 20, "page")]
    [InlineData(-1, 20, "page")]
    [InlineData(1, 0, "pageSize")]
    [InlineData(1, -1, "pageSize")]
    [InlineData(1, 101, "pageSize")]
    [InlineData(int.MaxValue, 100, "page")]
    public async Task HandleAsync_InvalidPagingReturnsValidationFailureWithoutReading(
        int page,
        int pageSize,
        string errorKey)
    {
        var repository = new RecordingBookRepository(new PagedBooks([], 0));
        var handler = new SearchBooksHandler(repository);

        var result = await handler.HandleAsync(
            new SearchBooksQuery(null, page, pageSize));

        Assert.False(result.IsSuccess);
        Assert.Equal("books.search_validation_failed", result.Error!.Code);
        Assert.Contains(errorKey, result.Error.Errors!.Keys);
        Assert.Equal(0, repository.CallCount);
    }

    [Theory]
    [InlineData("  Guin  ", "Guin")]
    [InlineData("   ", null)]
    public async Task HandleAsync_NormalizesSearchBeforeReading(
        string search,
        string? expected)
    {
        var repository = new RecordingBookRepository(new PagedBooks([], 0));
        var handler = new SearchBooksHandler(repository);

        var result = await handler.HandleAsync(
            new SearchBooksQuery(search, 3, 10));

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, repository.Search);
        Assert.Equal(3, repository.Page);
        Assert.Equal(10, repository.PageSize);
    }

    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(19, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(40, 20, 2)]
    public async Task HandleAsync_CalculatesTotalPages(
        long totalCount,
        int pageSize,
        long expectedTotalPages)
    {
        var books = new[] { CreateBook() };
        var repository = new RecordingBookRepository(
            new PagedBooks(books, totalCount));
        var handler = new SearchBooksHandler(repository);

        var result = await handler.HandleAsync(
            new SearchBooksQuery(null, 1, pageSize));

        Assert.True(result.IsSuccess);
        var resultBook = Assert.Single(result.Value!.Items);
        Assert.Equal(books[0].Id, resultBook.Id);
        Assert.Equal(books[0].Title, resultBook.Title);
        Assert.Equal(books[0].ShortDescription, resultBook.ShortDescription);
        Assert.Equal(books[0].PublishDate, resultBook.PublishDate);
        Assert.Equal(books[0].Authors, resultBook.Authors);
        Assert.Equal(books[0].Version, resultBook.Version);
        Assert.Equal(totalCount, result.Value.TotalCount);
        Assert.Equal(expectedTotalPages, result.Value.TotalPages);
    }

    private static Book CreateBook() =>
        new(
            "The Dispossessed",
            "An ambiguous utopia.",
            new DateOnly(1974, 5, 1),
            ["Ursula K. Le Guin"]);

    private sealed class RecordingBookRepository(PagedBooks result)
        : IBookRepository
    {
        public int CallCount { get; private set; }

        public string? Search { get; private set; }

        public int? Page { get; private set; }

        public int? PageSize { get; private set; }

        public Task<PagedBooks> SearchAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Search = search;
            Page = page;
            PageSize = pageSize;
            return Task.FromResult(result);
        }

        public Task<Book?> GetByIdAsync(
            long id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Add(Book book) => throw new NotSupportedException();

        public void Update(Book book, long expectedVersion) =>
            throw new NotSupportedException();
    }
}
