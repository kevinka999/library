namespace Library.Domain.Books;

public sealed class BookChange
{
    public BookChange(
        Book book,
        Guid changeSetId,
        BookField changedField,
        object? oldValue,
        object newValue,
        DateTimeOffset changedAt,
        long id = 0)
    {
        Id = id;
        Book = book;
        ChangeSetId = changeSetId;
        ChangedField = changedField;
        OldValue = oldValue;
        NewValue = newValue;
        ChangedAt = changedAt;
    }

    public long Id { get; private set; }

    public Book Book { get; }

    public long BookId => Book.Id;

    public Guid ChangeSetId { get; private set; }

    public BookField ChangedField { get; private set; }

    public object? OldValue { get; private set; }

    public object NewValue { get; private set; } = null!;

    public DateTimeOffset ChangedAt { get; private set; }
}

public enum BookField
{
    Title,
    ShortDescription,
    PublishDate,
    Authors
}
