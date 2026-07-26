using Library.Domain.Books;

namespace Library.Application.Abstractions;

public interface IBookRepository
{
    void Add(Book book);
}
