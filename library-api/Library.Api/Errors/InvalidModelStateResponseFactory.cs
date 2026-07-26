using Library.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Errors;

public static class InvalidModelStateResponseFactory
{
    public static IActionResult Create(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToDictionary(
                entry => NormalizeKey(entry.Key),
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "The supplied value is invalid."
                        : error.ErrorMessage)
                    .ToArray());

        return ApplicationError.Validation(
                "book.validation_failed",
                "One or more Book fields are invalid.",
                errors)
            .ToActionResult();
    }

    private static string NormalizeKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return "request";
        }

        return char.ToLowerInvariant(key[0]) + key[1..];
    }
}
