namespace Library.Api.Books;

public sealed class CreateBookRequest
{
    public string? Title { get; init; }

    public string? ShortDescription { get; init; }

    public DateOnly? PublishDate { get; init; }

    public IReadOnlyCollection<string?>? Authors { get; init; }
}
