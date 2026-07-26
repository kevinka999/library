using Library.Domain.Books;

namespace Library.Application.Handlers;

public sealed record BookResult(
    long Id,
    string Title,
    string ShortDescription,
    DateOnly PublishDate,
    IReadOnlyList<string> Authors,
    long Version)
{
    public static BookResult FromDomain(Book book) =>
        new(
            book.Id,
            book.Title,
            book.ShortDescription,
            book.PublishDate,
            book.Authors,
            book.Version);
}
