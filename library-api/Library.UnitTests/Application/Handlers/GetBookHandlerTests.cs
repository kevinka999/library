using Library.Application.Abstractions;
using Library.Application.Handlers.GetBook;
using Library.Domain.Books;

namespace Library.UnitTests.Application.Handlers;

public sealed class GetBookHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingBookReturnsCompleteCurrentState()
    {
        var expected = CreateBook();
        var repository = new RecordingBookRepository(expected);
        var handler = new GetBookHandler(repository);

        var result = await handler.HandleAsync(new GetBookQuery(42));

        Assert.True(result.IsSuccess);
        Assert.Equal(expected.Id, result.Value!.Id);
        Assert.Equal(expected.Title, result.Value.Title);
        Assert.Equal(expected.ShortDescription, result.Value.ShortDescription);
        Assert.Equal(expected.PublishDate, result.Value.PublishDate);
        Assert.Equal(expected.Authors, result.Value.Authors);
        Assert.Equal(expected.Version, result.Value.Version);
        Assert.Equal(42, repository.RequestedId);
        Assert.Equal(1, repository.CallCount);
    }

    [Fact]
    public async Task HandleAsync_MissingBookReturnsNotFound()
    {
        var repository = new RecordingBookRepository(null);
        var handler = new GetBookHandler(repository);

        var result = await handler.HandleAsync(new GetBookQuery(999));

        Assert.False(result.IsSuccess);
        Assert.Equal("book.not_found", result.Error!.Code);
        Assert.Equal("Book 999 was not found.", result.Error.Detail);
        Assert.Equal(999, repository.RequestedId);
        Assert.Equal(1, repository.CallCount);
    }

    private static Book CreateBook() =>
        new(
            "The Dispossessed",
            "An ambiguous utopia.",
            new DateOnly(1974, 5, 1),
            ["Ursula K. Le Guin"]);

    private sealed class RecordingBookRepository(Book? result) : IBookRepository
    {
        public int CallCount { get; private set; }

        public long? RequestedId { get; private set; }

        public Task<Book?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            CallCount++;
            RequestedId = id;
            return Task.FromResult(result);
        }

        public Task<PagedBooks> SearchAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Add(Book book) => throw new NotSupportedException();

        public void Update(Book book, long expectedVersion) =>
            throw new NotSupportedException();
    }
}
