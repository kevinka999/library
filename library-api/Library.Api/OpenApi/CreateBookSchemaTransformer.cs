using Library.Api.Books;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Library.Api.OpenApi;

public sealed class CreateBookSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type == typeof(CreateBookRequest))
        {
            schema.Required = new HashSet<string>
            {
                "title",
                "shortDescription",
                "publishDate",
                "authors"
            };
        }

        return Task.CompletedTask;
    }
}
