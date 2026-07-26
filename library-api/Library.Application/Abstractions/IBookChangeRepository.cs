using Library.Domain.Books;

namespace Library.Application.Abstractions;

public interface IBookChangeRepository
{
    void AddRange(IReadOnlyCollection<BookChange> changes);
}
