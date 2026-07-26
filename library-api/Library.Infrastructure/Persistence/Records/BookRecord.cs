namespace Library.Infrastructure.Persistence.Records;

internal sealed class BookRecord(
    string title,
    string shortDescription,
    DateOnly publishDate,
    string[] authors,
    long version)
{
    private BookRecord()
        : this(string.Empty, string.Empty, default, [], default)
    {
    }

    public long Id { get; private set; }

    public string Title { get; private set; } = title;

    public string ShortDescription { get; private set; } = shortDescription;

    public DateOnly PublishDate { get; private set; } = publishDate;

    public string[] Authors { get; private set; } = authors;

    public long Version { get; private set; } = version;
}
