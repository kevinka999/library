namespace Library.Domain.Books;

public sealed class Book
{
    public const int MaxTitleLength = 300;
    public const int MaxShortDescriptionLength = 1_000;
    public const int MaxAuthorNameLength = 200;

    private string[] _authors = [];

    public long Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string ShortDescription { get; private set; } = string.Empty;

    public DateOnly PublishDate { get; private set; }

    public IReadOnlyList<string> Authors => _authors;

    public long Version { get; private set; }

    public Book(
        string? title,
        string? shortDescription,
        DateOnly? publishDate,
        IEnumerable<string?>? authors)
    {
        var errors = new Dictionary<string, List<string>>();
        var normalizedTitle = NormalizeRequiredText(title, "title", "Title", MaxTitleLength, errors);
        var normalizedDescription = NormalizeRequiredText(
            shortDescription,
            "shortDescription",
            "Short description",
            MaxShortDescriptionLength,
            errors);

        if (publishDate is null)
        {
            AddError(errors, "publishDate", "Publish date is required.");
        }

        var normalizedAuthors = NormalizeAuthors(authors, errors);

        if (errors.Count > 0)
        {
            throw new BookValidationException(errors.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.ToArray()));
        }

        Title = normalizedTitle!;
        ShortDescription = normalizedDescription!;
        PublishDate = publishDate!.Value;
        _authors = normalizedAuthors!;
        Version = 1;
    }

    internal Book(
        long id,
        string title,
        string shortDescription,
        DateOnly publishDate,
        IEnumerable<string> authors,
        long version)
        : this(title, shortDescription, publishDate, authors)
    {
        Id = id;
        Version = version;
    }

    internal void AssignId(long id)
    {
        Id = id;
    }

    private static string? NormalizeRequiredText(
        string? value,
        string field,
        string displayName,
        int maximumLength,
        IDictionary<string, List<string>> errors)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            AddError(errors, field, $"{displayName} is required.");
            return null;
        }

        if (normalized.Length > maximumLength)
        {
            AddError(
                errors,
                field,
                $"{displayName} must be {maximumLength} characters or fewer.");
        }

        return normalized;
    }

    private static string[]? NormalizeAuthors(
        IEnumerable<string?>? authors,
        IDictionary<string, List<string>> errors)
    {
        if (authors is null)
        {
            AddError(errors, "authors", "At least one author name is required.");
            return null;
        }

        var suppliedAuthors = authors.ToArray();
        if (suppliedAuthors.Length == 0)
        {
            AddError(errors, "authors", "At least one author name is required.");
            return null;
        }

        var normalized = new List<string>(suppliedAuthors.Length);
        for (var index = 0; index < suppliedAuthors.Length; index++)
        {
            var name = suppliedAuthors[index]?.Trim();
            var field = $"authors[{index}]";

            if (string.IsNullOrWhiteSpace(name))
            {
                AddError(errors, field, "Author name must not be blank.");
                continue;
            }

            if (name.Length > MaxAuthorNameLength)
            {
                AddError(
                    errors,
                    field,
                    $"Author name must be {MaxAuthorNameLength} characters or fewer.");
            }

            normalized.Add(name);
        }

        foreach (var duplicateGroup in normalized
                     .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            AddError(
                errors,
                "authors",
                $"Author names must be unique; '{duplicateGroup.First()}' is duplicated.");
        }

        return normalized
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static void AddError(
        IDictionary<string, List<string>> errors,
        string field,
        string message)
    {
        if (!errors.TryGetValue(field, out var messages))
        {
            messages = [];
            errors[field] = messages;
        }

        messages.Add(message);
    }
}
