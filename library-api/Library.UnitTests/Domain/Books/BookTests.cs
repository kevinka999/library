using Library.Domain.Books;

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
