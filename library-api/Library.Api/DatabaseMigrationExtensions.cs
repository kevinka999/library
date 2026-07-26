using Library.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Library.Api;

public static class DatabaseMigrationExtensions
{
    public static async Task ApplyDevelopmentMigrationsAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        await database.Database.MigrateAsync();
    }
}
