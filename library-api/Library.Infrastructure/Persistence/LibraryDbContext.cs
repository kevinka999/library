using Library.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence;

public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options)
    : DbContext(options)
{
    internal DbSet<BookRecord> Books => Set<BookRecord>();

    internal DbSet<BookChangeRecord> BookChanges => Set<BookChangeRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
    }
}
