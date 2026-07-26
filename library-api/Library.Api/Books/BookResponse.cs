using Library.Application.Books;

namespace Library.Api.Books;

public sealed record BookResponse(
    long Id,
    string Title,
    string ShortDescription,
    DateOnly PublishDate,
    IReadOnlyList<string> Authors,
    long Version)
{
    public static BookResponse FromApplication(BookDto book) =>
        new(
            book.Id,
            book.Title,
            book.ShortDescription,
            book.PublishDate,
            book.Authors,
            book.Version);
}
