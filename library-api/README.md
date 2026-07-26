# Library API

A .NET 10 controller-based web API for managing books and exposing their immutable change history.

The runnable Clean Architecture foundation and Book creation vertical slice are
implemented. `POST /api/books` stores the normalized Book and its complete
initial Change Set atomically in PostgreSQL.

## Documentation

- [`CONTEXT.md`](CONTEXT.md) defines the domain language.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) describes the target component boundaries and runtime flows.
- [`docs/domain/BOOKS.md`](docs/domain/BOOKS.md) describes Book behavior, invariants, and change-history semantics.
- [`docs/adr/`](docs/adr/) records durable architectural decisions.

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

First configure the database connection with .NET user secrets. Use the same
password that you place in `.env`:

```sh
dotnet user-secrets init --project Library.Api
dotnet user-secrets set --project Library.Api \
  "ConnectionStrings:LibraryDatabase" \
  "Host=localhost;Port=5432;Database=library;Username=library;Password=choose-a-local-password"
```

Alternatively, set `ConnectionStrings__LibraryDatabase` in the shell. Start the
HTTP development profile:

```sh
dotnet run --launch-profile http
```

The current launch profile listens at:

- `http://localhost:5168`
- OpenAPI document: `http://localhost:5168/openapi/v1.json`
- Swagger UI: `http://localhost:5168/swagger`

HTTPS is also available through:

```sh
dotnet run --launch-profile https
```

The HTTPS profile listens at `https://localhost:7119` and may require trusting the local development certificate:

```sh
dotnet dev-certs https --trust
```

OpenAPI and Swagger UI are available only in the Development environment.

## Database

PostgreSQL 18 runs as the only Compose service; the API continues to run locally
with `dotnet run`. Create the ignored local environment file and start the
database:

```sh
cp .env.example .env
# Edit .env and choose a local-only password.
docker compose up -d
docker compose ps
docker compose down
```

The named `library-postgres-data` volume preserves data across container
restarts and `docker compose down`. Run `docker compose down --volumes` only
when you intentionally want to delete the development database.

Database credentials and real connection strings must remain in the ignored
`.env`, user secrets, or environment variables. `.env.example` contains only a
safe placeholder.

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
behavior and Book creation invariants in the Domain, while initial-history and
persistence behavior are exercised through the Application handler. The project
intentionally does not require automated integration tests.

## Target API

The planned public surface is:

```text
POST /api/books
PUT  /api/books/{id}
GET  /api/books/{id}
GET  /api/books?search=&page=&pageSize=
GET  /api/books/{id}/history?changedField=&changedFrom=&sortDirection=&cursor=&pageSize=
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
