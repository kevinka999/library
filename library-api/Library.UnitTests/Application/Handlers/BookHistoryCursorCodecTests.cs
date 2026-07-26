using System.Text;
using Library.Application.Abstractions;
using Library.Application.Handlers.GetBookHistory;
using Library.Domain.Books;

namespace Library.UnitTests.Application.Handlers;

public sealed class BookHistoryCursorCodecTests
{
    [Fact]
    public void EncodeAndDecode_RoundTripsNormalizedState()
    {
        var codec = new BookHistoryCursorCodec();
        var cursor = new BookHistoryCursor(
            HistorySortDirection.Ascending,
            [BookField.Authors, BookField.Title],
            new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
            new BookHistoryPosition(
                new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero),
                Guid.NewGuid()));

        var encoded = codec.Encode(cursor);
        var decoded = codec.TryDecode(encoded, out var result);

        Assert.True(decoded);
        Assert.NotNull(result);
        Assert.Equal(cursor.SortDirection, result.SortDirection);
        Assert.Equal(
            [BookField.Title, BookField.Authors],
            result.ChangedFields);
        Assert.Equal(cursor.ChangedFrom, result.ChangedFrom);
        Assert.Equal(cursor.ChangedBefore, result.ChangedBefore);
        Assert.Equal(cursor.Position, result.Position);
        Assert.DoesNotContain("=", encoded);
    }

    [Fact]
    public void TryDecode_RejectsUnsupportedVersion()
    {
        var json = """
            {
              "version": 2,
              "sortDirection": "descending",
              "changedFields": [],
              "changedFrom": null,
              "changedBefore": null,
              "lastChangedAt": "2026-07-20T12:00:00+00:00",
              "changeSetId": "68a6f68a-b57e-4940-b95e-201bf0936177"
            }
            """;
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var decoded = new BookHistoryCursorCodec()
            .TryDecode(encoded, out var cursor);

        Assert.False(decoded);
        Assert.Null(cursor);
    }
}
