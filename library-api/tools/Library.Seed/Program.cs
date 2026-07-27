using System.Net.Http.Json;
using System.Text.Json;

namespace Library.Seed;

internal static class Program
{
    private const int ExpectedBookCount = 15;
    private const int MinimumChangeSetCount = 8;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<int> Main(string[] args)
    {
        SeedOptions options;
        try
        {
            options = SeedOptions.Parse(args);
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            Console.Error.WriteLine();
            PrintUsage();
            return 2;
        }

        if (options.ShowHelp)
        {
            PrintUsage();
            return 0;
        }

        try
        {
            var catalog = await LoadCatalogAsync();
            ValidateCatalog(catalog);

            using var client = new HttpClient
            {
                BaseAddress = new Uri($"{options.BaseUrl.TrimEnd('/')}/"),
                Timeout = TimeSpan.FromSeconds(30)
            };

            var existingBookCount = await GetBookCountAsync(client);
            if (existingBookCount > 0 && !options.AllowNonEmpty)
            {
                Console.Error.WriteLine(
                    $"Seed cancelled: the API already contains {existingBookCount} Book(s).");
                Console.Error.WriteLine(
                    "Use an empty development database or pass --allow-non-empty if duplicates are acceptable.");
                return 1;
            }

            Console.WriteLine(
                $"Seeding {catalog.Books.Count} Books through {client.BaseAddress}...");

            var seededBooks = new List<BookResponse>(catalog.Books.Count);
            foreach (var seedBook in catalog.Books)
            {
                var current = seedBook.Initial;
                var created = await SendAsync<BookResponse>(
                    client,
                    HttpMethod.Post,
                    "api/books",
                    current);

                foreach (var change in seedBook.Changes)
                {
                    current = change.ApplyTo(current);
                    created = await SendAsync<BookResponse>(
                        client,
                        HttpMethod.Put,
                        $"api/books/{created.Id}",
                        current,
                        created.Version);
                }

                var history = await GetAsync<HistoryResponse>(
                    client,
                    $"api/books/{created.Id}/history?sortDirection=ascending&limit=100");
                if (history.Items.Count < MinimumChangeSetCount)
                {
                    throw new InvalidOperationException(
                        $"Book {created.Id} has {history.Items.Count} Change Sets; expected at least {MinimumChangeSetCount}.");
                }

                seededBooks.Add(created);
                Console.WriteLine(
                    $"  [{seededBooks.Count,2}/{catalog.Books.Count}] Book {created.Id,3}: "
                    + $"{created.Title} ({history.Items.Count} Change Sets, version {created.Version})");
            }

            Console.WriteLine();
            Console.WriteLine(
                $"Seed complete: {seededBooks.Count} Books and at least "
                + $"{seededBooks.Count * MinimumChangeSetCount} Change Sets created.");
            Console.WriteLine(
                "Try GET /api/books?page=1&pageSize=5 and "
                + $"GET /api/books/{seededBooks[0].Id}/history?limit=3.");
            return 0;
        }
        catch (HttpRequestException exception)
        {
            Console.Error.WriteLine($"Seed failed while contacting the API: {exception.Message}");
            return 1;
        }
        catch (Exception exception) when (
            exception is IOException
            or JsonException
            or InvalidOperationException)
        {
            Console.Error.WriteLine($"Seed failed: {exception.Message}");
            return 1;
        }
    }

