using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Library.Api.OpenApi;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ProducesETagAttribute(int statusCode) : Attribute
{
    public int StatusCode { get; } = statusCode;
}

public sealed class ETagOpenApiTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<ProducesETagAttribute>()
            .SingleOrDefault();

        if (metadata is null
            || operation.Responses?.TryGetValue(
                metadata.StatusCode.ToString(),
                out var response) != true
            || response is null)
        {
            return Task.CompletedTask;
        }

        var headers = response.Headers;
        if (headers is null)
        {
            if (response is not OpenApiResponse concreteResponse)
            {
                return Task.CompletedTask;
            }

            concreteResponse.Headers = new Dictionary<string, IOpenApiHeader>();
            headers = concreteResponse.Headers;
        }

        headers["ETag"] = new OpenApiHeader
        {
            Description = "Strong ETag containing the decimal Book version.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
        };

        return Task.CompletedTask;
    }
}
