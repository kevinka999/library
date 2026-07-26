using Library.Application.Abstractions;
using Library.Application.Handlers.GetBookHistory;
using Library.Domain.Books;

namespace Library.UnitTests.Application.Handlers;

public sealed class GetBookHistoryHandlerTests
{
    [Fact]
    public async Task HandleAsync_MissingBookDoesNotReadHistory()
    {
        var reader = new RecordingBookChangeRepository(new BookHistoryPage([], false));
        var handler = CreateHandler(null, reader);

        var result = await handler.HandleAsync(new GetBookHistoryQuery(404));

        Assert.False(result.IsSuccess);
        Assert.Equal("book.not_found", result.Error!.Code);
        Assert.Equal(0, reader.CallCount);
    }

    [Fact]
    public async Task HandleAsync_DefaultsToTwentyNewestFirst()
    {
        var reader = new RecordingBookChangeRepository(new BookHistoryPage([], false));
        var handler = CreateHandler(CreateBook(), reader);

        var result = await handler.HandleAsync(new GetBookHistoryQuery(42));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, reader.CallCount);
        Assert.Equal(20, reader.Criteria!.Limit);
        Assert.Equal(
            HistorySortDirection.Descending,
            reader.Criteria.SortDirection);
        Assert.Empty(reader.Criteria.ChangedFields);
        Assert.Null(reader.Criteria.ChangedFrom);
        Assert.Null(reader.Criteria.ChangedBefore);
        Assert.Null(reader.Criteria.After);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task HandleAsync_ValidLimitBoundariesReachReader(int limit)
    {
        var reader = new RecordingBookChangeRepository(new BookHistoryPage([], false));
        var handler = CreateHandler(CreateBook(), reader);

        var result = await handler.HandleAsync(
            new GetBookHistoryQuery(42, Limit: limit));

        Assert.True(result.IsSuccess);
        Assert.Equal(limit, reader.Criteria!.Limit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task HandleAsync_InvalidLimitDoesNotReadRepositories(int limit)
    {
        var books = new RecordingBookRepository(CreateBook());
        var reader = new RecordingBookChangeRepository(new BookHistoryPage([], false));
        var handler = new GetBookHistoryHandler(
            books,
            reader,
            new BookHistoryCursorCodec());

        var result = await handler.HandleAsync(
            new GetBookHistoryQuery(42, Limit: limit));

        Assert.False(result.IsSuccess);
        Assert.Contains("limit", result.Error!.Errors!.Keys);
        Assert.Equal(0, books.CallCount);
        Assert.Equal(0, reader.CallCount);
    }

    [Fact]
    public async Task HandleAsync_NormalizesRepeatedFieldsTimesAndDirectionOnce()
    {
        var reader = new RecordingBookChangeRepository(new BookHistoryPage([], false));
        var handler = CreateHandler(CreateBook(), reader);
        var from = new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.FromHours(2));
        var before = from.AddDays(1);

        var result = await handler.HandleAsync(new GetBookHistoryQuery(
            42,
            [" Authors ", "title", "AUTHORS"],
            from,
            before,
            " ASCENDING "));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            [BookField.Title, BookField.Authors],
            reader.Criteria!.ChangedFields.Order());
        Assert.Equal(from.ToUniversalTime(), reader.Criteria.ChangedFrom);
        Assert.Equal(before.ToUniversalTime(), reader.Criteria.ChangedBefore);
        Assert.Equal(
            HistorySortDirection.Ascending,
            reader.Criteria.SortDirection);
    }

    [Fact]
    public async Task HandleAsync_InvalidFieldsDirectionAndRangeReturnAllErrors()
    {
        var books = new RecordingBookRepository(CreateBook());
        var reader = new RecordingBookChangeRepository(new BookHistoryPage([], false));
        var handler = new GetBookHistoryHandler(
            books,
            reader,
            new BookHistoryCursorCodec());
        var instant = DateTimeOffset.UtcNow;

        var result = await handler.HandleAsync(new GetBookHistoryQuery(
            42,
            ["isbn"],
            instant,
            instant,
            "sideways"));

        Assert.False(result.IsSuccess);
        Assert.Equal("book.history_validation_failed", result.Error!.Code);
        Assert.Equal(
            ["changedBefore", "changedField", "sortDirection"],
            result.Error.Errors!.Keys.Order());
        Assert.Equal(0, books.CallCount);
        Assert.Equal(0, reader.CallCount);
    }

    [Fact]
    public async Task HandleAsync_CompatibleCursorPropagatesPosition()
    {
        var codec = new BookHistoryCursorCodec();
        var position = new BookHistoryPosition(
            new DateTimeOffset(2026, 7, 20, 12, 0, 0, TimeSpan.Zero),
            Guid.NewGuid());
        var cursor = codec.Encode(new BookHistoryCursor(
            HistorySortDirection.Descending,
            [BookField.Title],
            null,
            null,
            position));
        var reader = new RecordingBookChangeRepository(new BookHistoryPage([], false));
        var handler = CreateHandler(CreateBook(), reader, codec);

        var result = await handler.HandleAsync(new GetBookHistoryQuery(
            42,
            ["TITLE"],
            SortDirection: "descending",
            After: cursor));

        Assert.True(result.IsSuccess);
        Assert.Equal(position, reader.Criteria!.After);
    }

