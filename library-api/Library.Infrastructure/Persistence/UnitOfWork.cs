using Library.Application.Abstractions;
using Library.Application.Exceptions;
using Library.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence;

internal sealed class UnitOfWork(
    LibraryDbContext database,
    BookRepository bookRepository)
    : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(exception);
        }

        bookRepository.SynchronizeGeneratedIds();
    }
}
