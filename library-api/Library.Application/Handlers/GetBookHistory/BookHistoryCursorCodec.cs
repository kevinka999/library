using System.Text.Json;
using System.Text.Json.Serialization;
using Library.Application.Abstractions;
using Library.Domain.Books;

namespace Library.Application.Handlers.GetBookHistory;

public sealed class BookHistoryCursorCodec
{
    private const int CurrentVersion = 1;
    private const int MaximumCursorLength = 4096;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public string Encode(BookHistoryCursor cursor)
    {
        var payload = new CursorPayload(
            CurrentVersion,
            cursor.SortDirection,
            cursor.ChangedFields.Order().ToArray(),
            cursor.ChangedFrom,
            cursor.ChangedBefore,
            cursor.Position.ChangedAt,
            cursor.Position.ChangeSetId);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions);

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public bool TryDecode(string encoded, out BookHistoryCursor? cursor)
    {
        cursor = null;

        if (string.IsNullOrWhiteSpace(encoded)
            || encoded.Length > MaximumCursorLength
            || encoded.Any(character =>
                !char.IsAsciiLetterOrDigit(character)
                && character is not '-' and not '_'))
        {
            return false;
        }

        try
        {
            var base64 = encoded.Replace('-', '+').Replace('_', '/');
            base64 += new string('=', (4 - base64.Length % 4) % 4);
            var payload = JsonSerializer.Deserialize<CursorPayload>(
                Convert.FromBase64String(base64),
                SerializerOptions);

            if (payload is null
                || payload.Version != CurrentVersion
                || payload.ChangeSetId == Guid.Empty
                || payload.ChangedFields is null
                || payload.ChangedFields.Distinct().Count() != payload.ChangedFields.Length
                || payload.ChangedFields.Any(field => !Enum.IsDefined(field))
                || !Enum.IsDefined(payload.SortDirection)
                || !HasUtcOffset(payload.ChangedFrom)
                || !HasUtcOffset(payload.ChangedBefore)
                || !HasUtcOffset(payload.LastChangedAt))
            {
                return false;
            }

            cursor = new BookHistoryCursor(
                payload.SortDirection,
                payload.ChangedFields.Order().ToArray(),
                payload.ChangedFrom,
                payload.ChangedBefore,
                new BookHistoryPosition(payload.LastChangedAt, payload.ChangeSetId));
            return true;
        }
        catch (Exception exception) when (
            exception is FormatException
                or JsonException
                or NotSupportedException
                or ArgumentException)
        {
            return false;
        }
    }

    private static bool HasUtcOffset(DateTimeOffset? value) =>
        value is null || value.Value.Offset == TimeSpan.Zero;

    private sealed record CursorPayload(
        int Version,
        HistorySortDirection SortDirection,
        BookField[] ChangedFields,
        DateTimeOffset? ChangedFrom,
        DateTimeOffset? ChangedBefore,
        DateTimeOffset LastChangedAt,
        Guid ChangeSetId);
}

public sealed record BookHistoryCursor(
    HistorySortDirection SortDirection,
    IReadOnlyList<BookField> ChangedFields,
    DateTimeOffset? ChangedFrom,
    DateTimeOffset? ChangedBefore,
    BookHistoryPosition Position);
