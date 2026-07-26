using Library.Api.Errors;
using Library.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Library.UnitTests.Api.Errors;

public sealed class ApplicationErrorHttpMapperTests
{
    public static TheoryData<ApplicationError, int> ExpectedMappings =>
        new()
        {
            {
                ApplicationError.Validation(
                    "invalid_request",
                    "One or more values are invalid.",
                    new Dictionary<string, string[]> { ["title"] = ["Title is required."] }),
                StatusCodes.Status400BadRequest
            },
            {
                ApplicationError.NotFound("book_not_found", "The requested Book was not found."),
                StatusCodes.Status404NotFound
            },
            {
                ApplicationError.PreconditionFailed("stale_version", "The Book has changed."),
                StatusCodes.Status412PreconditionFailed
            },
            {
                ApplicationError.PreconditionRequired("if_match_required", "If-Match is required."),
                StatusCodes.Status428PreconditionRequired
            }
        };

    [Theory]
    [MemberData(nameof(ExpectedMappings))]
    public void ToProblemDetails_MapsExpectedError(
        ApplicationError error,
        int expectedStatus)
    {
        var problemDetails = error.ToProblemDetails();

        Assert.Equal(expectedStatus, problemDetails.Status);
        Assert.Equal(error.Title, problemDetails.Title);
        Assert.Equal(error.Detail, problemDetails.Detail);
        Assert.Equal(error.Code, problemDetails.Extensions["code"]);
    }

    [Fact]
    public void ToProblemDetails_PreservesAllValidationErrors()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["title"] = ["Title is required."],
            ["authors"] = ["At least one Author Name is required.", "Author Names must be unique."]
        };
        var error = ApplicationError.Validation(
            "invalid_request",
            "One or more values are invalid.",
            errors);

        var problemDetails = Assert.IsType<HttpValidationProblemDetails>(error.ToProblemDetails());

        Assert.Equal(errors, problemDetails.Errors);
    }

    [Fact]
    public void ToActionResult_UsesProblemStatusAndBody()
    {
        var error = ApplicationError.NotFound(
            "book_not_found",
            "The requested Book was not found.");

        var result = error.ToActionResult();

        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        Assert.IsType<ProblemDetails>(result.Value);
    }
}
