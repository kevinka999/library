using Library.Application.Abstractions;
using Library.Infrastructure.Persistence.Repositories;

namespace Library.Infrastructure.Persistence;

internal sealed class UnitOfWork(
    LibraryDbContext database,
    BookRepository bookRepository)
    : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await database.SaveChangesAsync(cancellationToken);
        bookRepository.SynchronizeGeneratedIds();
    }
}
