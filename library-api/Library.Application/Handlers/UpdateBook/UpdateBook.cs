using Library.Application.Abstractions;
using Library.Application.Common;
using Library.Application.Exceptions;
using Library.Domain.Books;
using Library.Domain.Exceptions;

namespace Library.Application.Handlers.UpdateBook;

public sealed record UpdateBookCommand(
    long Id,
    long? ExpectedVersion,
    string? Title,
    string? ShortDescription,
    DateOnly? PublishDate,
    IReadOnlyCollection<string?>? Authors);

public sealed record UpdateBookResult(
    BookResult Book,
    bool WasUpdated);

public sealed class UpdateBookHandler(
    IBookRepository bookRepository,
    IBookChangeRepository bookChangeRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    IChangeSetIdGenerator changeSetIdGenerator)
{
    public async Task<Result<UpdateBookResult>> HandleAsync(
        UpdateBookCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ExpectedVersion is null)
        {
            return Result<UpdateBookResult>.Failure(ApplicationError.PreconditionRequired(
                "book.if_match_required",
                "The If-Match header is required."));
        }

        var book = await bookRepository.GetByIdAsync(command.Id, cancellationToken);
        if (book is null)
        {
            return Result<UpdateBookResult>.Failure(ApplicationError.NotFound(
                "book.not_found",
                $"Book {command.Id} was not found."));
        }

        if (book.Version != command.ExpectedVersion.Value)
        {
            return Stale(command.Id);
        }

        IReadOnlyList<BookFieldChange> fieldChanges;
        try
        {
            fieldChanges = book.Update(
                command.Title,
                command.ShortDescription,
                command.PublishDate,
                command.Authors);
        }
        catch (BookValidationException exception)
        {
            return Result<UpdateBookResult>.Failure(ApplicationError.Validation(
                "book.validation_failed",
                "One or more Book fields are invalid.",
                exception.Errors));
        }

        if (fieldChanges.Count == 0)
        {
            return Result<UpdateBookResult>.Success(new UpdateBookResult(
                BookResult.FromDomain(book),
                WasUpdated: false));
        }

        var changeSetId = changeSetIdGenerator.NewId();
        var changedAt = clock.UtcNow;
        var changes = fieldChanges
            .Select(change => new BookChange(
                book,
                changeSetId,
                change.ChangedField,
                change.OldValue,
                change.NewValue,
                changedAt))
            .ToArray();

        bookRepository.Update(book, command.ExpectedVersion.Value);
        bookChangeRepository.AddRange(changes);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Stale(command.Id);
        }

        return Result<UpdateBookResult>.Success(new UpdateBookResult(
            BookResult.FromDomain(book),
            WasUpdated: true));
    }

    private static Result<UpdateBookResult> Stale(long id) =>
        Result<UpdateBookResult>.Failure(ApplicationError.PreconditionFailed(
            "book.precondition_failed",
            $"Book {id} has changed since the supplied ETag was issued."));
}
