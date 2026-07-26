using Library.Application.Abstractions;
using Library.Infrastructure.Persistence;
using Library.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LibraryDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'LibraryDatabase' is required. Configure it with user secrets or the ConnectionStrings__LibraryDatabase environment variable.");

        services.AddDbContext<LibraryDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<BookRepository>();
        services.AddScoped<IBookRepository>(provider => provider.GetRequiredService<BookRepository>());
        services.AddScoped<IBookReader, BookReader>();
        services.AddScoped<BookChangeRepository>();
        services.AddScoped<IBookChangeRepository>(
            provider => provider.GetRequiredService<BookChangeRepository>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IChangeSetIdGenerator, GuidChangeSetIdGenerator>();

        return services;
    }
}
