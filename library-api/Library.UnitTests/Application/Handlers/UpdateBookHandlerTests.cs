using Library.Application.Abstractions;
using Library.Application.Exceptions;
using Library.Application.Handlers.UpdateBook;
using Library.Domain.Books;

namespace Library.UnitTests.Application.Handlers;

public sealed class UpdateBookHandlerTests
{
    private static readonly Guid ChangeSetId =
        Guid.Parse("e67bda0b-77da-4d99-94b8-612ebaf93bd5");

    private static readonly DateTimeOffset Now =
        new(2026, 7, 26, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_EffectiveUpdateStagesOneCompleteChangeSetAndCommitsOnce()
    {
        var book = CreateBook();
        var bookRepository = new RecordingBookRepository(book);
        var changeRepository = new RecordingBookChangeRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(bookRepository, changeRepository, unitOfWork);

        var result = await handler.HandleAsync(Command(
            expectedVersion: 1,
            title: "  The Dispossessed ",
            shortDescription: "  An ambiguous utopia. ",
            authors: [" Ursula K. Le Guin ", "Samuel R. Delany"]));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.WasUpdated);
        Assert.Equal("The Dispossessed", result.Value.Book.Title);
        Assert.Equal("An ambiguous utopia.", result.Value.Book.ShortDescription);
        Assert.Equal(
            ["Samuel R. Delany", "Ursula K. Le Guin"],
            result.Value.Book.Authors);
        Assert.Equal(2, result.Value.Book.Version);
        Assert.Equal(1, bookRepository.UpdateCount);
        Assert.Equal(1, bookRepository.ExpectedVersion);
        Assert.Equal(1, unitOfWork.SaveCount);
        Assert.Collection(
            changeRepository.Changes,
            change =>
            {
                Assert.Equal(BookField.Title, change.ChangedField);
                Assert.Equal("The Left Hand of Darkness", change.OldValue);
                Assert.Equal("The Dispossessed", change.NewValue);
            },
            change =>
            {
                Assert.Equal(BookField.ShortDescription, change.ChangedField);
                Assert.Equal("A science fiction novel.", change.OldValue);
                Assert.Equal("An ambiguous utopia.", change.NewValue);
            },
            change =>
            {
                Assert.Equal(BookField.Authors, change.ChangedField);
                Assert.Equal(
                    ["Ursula K. Le Guin"],
                    Assert.IsType<string[]>(change.OldValue));
                Assert.Equal(
                    ["Samuel R. Delany", "Ursula K. Le Guin"],
                    Assert.IsType<string[]>(change.NewValue));
            });
        Assert.All(changeRepository.Changes, change =>
        {
            Assert.Same(book, change.Book);
            Assert.Equal(ChangeSetId, change.ChangeSetId);
            Assert.Equal(Now, change.ChangedAt);
        });
    }