    private static async Task<SeedCatalog> LoadCatalogAsync()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "books.json");
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<SeedCatalog>(stream, JsonOptions)
            ?? throw new InvalidOperationException("The seed catalog is empty.");
    }

    private static void ValidateCatalog(SeedCatalog catalog)
    {
        if (catalog.Books.Count != ExpectedBookCount)
        {
            throw new InvalidOperationException(
                $"The seed catalog must contain exactly {ExpectedBookCount} Books.");
        }

        var duplicateTitles = catalog.Books
            .Select(book => book.Changes.Aggregate(
                book.Initial,
                (state, change) => change.ApplyTo(state)).Title)
            .GroupBy(title => title, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateTitles.Length > 0)
        {
            throw new InvalidOperationException(
                $"Final Book titles must be unique: {string.Join(", ", duplicateTitles)}.");
        }

        foreach (var book in catalog.Books)
        {
            if (book.Changes.Count + 1 < MinimumChangeSetCount)
            {
                throw new InvalidOperationException(
                    $"'{book.Initial.Title}' must define at least "
                    + $"{MinimumChangeSetCount - 1} effective updates.");
            }

            var state = book.Initial;
            foreach (var change in book.Changes)
            {
                var next = change.ApplyTo(state);
                if (next == state)
                {
                    throw new InvalidOperationException(
                        $"'{book.Initial.Title}' contains an update that changes no field.");
                }

                state = next;
            }
        }
    }

    private static async Task<long> GetBookCountAsync(HttpClient client)
    {
        var result = await GetAsync<SearchResponse>(
            client,
            "api/books?page=1&pageSize=1");
        return result.TotalCount;
    }

    private static async Task<T> GetAsync<T>(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        return await ReadResponseAsync<T>(response);
    }

    private static async Task<T> SendAsync<T>(
        HttpClient client,
        HttpMethod method,
        string path,
        BookState state,
        long? version = null)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(state, options: JsonOptions)
        };
        if (version is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        }

        using var response = await client.SendAsync(request);
        return await ReadResponseAsync<T>(response);
    }

    private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"HTTP {(int)response.StatusCode} ({response.ReasonPhrase}): {body}");
        }

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions)
            ?? throw new InvalidOperationException(
                $"The API returned an empty {typeof(T).Name} response.");
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            """
            Seeds the running Library API with coherent development data.

            Usage:
              dotnet run --project tools/Library.Seed
              dotnet run --project tools/Library.Seed -- --base-url http://localhost:5168
              dotnet run --project tools/Library.Seed -- --allow-non-empty

            Options:
              --base-url <url>    API base URL (default: http://localhost:5168)
              --allow-non-empty   Seed even when Books already exist; may create duplicates
              --help              Show this help
            """);
    }
}

internal sealed record SeedOptions(
    string BaseUrl,
    bool AllowNonEmpty,
    bool ShowHelp)
{
    public static SeedOptions Parse(string[] args)
    {
        var baseUrl = "http://localhost:5168";
        var allowNonEmpty = false;
        var showHelp = false;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--base-url":
                    if (++index >= args.Length)
                    {
                        throw new ArgumentException("--base-url requires a URL.");
                    }

                    baseUrl = args[index];
                    if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
                        || (uri.Scheme != Uri.UriSchemeHttp
                            && uri.Scheme != Uri.UriSchemeHttps))
                    {
                        throw new ArgumentException("--base-url must be an absolute HTTP(S) URL.");
                    }

                    break;
                case "--allow-non-empty":
                    allowNonEmpty = true;
                    break;
                case "--help":
                case "-h":
                    showHelp = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {args[index]}");
            }
        }

        return new SeedOptions(baseUrl, allowNonEmpty, showHelp);
    }
}

internal sealed record SeedCatalog(IReadOnlyList<SeedBook> Books);

internal sealed record SeedBook(
    BookState Initial,
    IReadOnlyList<BookPatch> Changes);

internal sealed record BookState(
    string Title,
    string ShortDescription,
    DateOnly PublishDate,
    IReadOnlyList<string> Authors);

internal sealed record BookPatch(
    string? Title,
    string? ShortDescription,
    DateOnly? PublishDate,
    IReadOnlyList<string>? Authors)
{
    public BookState ApplyTo(BookState current) =>
        new(
            Title ?? current.Title,
            ShortDescription ?? current.ShortDescription,
            PublishDate ?? current.PublishDate,
            Authors ?? current.Authors);
}

internal sealed record BookResponse(
    long Id,
    string Title,
    string ShortDescription,
    DateOnly PublishDate,
    IReadOnlyList<string> Authors,
    long Version);

internal sealed record SearchResponse(long TotalCount);

internal sealed record HistoryResponse(IReadOnlyList<HistoryItem> Items);

internal sealed record HistoryItem(Guid ChangeSetId);
