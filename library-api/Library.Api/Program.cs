using System.Text.Json;
using Library.Api.Errors;
using Library.Application;
using Library.Infrastructure;

const string openApiDocumentName = "v1";

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<UnexpectedExceptionHandler>();
builder.Services.AddOpenApi(openApiDocumentName);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("ETag"));
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi($"/openapi/{openApiDocumentName}.json");
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.SwaggerEndpoint(
            $"/openapi/{openApiDocumentName}.json",
            "Library API v1");
    });
}

app.MapControllers();

app.Run();

public partial class Program;
