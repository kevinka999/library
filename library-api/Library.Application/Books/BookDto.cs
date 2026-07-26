using Library.Domain.Books;

namespace Library.Application.Books;

public sealed record BookDto(
    long Id,
    string Title,
    string ShortDescription,
    DateOnly PublishDate,
    IReadOnlyList<string> Authors,
    long Version)
{
    public static BookDto FromDomain(Book book) =>
        new(
            book.Id,
            book.Title,
            book.ShortDescription,
            book.PublishDate,
            book.Authors,
            book.Version);
}
