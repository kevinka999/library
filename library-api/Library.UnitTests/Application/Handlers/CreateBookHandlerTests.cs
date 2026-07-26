using Library.Application.Abstractions;
using Library.Application.Handlers.CreateBook;
using Library.Domain.Books;

namespace Library.UnitTests.Application.Handlers;

public sealed class CreateBookHandlerTests
{
    private static readonly Guid ChangeSetId = Guid.Parse("226b370b-78b4-489c-94d3-af83db0ab145");
    private static readonly DateTimeOffset Now = new(2026, 7, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_PersistsBookAndChangesThenCommitsOnce()
    {
        var bookRepository = new RecordingBookRepository();
        var changeRepository = new RecordingBookChangeRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(bookRepository, changeRepository, unitOfWork);

        var result = await handler.HandleAsync(new CreateBookCommand(
            "  The Left Hand of Darkness ",
            "  A science fiction novel. ",
            new DateOnly(1969, 3, 1),
            [" Ursula K. Le Guin "]));

        Assert.True(result.IsSuccess);
        Assert.Equal("The Left Hand of Darkness", result.Value!.Title);
        Assert.Equal("A science fiction novel.", result.Value.ShortDescription);
        Assert.Equal(["Ursula K. Le Guin"], result.Value.Authors);
        Assert.Equal(1, result.Value.Version);

        Assert.NotNull(bookRepository.Book);
        Assert.Equal(4, changeRepository.Changes.Count);
        Assert.All(changeRepository.Changes, change =>
        {
            Assert.Equal(ChangeSetId, change.ChangeSetId);
            Assert.Equal(Now, change.ChangedAt);
            Assert.Null(change.OldValue);
        });
        Assert.Collection(
            changeRepository.Changes,
            change =>
            {
                Assert.Equal(BookField.Title, change.ChangedField);
                Assert.Equal("The Left Hand of Darkness", change.NewValue);
            },
            change =>
            {
                Assert.Equal(BookField.ShortDescription, change.ChangedField);
                Assert.Equal("A science fiction novel.", change.NewValue);
            },
            change =>
            {
                Assert.Equal(BookField.PublishDate, change.ChangedField);
                Assert.Equal(new DateOnly(1969, 3, 1), change.NewValue);
            },
            change =>
            {
                Assert.Equal(BookField.Authors, change.ChangedField);
                var value = Assert.IsType<string[]>(change.NewValue);
                Assert.Equal(["Ursula K. Le Guin"], value);
            });
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_InvalidInputReturnsAllErrorsAndWritesNothing()
    {
        var bookRepository = new RecordingBookRepository();
        var changeRepository = new RecordingBookChangeRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(bookRepository, changeRepository, unitOfWork);

        var result = await handler.HandleAsync(new CreateBookCommand(
            " ",
            null,
            null,
            ["same", "SAME", " "]));

        Assert.False(result.IsSuccess);
        Assert.Equal("book.validation_failed", result.Error!.Code);
        Assert.Equal(
            ["authors", "authors[2]", "publishDate", "shortDescription", "title"],
            result.Error.Errors!.Keys.Order());
        Assert.Null(bookRepository.Book);
        Assert.Empty(changeRepository.Changes);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    private static CreateBookHandler CreateHandler(
        RecordingBookRepository bookRepository,
        RecordingBookChangeRepository changeRepository,
        RecordingUnitOfWork unitOfWork) =>
        new(
            bookRepository,
            changeRepository,
            unitOfWork,
            new StubClock(Now),
            new StubIdGenerator(ChangeSetId));

    private sealed class RecordingBookRepository : IBookRepository
    {
        public Book? Book { get; private set; }

        public Task<Book?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PagedBooks> SearchAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Add(Book book)
        {
            Book = book;
        }

        public void Update(Book book, long expectedVersion) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingBookChangeRepository : IBookChangeRepository
    {
        public IReadOnlyCollection<BookChange> Changes { get; private set; } = [];

        public void AddRange(IReadOnlyCollection<BookChange> changes)
        {
            Changes = changes;
        }

        public Task<BookHistoryPage> ReadHistoryAsync(
            BookHistoryCriteria criteria,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed record StubClock(DateTimeOffset UtcNow) : IClock;

    private sealed record StubIdGenerator(Guid Id) : IChangeSetIdGenerator
    {
        public Guid NewId() => Id;
    }
}
