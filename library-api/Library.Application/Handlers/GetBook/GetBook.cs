using Library.Application.Abstractions;
using Library.Application.Common;

namespace Library.Application.Handlers.GetBook;

public sealed record GetBookQuery(long Id);

public sealed class GetBookHandler(IBookRepository bookRepository)
{
    public async Task<Result<BookResult>> HandleAsync(
        GetBookQuery query,
        CancellationToken cancellationToken = default)
    {
        var book = await bookRepository.GetByIdAsync(query.Id, cancellationToken);

        return book is null
            ? Result<BookResult>.Failure(ApplicationError.NotFound(
                "book.not_found",
                $"Book {query.Id} was not found."))
            : Result<BookResult>.Success(BookResult.FromDomain(book));
    }
}
