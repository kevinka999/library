using Library.Application.Abstractions;
using Library.Application.Common;
using Library.Domain.Books;

namespace Library.Application.Books.CreateBook;

public sealed class CreateBookHandler(
    IBookRepository bookRepository,
    IBookChangeRepository bookChangeRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    IChangeSetIdGenerator changeSetIdGenerator)
{
    public async Task<Result<BookDto>> HandleAsync(
        CreateBookCommand command,
        CancellationToken cancellationToken = default)
    {
        Book book;
        try
        {
            book = new Book(
                command.Title,
                command.ShortDescription,
                command.PublishDate,
                command.Authors);
        }
        catch (BookValidationException exception)
        {
            return Result<BookDto>.Failure(ApplicationError.Validation(
                "book.validation_failed",
                "One or more Book fields are invalid.",
                exception.Errors));
        }

        var changeSetId = changeSetIdGenerator.NewId();
        var changedAt = clock.UtcNow;
        var changes = new BookChange[]
        {
            new(book, changeSetId, BookField.Title, null, book.Title, changedAt),
            new(book, changeSetId, BookField.ShortDescription, null, book.ShortDescription, changedAt),
            new(book, changeSetId, BookField.PublishDate, null, book.PublishDate, changedAt),
            new(book, changeSetId, BookField.Authors, null, book.Authors.ToArray(), changedAt)
        };

        bookRepository.Add(book);
        bookChangeRepository.AddRange(changes);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<BookDto>.Success(BookDto.FromDomain(book));
    }
}
