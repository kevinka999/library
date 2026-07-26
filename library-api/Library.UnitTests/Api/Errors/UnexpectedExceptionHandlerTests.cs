using Library.Api.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Library.UnitTests.Api.Errors;

public sealed class UnexpectedExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_DoesNotExposeExceptionDetails()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();
        await using var serviceProvider = services.BuildServiceProvider();

        var problemDetailsService = serviceProvider.GetRequiredService<IProblemDetailsService>();
        var handler = new UnexpectedExceptionHandler(
            NullLogger<UnexpectedExceptionHandler>.Instance,
            problemDetailsService);
        var responseBody = new MemoryStream();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Response.Body = responseBody;

        var handled = await handler.TryHandleAsync(
            httpContext,
            new InvalidOperationException("sensitive database detail"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        Assert.StartsWith("application/problem+json", httpContext.Response.ContentType);

        responseBody.Position = 0;
        using var reader = new StreamReader(responseBody);
        var response = await reader.ReadToEndAsync(CancellationToken.None);

        Assert.Contains("\"code\":\"unexpected_error\"", response);
        Assert.DoesNotContain("sensitive", response, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("database", response, StringComparison.OrdinalIgnoreCase);
    }
}
