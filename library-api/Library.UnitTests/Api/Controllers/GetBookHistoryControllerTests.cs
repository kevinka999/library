using Library.Api.Controllers.GetBookHistory;
using Library.Application.Abstractions;
using Library.Application.Handlers.GetBookHistory;
using Library.Domain.Books;

namespace Library.UnitTests.Api.Controllers;

public sealed class GetBookHistoryControllerTests
{
    [Fact]
    public void InputDto_UsesDocumentedDefaults()
    {
        var input = new GetBookHistoryInputDto();

        Assert.Empty(input.ChangedField);
        Assert.Equal(20, input.Limit);
        Assert.Null(input.SortDirection);
    }

    [Fact]
    public void FromResult_MapsCompleteChangeSetAndNaturalValues()
    {
        var changeSetId = Guid.NewGuid();
        var changedAt = DateTimeOffset.UtcNow;
        var result = new GetBookHistoryResult(
            [
                new BookHistoryChangeSet(
                    changeSetId,
                    changedAt,
                    [
                        new BookHistoryChange(
                            10,
                            BookField.PublishDate,
                            new DateOnly(2020, 1, 1),
                            new DateOnly(2021, 2, 2)),
                        new BookHistoryChange(
                            11,
                            BookField.Authors,
                            null,
                            new[] { "Octavia E. Butler" })
                    ])
            ],
            "next",
            true);

        var output = GetBookHistoryOutputDto.FromResult(result);

        Assert.True(output.HasMore);
        Assert.Equal("next", output.NextCursor);
        var item = Assert.Single(output.Items);
        Assert.Equal(changeSetId, item.ChangeSetId);
        Assert.Equal(changedAt, item.ChangedAt);
        Assert.Equal(2, item.Changes.Count);
        Assert.Equal("publishDate", item.Changes[0].ChangedField);
        Assert.IsType<DateOnly>(item.Changes[0].OldValue);
        Assert.Equal("authors", item.Changes[1].ChangedField);
        Assert.Null(item.Changes[1].OldValue);
        Assert.Equal(
            ["Octavia E. Butler"],
            Assert.IsType<string[]>(item.Changes[1].NewValue));
    }

    [Fact]
    public void QueryParameterValidation_AllowsRepeatedChangedField()
    {
        var error = GetBookHistoryQueryParameters.Validate(
        [
            KeyValuePair.Create("changedField", 3),
            KeyValuePair.Create("changedFrom", 1),
            KeyValuePair.Create("changedBefore", 1),
            KeyValuePair.Create("sortDirection", 1),
            KeyValuePair.Create("limit", 1),
            KeyValuePair.Create("after", 1)
        ]);

        Assert.Null(error);
    }

    [Fact]
    public void QueryParameterValidation_RejectsUnknownAndRepeatedScalarParameters()
    {
        var error = GetBookHistoryQueryParameters.Validate(
        [
            KeyValuePair.Create("limit", 2),
            KeyValuePair.Create("page", 1)
        ]);

        Assert.NotNull(error);
        Assert.Equal("book.history_validation_failed", error.Code);
        Assert.Equal(["limit", "page"], error.Errors!.Keys.Order());
    }
}
