using Library.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Errors;

public static class ApplicationErrorHttpMapper
{
    public static ProblemDetails ToProblemDetails(this ApplicationError error)
    {
        var status = error.Type switch
        {
            ApplicationErrorType.Validation => StatusCodes.Status400BadRequest,
            ApplicationErrorType.NotFound => StatusCodes.Status404NotFound,
            ApplicationErrorType.PreconditionFailed => StatusCodes.Status412PreconditionFailed,
            ApplicationErrorType.PreconditionRequired => StatusCodes.Status428PreconditionRequired,
            _ => throw new ArgumentOutOfRangeException(nameof(error), error.Type, "Unknown error type.")
        };

        ProblemDetails problemDetails = error.Type == ApplicationErrorType.Validation
            ? new HttpValidationProblemDetails(error.Errors ?? new Dictionary<string, string[]>())
            : new ProblemDetails();

        problemDetails.Status = status;
        problemDetails.Title = error.Title;
        problemDetails.Detail = error.Detail;
        problemDetails.Extensions["code"] = error.Code;

        return problemDetails;
    }

    public static ObjectResult ToActionResult(this ApplicationError error)
    {
        var problemDetails = error.ToProblemDetails();

        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };
    }
}
