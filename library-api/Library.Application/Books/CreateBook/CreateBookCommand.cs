namespace Library.Application.Books.CreateBook;

public sealed record CreateBookCommand(
    string? Title,
    string? ShortDescription,
    DateOnly? PublishDate,
    IReadOnlyCollection<string?>? Authors);
