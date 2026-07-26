using Library.Application.Books;

namespace Library.Api.Books;

public static class BookHttpResponseMapper
{
    public static BookResponse ToResponse(BookDto book) => BookResponse.FromApplication(book);

    public static string ToETag(long version) => $"\"{version}\"";
}
