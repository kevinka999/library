using Microsoft.Extensions.DependencyInjection;
using Library.Application.Books.CreateBook;

namespace Library.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateBookHandler>();

        return services;
    }
}
