# Library API

A .NET 10 controller-based web API for managing books and exposing their immutable change history.

The initial Book API is implemented and delivery-verified. `POST /api/books`
stores the normalized Book and its complete initial Change Set atomically in
PostgreSQL. `GET /api/books/{id}` returns its current state and version-backed
ETag. `PUT /api/books/{id}` safely replaces it using `If-Match`, appending one
complete Change Set only when the normalized state changes. `GET /api/books`
returns deterministic numbered pages and optionally searches current Book
fields. `GET /api/books/{id}/history` cursor-pages through complete Change Sets
with field, time, and direction filters.

## Documentation

- [`CONTEXT.md`](CONTEXT.md) defines the domain language.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) describes the target component boundaries and runtime flows.
- [`docs/domain/BOOKS.md`](docs/domain/BOOKS.md) describes Book behavior, invariants, and change-history semantics.
- [`docs/adr/`](docs/adr/) records durable architectural decisions.
- [`docs/delivery/01-initial-implementation-verification.md`](docs/delivery/01-initial-implementation-verification.md) records the initial delivery verification.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker with Docker Compose

Check the installed SDK:

```sh
dotnet --version
```

## Restore and build

Run commands from the `library-api` directory:

```sh
dotnet restore
dotnet build
```

## Run the API

Docker runs PostgreSQL only; the API runs with `dotnet run`.

Create `.env`, choose a local password, and use that same password in the API
connection string:

```sh
cp .env.example .env
# Set LIBRARY_DB_PASSWORD=pw123 in .env.

dotnet user-secrets init --project Library.Api
dotnet user-secrets set --project Library.Api \
  "ConnectionStrings:LibraryDatabase" \
  "Host=localhost;Port=5432;Database=library;Username=library;Password=pw123"

docker compose up -d --wait
dotnet run --project Library.Api --launch-profile http
```

Swagger is available at `http://localhost:5168/swagger` and OpenAPI at
`http://localhost:5168/openapi/v1.json`. Development migrations are applied
automatically.

If the database already exists and you need to change its password, update both
PostgreSQL and the API secret, then put the new value in
`LIBRARY_DB_PASSWORD` inside `.env`:

```sh
docker compose exec postgres \
  psql -U library -d library \
  -c "ALTER USER library WITH PASSWORD 'new-password';"
dotnet user-secrets set --project Library.Api \
  "ConnectionStrings:LibraryDatabase" \
  "Host=localhost;Port=5432;Database=library;Username=library;Password=new-password"
```

Use `docker compose down` to stop PostgreSQL. Add `--volumes` only when you also
want to delete all local database data.

## Development seed data

With the API running, populate an empty development database through the public
Book endpoints:

```sh
dotnet run --project tools/Library.Seed
```

The seed creates 15 fictional Books for numbered paging and search scenarios.
Each Book is created and then updated seven times, producing eight coherent,
chronologically ordered Change Sets per Book (at least 120 Change Sets in
total). Because the tool uses `POST /api/books` and `PUT /api/books/{id}`, it
exercises the same validation, immutable history, and optimistic concurrency
behavior as a client.

The tool targets `http://localhost:5168` by default and refuses to run when any
Books already exist, which prevents accidental duplicate data. Use a different
API address with:

```sh
dotnet run --project tools/Library.Seed -- \
  --base-url http://localhost:5000
```

Passing `--allow-non-empty` overrides the safety check and may create duplicate
Books. Run `dotnet run --project tools/Library.Seed -- --help` for all options.

After seeding, useful paging examples include:

```text
GET /api/books?page=1&pageSize=5
GET /api/books/{id}/history?sortDirection=ascending&limit=3
```

## Migrations

The API automatically applies pending migrations at startup only when
`ASPNETCORE_ENVIRONMENT=Development`. Generate a migration from the repository
root with:

```sh
dotnet ef migrations add MigrationName \
  --project Library.Infrastructure \
  --startup-project Library.Api \
  --output-dir Persistence/Migrations
```

