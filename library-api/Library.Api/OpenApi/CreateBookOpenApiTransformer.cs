using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Library.Api.OpenApi;

public sealed class CreateBookOpenApiTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.HttpMethod != HttpMethods.Post
            || context.Description.RelativePath != "api/books")
        {
            return Task.CompletedTask;
        }

        if (operation.RequestBody?.Content is { } requestBodyContent
            && requestBodyContent.TryGetValue("application/json", out var requestContent))
        {
            requestContent.Example = JsonNode.Parse(
                """
                {
                  "title": "The Left Hand of Darkness",
                  "shortDescription": "A science fiction novel.",
                  "publishDate": "1969-03-01",
                  "authors": ["Ursula K. Le Guin"]
                }
                """);
        }

        if (operation.Responses?.TryGetValue("201", out var createdResponse) == true)
        {
            var headers = createdResponse.Headers;
            if (headers is null && createdResponse is OpenApiResponse concreteResponse)
            {
                concreteResponse.Headers = new Dictionary<string, IOpenApiHeader>();
                headers = concreteResponse.Headers;
            }

            headers!["Location"] = new OpenApiHeader
            {
                Description = "Canonical path of the created Book.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String },
                Example = JsonValue.Create("/api/books/1")
            };
            headers["ETag"] = new OpenApiHeader
            {
                Description = "Strong ETag containing the decimal Book version.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String },
                Example = JsonValue.Create("\"1\"")
            };

            if (createdResponse.Content?.TryGetValue(
                    "application/json",
                    out var responseContent) == true)
            {
                responseContent.Example = JsonNode.Parse(
                    """
                    {
                      "id": 1,
                      "title": "The Left Hand of Darkness",
                      "shortDescription": "A science fiction novel.",
                      "publishDate": "1969-03-01",
                      "authors": ["Ursula K. Le Guin"],
                      "version": 1
                    }
                    """);
            }
        }

        if (operation.Responses?.TryGetValue("400", out var badRequestResponse) == true
            && badRequestResponse.Content?.TryGetValue(
                "application/problem+json",
                out var problemContent) == true)
        {
            problemContent.Example = JsonNode.Parse(
                """
                {
                  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                  "title": "Validation failed",
                  "status": 400,
                  "detail": "One or more Book fields are invalid.",
                  "code": "book.validation_failed",
                  "errors": {
                    "title": ["Title is required."]
                  }
                }
                """);
        }

        return Task.CompletedTask;
    }
}