    [Fact]
    public async Task HandleAsync_CurrentVersionNoOpDoesNotStageOrCommit()
    {
        var bookRepository = new RecordingBookRepository(CreateBook());
        var changeRepository = new RecordingBookChangeRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(bookRepository, changeRepository, unitOfWork);

        var result = await handler.HandleAsync(Command(
            expectedVersion: 1,
            title: " The Left Hand of Darkness ",
            shortDescription: " A science fiction novel. ",
            authors: ["Ursula K. Le Guin"]));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.WasUpdated);
        Assert.Equal(1, result.Value.Book.Version);
        Assert.Equal(0, bookRepository.UpdateCount);
        Assert.Empty(changeRepository.Changes);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_StaleVersionIsCheckedBeforeNoOpDetection()
    {
        var book = CreateBook();
        var bookRepository = new RecordingBookRepository(book);
        var changeRepository = new RecordingBookChangeRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(bookRepository, changeRepository, unitOfWork);

        var result = await handler.HandleAsync(Command(expectedVersion: 2));

        Assert.False(result.IsSuccess);
        Assert.Equal("book.precondition_failed", result.Error!.Code);
        Assert.Equal(1, book.Version);
        Assert.Equal(0, bookRepository.UpdateCount);
        Assert.Empty(changeRepository.Changes);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_MissingPreconditionDoesNotReadOrWrite()
    {
        var bookRepository = new RecordingBookRepository(CreateBook());
        var changeRepository = new RecordingBookChangeRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(bookRepository, changeRepository, unitOfWork);

        var result = await handler.HandleAsync(Command(expectedVersion: null));

        Assert.False(result.IsSuccess);
        Assert.Equal("book.if_match_required", result.Error!.Code);
        Assert.Equal(0, bookRepository.GetCount);
        Assert.Equal(0, bookRepository.UpdateCount);
        Assert.Empty(changeRepository.Changes);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_MissingBookReturnsNotFoundWithoutWrites()
    {
        var bookRepository = new RecordingBookRepository(null);
        var changeRepository = new RecordingBookChangeRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(bookRepository, changeRepository, unitOfWork);

        var result = await handler.HandleAsync(Command(expectedVersion: 1));

        Assert.False(result.IsSuccess);
        Assert.Equal("book.not_found", result.Error!.Code);
        Assert.Equal(1, bookRepository.GetCount);
        Assert.Equal(0, bookRepository.UpdateCount);
        Assert.Empty(changeRepository.Changes);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_InvalidReplacementDoesNotMutateOrWrite()
    {
        var book = CreateBook();
        var bookRepository = new RecordingBookRepository(book);
        var changeRepository = new RecordingBookChangeRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(bookRepository, changeRepository, unitOfWork);

        var result = await handler.HandleAsync(new UpdateBookCommand(
            42,
            1,
            " ",
            null,
            null,
            ["same", "SAME", " "]));

        Assert.False(result.IsSuccess);
        Assert.Equal("book.validation_failed", result.Error!.Code);
        Assert.Equal(1, book.Version);
        Assert.Equal("The Left Hand of Darkness", book.Title);
        Assert.Equal(0, bookRepository.UpdateCount);
        Assert.Empty(changeRepository.Changes);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_DatabaseConcurrencyConflictReturnsStaleResult()
    {
        var bookRepository = new RecordingBookRepository(CreateBook());
        var changeRepository = new RecordingBookChangeRepository();
        var unitOfWork = new RecordingUnitOfWork
        {
            Exception = new ConcurrencyConflictException(new InvalidOperationException())
        };
        var handler = CreateHandler(bookRepository, changeRepository, unitOfWork);

        var result = await handler.HandleAsync(Command(
            expectedVersion: 1,
            title: "The Dispossessed"));

        Assert.False(result.IsSuccess);
        Assert.Equal("book.precondition_failed", result.Error!.Code);
        Assert.Equal(1, bookRepository.UpdateCount);
        Assert.Single(changeRepository.Changes);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    private static UpdateBookCommand Command(
        long? expectedVersion,
        string title = "The Left Hand of Darkness",
        string shortDescription = "A science fiction novel.",
        DateOnly? publishDate = null,
        IReadOnlyCollection<string?>? authors = null) =>
        new(
            42,
            expectedVersion,
            title,
            shortDescription,
            publishDate ?? new DateOnly(1969, 3, 1),
            authors ?? ["Ursula K. Le Guin"]);

    private static Book CreateBook() =>
        new(
            "The Left Hand of Darkness",
            "A science fiction novel.",
            new DateOnly(1969, 3, 1),
            ["Ursula K. Le Guin"]);

    private static UpdateBookHandler CreateHandler(
        RecordingBookRepository bookRepository,
        RecordingBookChangeRepository changeRepository,
        RecordingUnitOfWork unitOfWork) =>
        new(
            bookRepository,
            changeRepository,
            unitOfWork,
            new StubClock(Now),
            new StubIdGenerator(ChangeSetId));

    private sealed class RecordingBookRepository(Book? book) : IBookRepository
    {
        public int GetCount { get; private set; }

        public int UpdateCount { get; private set; }

        public long? ExpectedVersion { get; private set; }

        public Task<Book?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            GetCount++;
            return Task.FromResult(book);
        }

        public Task<PagedBooks> SearchAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Add(Book addedBook) => throw new NotSupportedException();

        public void Update(Book updatedBook, long expectedVersion)
        {
            Assert.Same(book, updatedBook);
            UpdateCount++;
            ExpectedVersion = expectedVersion;
        }
    }

    private sealed class RecordingBookChangeRepository : IBookChangeRepository
    {
        public IReadOnlyCollection<BookChange> Changes { get; private set; } = [];

        public void AddRange(IReadOnlyCollection<BookChange> changes)
        {
            Changes = changes;
        }
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public Exception? Exception { get; init; }

        public int SaveCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Exception is null
                ? Task.CompletedTask
                : Task.FromException(Exception);
        }
    }

    private sealed record StubClock(DateTimeOffset UtcNow) : IClock;

    private sealed record StubIdGenerator(Guid Id) : IChangeSetIdGenerator
    {
        public Guid NewId() => Id;
    }
}