Install the matching `dotnet-ef` 10.0.x tool first if it is unavailable. For a
production deployment, apply migrations explicitly:

```sh
dotnet ef database update \
  --project Library.Infrastructure \
  --startup-project Library.Api \
  --connection "$LIBRARY_DATABASE_CONNECTION_STRING"
```

The intended operational policy is:

- Development may apply pending migrations automatically at startup.
- Production migrations are an explicit deployment step and must not run automatically when the API starts.

## Tests

Run all configured tests with:

```sh
dotnet test
```

The test suite uses xUnit and hand-written fakes. It covers cross-cutting error
behavior and Book creation and replacement invariants in the Domain, while
initial-history, update-history, no-op, optimistic-concurrency, and search
validation and paging behavior are exercised through the Application handlers.
The project intentionally does not require automated integration tests.

## API

The public surface is:

```text
POST /api/books
PUT  /api/books/{id}
GET  /api/books/{id}
GET  /api/books?search=&page=&pageSize=
GET  /api/books/{id}/history?changedField=&changedFrom=&changedBefore=&sortDirection=&cursor=&limit=
```

Updates use the current Book ETag through the `If-Match` header. The history endpoint returns complete Change Sets and structured values so the frontend can generate its own descriptions.

Create a Book:

```sh
curl --include http://localhost:5168/api/books \
  --header 'Content-Type: application/json' \
  --data '{
    "title": "The Left Hand of Darkness",
    "shortDescription": "A science fiction novel.",
    "publishDate": "1969-03-01",
    "authors": ["Ursula K. Le Guin"]
  }'
```

A successful response is `201 Created`, includes `Location: /api/books/{id}`
and `ETag: "1"`, and returns the complete normalized Book.

Retrieve the created Book using the path from the `Location` header:

```sh
curl --include http://localhost:5168/api/books/1
```

An existing Book returns `200 OK`, its complete current representation, and its
current ETag. An unknown ID returns `404 application/problem+json`.

Replace every editable field using the current ETag:

```sh
curl --include --request PUT http://localhost:5168/api/books/1 \
  --header 'Content-Type: application/json' \
  --header 'If-Match: "1"' \
  --data '{
    "title": "The Dispossessed",
    "shortDescription": "An ambiguous utopia.",
    "publishDate": "1974-05-01",
    "authors": ["Ursula K. Le Guin"]
  }'
```

An effective replacement returns `200 OK` with an advanced ETag and appends one
Book Change per changed field in a single Change Set. A normalized no-op keeps
the current version and creates no history. Missing, malformed, and stale
preconditions return `428`, `400`, and `412` respectively.

Search and page Books:

```sh
curl 'http://localhost:5168/api/books?search=guin&page=1&pageSize=20'
```

Search is trimmed and matched case-insensitively within titles, short
descriptions, and every Author Name. `%`, `_`, quotes, and backslashes are
treated as literal search text. Paging is one-based, defaults to 20 Books per
page, and permits at most 100. Results are always ordered by title and then Book
ID. Unsupported or repeated query parameters and invalid paging values return
`400 application/problem+json`.

Browse complete Change Sets, newest first:

```sh
curl 'http://localhost:5168/api/books/1/history?limit=20&sortDirection=descending'
```

Use the returned `nextCursor` as the `after` value for the next page. Cursors
are valid only with the same normalized filters and direction:

```sh
curl --get 'http://localhost:5168/api/books/1/history' \
  --data-urlencode 'changedField=title' \
  --data-urlencode 'changedField=authors' \
  --data-urlencode 'changedFrom=2020-01-01T00:00:00Z' \
  --data-urlencode 'sortDirection=ascending' \
  --data-urlencode 'limit=20' \
  --data-urlencode 'after=CURSOR_FROM_THE_PREVIOUS_RESPONSE'
```

`changedFrom` is inclusive, `changedBefore` is exclusive, and a Changed Field
match returns the entire containing Change Set. Invalid or incompatible cursors
return `400 application/problem+json`.
