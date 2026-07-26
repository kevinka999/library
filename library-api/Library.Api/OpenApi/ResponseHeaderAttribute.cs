using System.Globalization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Library.Api.OpenApi;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ResponseHeaderAttribute(
    int statusCode,
    string name,
    string description) : Attribute
{
    public int StatusCode { get; } = statusCode;

    public string Name { get; } = name;

    public string Description { get; } = description;
}

public sealed class ResponseHeaderOperationTransformer
    : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        foreach (var header in context.Description.ActionDescriptor.EndpointMetadata
                     .OfType<ResponseHeaderAttribute>())
        {
            var statusCode = header.StatusCode.ToString(CultureInfo.InvariantCulture);
            if (operation.Responses?.TryGetValue(statusCode, out var response) != true)
            {
                continue;
            }

            if (response is not OpenApiResponse mutableResponse)
            {
                continue;
            }

            mutableResponse.Headers ??= new Dictionary<string, IOpenApiHeader>();
            mutableResponse.Headers[header.Name] = new OpenApiHeader
            {
                Description = header.Description,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String
                }
            };
        }

        return Task.CompletedTask;
    }
}
