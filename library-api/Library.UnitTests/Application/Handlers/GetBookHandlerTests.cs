using Library.Application.Abstractions;
using Library.Application.Handlers;
using Library.Application.Handlers.GetBook;

namespace Library.UnitTests.Application.Handlers;

public sealed class GetBookHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingBookReturnsCompleteCurrentState()
    {
        var expected = CreateBook();
        var reader = new RecordingBookReader(expected);
        var handler = new GetBookHandler(reader);

        var result = await handler.HandleAsync(new GetBookQuery(expected.Id));

        Assert.True(result.IsSuccess);
        Assert.Same(expected, result.Value);
        Assert.Equal(expected.Id, reader.RequestedId);
        Assert.Equal(1, reader.CallCount);
    }

    [Fact]
    public async Task HandleAsync_MissingBookReturnsNotFound()
    {
        var reader = new RecordingBookReader(null);
        var handler = new GetBookHandler(reader);

        var result = await handler.HandleAsync(new GetBookQuery(999));

        Assert.False(result.IsSuccess);
        Assert.Equal("book.not_found", result.Error!.Code);
        Assert.Equal("Book 999 was not found.", result.Error.Detail);
        Assert.Equal(999, reader.RequestedId);
        Assert.Equal(1, reader.CallCount);
    }

    private static BookResult CreateBook() =>
        new(
            42,
            "The Dispossessed",
            "An ambiguous utopia.",
            new DateOnly(1974, 5, 1),
            ["Ursula K. Le Guin"],
            1);

    private sealed class RecordingBookReader(BookResult? result) : IBookReader
    {
        public int CallCount { get; private set; }

        public long? RequestedId { get; private set; }

        public Task<BookResult?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            CallCount++;
            RequestedId = id;
            return Task.FromResult(result);
        }
    }
}
