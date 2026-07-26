using Microsoft.Extensions.DependencyInjection;
using Library.Application.Handlers.CreateBook;
using Library.Application.Handlers.GetBook;

namespace Library.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateBookHandler>();
        services.AddScoped<GetBookHandler>();

        return services;
    }
}
