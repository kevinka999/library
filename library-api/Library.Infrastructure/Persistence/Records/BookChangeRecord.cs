using System.Text.Json;

namespace Library.Infrastructure.Persistence.Records;

internal sealed class BookChangeRecord(
    BookRecord book,
    Guid changeSetId,
    string changedField,
    JsonDocument? oldValue,
    JsonDocument newValue,
    DateTimeOffset changedAt)
{
    private BookChangeRecord()
        : this(null!, Guid.Empty, string.Empty, null, null!, default)
    {
    }

    public long Id { get; private set; }

    public long BookId { get; private set; }

    public BookRecord Book { get; private set; } = book;

    public Guid ChangeSetId { get; private set; } = changeSetId;

    public string ChangedField { get; private set; } = changedField;

    public JsonDocument? OldValue { get; private set; } = oldValue;

    public JsonDocument NewValue { get; private set; } = newValue;

    public DateTimeOffset ChangedAt { get; private set; } = changedAt;
}