    [Fact]
    public async Task HandleAsync_IncompatibleCursorDoesNotReadRepositories()
    {
        var codec = new BookHistoryCursorCodec();
        var cursor = codec.Encode(new BookHistoryCursor(
            HistorySortDirection.Descending,
            [BookField.Title],
            null,
            null,
            new BookHistoryPosition(DateTimeOffset.UtcNow, Guid.NewGuid())));
        var books = new RecordingBookRepository(CreateBook());
        var reader = new RecordingBookChangeRepository(new BookHistoryPage([], false));
        var handler = new GetBookHistoryHandler(books, reader, codec);

        var result = await handler.HandleAsync(new GetBookHistoryQuery(
            42,
            [nameof(BookField.Authors)],
            After: cursor));

        Assert.False(result.IsSuccess);
        Assert.Contains("after", result.Error!.Errors!.Keys);
        Assert.Equal(0, books.CallCount);
        Assert.Equal(0, reader.CallCount);
    }

    [Theory]
    [InlineData("not-base64!")]
    [InlineData("e30")]
    public async Task HandleAsync_MalformedCursorReturnsValidationFailure(
        string cursor)
    {
        var books = new RecordingBookRepository(CreateBook());
        var reader = new RecordingBookChangeRepository(new BookHistoryPage([], false));
        var handler = new GetBookHistoryHandler(
            books,
            reader,
            new BookHistoryCursorCodec());

        var result = await handler.HandleAsync(
            new GetBookHistoryQuery(42, After: cursor));

        Assert.False(result.IsSuccess);
        Assert.Contains("after", result.Error!.Errors!.Keys);
        Assert.Equal(0, books.CallCount);
    }

    [Fact]
    public async Task HandleAsync_PropagatesCompleteSetsAndCreatesNextCursor()
    {
        var first = CreateChangeSet(
            new DateTimeOffset(2026, 7, 21, 8, 0, 0, TimeSpan.Zero));
        var second = CreateChangeSet(
            new DateTimeOffset(2026, 7, 20, 8, 0, 0, TimeSpan.Zero));
        var reader = new RecordingBookChangeRepository(
            new BookHistoryPage([first, second], true));
        var codec = new BookHistoryCursorCodec();
        var handler = CreateHandler(CreateBook(), reader, codec);

        var result = await handler.HandleAsync(new GetBookHistoryQuery(
            42,
            ["title"]));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.HasMore);
        Assert.Equal([first, second], result.Value.Items);
        Assert.NotNull(result.Value.NextCursor);
        Assert.True(codec.TryDecode(result.Value.NextCursor, out var cursor));
        Assert.Equal(second.ChangedAt, cursor!.Position.ChangedAt);
        Assert.Equal(second.ChangeSetId, cursor.Position.ChangeSetId);
        Assert.Equal([BookField.Title], cursor.ChangedFields);
    }

    [Fact]
    public async Task HandleAsync_FinalPageHasNoNextCursor()
    {
        var page = new BookHistoryPage(
            [CreateChangeSet(DateTimeOffset.UtcNow)],
            false);
        var handler = CreateHandler(
            CreateBook(),
            new RecordingBookChangeRepository(page));

        var result = await handler.HandleAsync(new GetBookHistoryQuery(42));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.HasMore);
        Assert.Null(result.Value.NextCursor);
    }

    private static GetBookHistoryHandler CreateHandler(
        Book? book,
        RecordingBookChangeRepository reader,
        BookHistoryCursorCodec? codec = null) =>
        new(
            new RecordingBookRepository(book),
            reader,
            codec ?? new BookHistoryCursorCodec());

    private static Book CreateBook() =>
        new(
            "The Dispossessed",
            "An ambiguous utopia.",
            new DateOnly(1974, 5, 1),
            ["Ursula K. Le Guin"]);

    private static BookHistoryChangeSet CreateChangeSet(DateTimeOffset changedAt) =>
        new(
            Guid.NewGuid(),
            changedAt,
            [
                new BookHistoryChange(
                    1,
                    BookField.Title,
                    "Old title",
                    "New title"),
                new BookHistoryChange(
                    2,
                    BookField.Authors,
                    new[] { "Old Author" },
                    new[] { "New Author" })
            ]);

    private sealed class RecordingBookRepository(Book? book) : IBookRepository
    {
        public int CallCount { get; private set; }

        public Task<Book?> GetByIdAsync(
            long id,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(book);
        }

        public Task<PagedBooks> SearchAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Add(Book value) => throw new NotSupportedException();

        public void Update(Book value, long expectedVersion) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingBookChangeRepository(BookHistoryPage page)
        : IBookChangeRepository
    {
        public int CallCount { get; private set; }

        public BookHistoryCriteria? Criteria { get; private set; }

        public Task<BookHistoryPage> ReadHistoryAsync(
            BookHistoryCriteria criteria,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Criteria = criteria;
            return Task.FromResult(page);
        }

        public void AddRange(IReadOnlyCollection<BookChange> changes) =>
            throw new NotSupportedException();
    }
}
