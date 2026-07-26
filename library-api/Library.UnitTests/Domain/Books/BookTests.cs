using Library.Domain.Books;
using Library.Domain.Exceptions;

namespace Library.UnitTests.Domain.Books;

public sealed class BookTests
{
    [Fact]
    public void Create_NormalizesValidState()
    {
        var book = Create(
            title: "  Domain-Driven Design  ",
            shortDescription: "  Tackling complexity in software.  ",
            authors: [" Eric Evans ", "Addison Wesley"]);

        Assert.Equal("Domain-Driven Design", book.Title);
        Assert.Equal("Tackling complexity in software.", book.ShortDescription);
        Assert.Equal(new DateOnly(2003, 8, 30), book.PublishDate);
        Assert.Equal(["Addison Wesley", "Eric Evans"], book.Authors);
        Assert.Equal(1, book.Version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsMissingOrBlankTitle(string? title)
    {
        AssertHasError(() => Create(title: title), "title");
    }

    [Fact]
    public void Create_AcceptsTitleAtMaximumLengthAndRejectsLongerTitle()
    {
        Create(title: new string('t', Book.MaxTitleLength));
        AssertHasError(() => Create(title: new string('t', Book.MaxTitleLength + 1)), "title");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsMissingOrBlankShortDescription(string? description)
    {
        AssertHasError(() => Create(shortDescription: description), "shortDescription");
    }

    [Fact]
    public void Create_AcceptsDescriptionAtMaximumLengthAndRejectsLongerDescription()
    {
        Create(shortDescription: new string('d', Book.MaxShortDescriptionLength));
        AssertHasError(
            () => Create(shortDescription: new string('d', Book.MaxShortDescriptionLength + 1)),
            "shortDescription");
    }

    [Fact]
    public void Create_RejectsMissingPublishDate()
    {
        AssertHasError(() => Create(omitPublishDate: true), "publishDate");
    }

    [Fact]
    public void Create_AllowsFuturePublishDate()
    {
        Create(publishDate: new DateOnly(2200, 1, 1));
    }

    [Fact]
    public void Create_RejectsMissingOrEmptyAuthors()
    {
        AssertHasError(() => Create(omitAuthors: true), "authors");
        AssertHasError(() => Create(authors: []), "authors");
    }

    [Theory]
    [MemberData(nameof(BlankAuthors))]
    public void Create_RejectsBlankAuthorNames(IReadOnlyCollection<string?> authors)
    {
        AssertHasError(() => Create(authors: authors), "authors[0]");
    }

    public static TheoryData<IReadOnlyCollection<string?>> BlankAuthors =>
        new()
        {
            new string?[] { null },
            new string?[] { "" },
            new string?[] { "   " }
        };

    [Fact]
    public void Create_AcceptsAuthorAtMaximumLengthAndRejectsLongerAuthor()
    {
        Create(authors: [new string('a', Book.MaxAuthorNameLength)]);
        AssertHasError(
            () => Create(authors: [new string('a', Book.MaxAuthorNameLength + 1)]),
            "authors[0]");
    }

    [Fact]
    public void Create_RejectsDuplicateAuthorsCaseInsensitively()
    {
        AssertHasError(
            () => Create(authors: ["Ursula Le Guin", "ursula le guin"]),
            "authors");
    }

    [Fact]
    public void Create_OrdersAuthorsDeterministicallyWithoutChangingDisplayCasing()
    {
        var first = Create(authors: ["zoe", "Amy", "bob"]).Authors;
        var second = Create(authors: ["bob", "zoe", "Amy"]).Authors;

        Assert.Equal(["Amy", "bob", "zoe"], first);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Update_ReportsEveryEffectiveFieldChangeAndIncrementsVersionOnce()
    {
        var book = Create(authors: ["Eric Evans"]);

        var changes = book.Update(
            "Implementing Domain-Driven Design",
            "A practical guide.",
            new DateOnly(2013, 2, 6),
            ["Vaughn Vernon", "Abel Avram"]);

        Assert.Equal("Implementing Domain-Driven Design", book.Title);
        Assert.Equal("A practical guide.", book.ShortDescription);
        Assert.Equal(new DateOnly(2013, 2, 6), book.PublishDate);
        Assert.Equal(["Abel Avram", "Vaughn Vernon"], book.Authors);
        Assert.Equal(2, book.Version);
        Assert.Collection(
            changes,
            change =>
            {
                Assert.Equal(BookField.Title, change.ChangedField);
                Assert.Equal("Domain-Driven Design", change.OldValue);
                Assert.Equal("Implementing Domain-Driven Design", change.NewValue);
            },
            change =>
            {
                Assert.Equal(BookField.ShortDescription, change.ChangedField);
                Assert.Equal("Tackling complexity in software.", change.OldValue);
                Assert.Equal("A practical guide.", change.NewValue);
            },
            change =>
            {
                Assert.Equal(BookField.PublishDate, change.ChangedField);
                Assert.Equal(new DateOnly(2003, 8, 30), change.OldValue);
                Assert.Equal(new DateOnly(2013, 2, 6), change.NewValue);
            },
            change =>
            {
                Assert.Equal(BookField.Authors, change.ChangedField);
                Assert.Equal(["Eric Evans"], Assert.IsType<string[]>(change.OldValue));
                Assert.Equal(
                    ["Abel Avram", "Vaughn Vernon"],
                    Assert.IsType<string[]>(change.NewValue));
            });
    }

    [Theory]
    [InlineData(BookField.Title)]
    [InlineData(BookField.ShortDescription)]
    [InlineData(BookField.PublishDate)]
    [InlineData(BookField.Authors)]
    public void Update_ReportsEachFieldIndependently(BookField changedField)
    {
        var book = Create(authors: ["Eric Evans"]);
        var title = book.Title;
        var shortDescription = book.ShortDescription;
        var publishDate = book.PublishDate;
        IReadOnlyCollection<string?> authors = book.Authors.ToArray();

        switch (changedField)
        {
            case BookField.Title:
                title = "Implementing Domain-Driven Design";
                break;
            case BookField.ShortDescription:
                shortDescription = "A practical guide.";
                break;
            case BookField.PublishDate:
                publishDate = new DateOnly(2013, 2, 6);
                break;
            case BookField.Authors:
                authors = ["Vaughn Vernon"];
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(changedField), changedField, null);
        }

        var change = Assert.Single(book.Update(
            title,
            shortDescription,
            publishDate,
            authors));

        Assert.Equal(changedField, change.ChangedField);
        Assert.Equal(2, book.Version);
    }

    [Fact]
    public void Update_NormalizedEquivalentStateIsANoOp()
    {
        var book = Create(authors: ["Eric Evans", "Addison Wesley"]);

        var changes = book.Update(
            "  Domain-Driven Design ",
            " Tackling complexity in software. ",
            new DateOnly(2003, 8, 30),
            [" Eric Evans ", "Addison Wesley"]);

        Assert.Empty(changes);
        Assert.Equal(1, book.Version);
        Assert.Equal(["Addison Wesley", "Eric Evans"], book.Authors);
    }

    [Fact]
    public void Update_AuthorOrderAloneIsANoOp()
    {
        var book = Create(authors: ["Ursula K. Le Guin", "Ann Leckie"]);

        var changes = book.Update(
            book.Title,
            book.ShortDescription,
            book.PublishDate,
            ["Ursula K. Le Guin", "Ann Leckie"]);

        Assert.Empty(changes);
        Assert.Equal(1, book.Version);
    }

    [Fact]
    public void Update_AuthorDisplayCasingCorrectionIsEffective()
    {
        var book = Create(authors: ["ursula k. le guin"]);

        var changes = book.Update(
            book.Title,
            book.ShortDescription,
            book.PublishDate,
            ["Ursula K. Le Guin"]);

        var change = Assert.Single(changes);
        Assert.Equal(BookField.Authors, change.ChangedField);
        Assert.Equal(["ursula k. le guin"], Assert.IsType<string[]>(change.OldValue));
        Assert.Equal(["Ursula K. Le Guin"], Assert.IsType<string[]>(change.NewValue));
        Assert.Equal(2, book.Version);
    }

    [Fact]
    public void Update_ValidatesEveryFieldBeforeMutatingState()
    {
        var book = Create();

        var exception = Assert.Throws<BookValidationException>(() => book.Update(
            " ",
            null,
            null,
            ["same", "SAME", " "]));

        Assert.Equal(
            ["authors", "authors[2]", "publishDate", "shortDescription", "title"],
            exception.Errors.Keys.Order());
        Assert.Equal("Domain-Driven Design", book.Title);
        Assert.Equal("Tackling complexity in software.", book.ShortDescription);
        Assert.Equal(new DateOnly(2003, 8, 30), book.PublishDate);
        Assert.Equal(["Eric Evans"], book.Authors);
        Assert.Equal(1, book.Version);
    }

    private static Book Create(
        string? title = "Domain-Driven Design",
        string? shortDescription = "Tackling complexity in software.",
        DateOnly? publishDate = default,
        IReadOnlyCollection<string?>? authors = default,
        bool omitPublishDate = false,
        bool omitAuthors = false)
    {
        if (!omitPublishDate)
        {
            publishDate ??= new DateOnly(2003, 8, 30);
        }

        if (!omitAuthors)
        {
            authors ??= ["Eric Evans"];
        }

        return new Book(
            title,
            shortDescription,
            publishDate,
            authors);
    }

    private static void AssertHasError(Func<Book> create, string field)
    {
        var exception = Assert.Throws<BookValidationException>(create);
        Assert.Contains(field, exception.Errors.Keys);
    }
}
