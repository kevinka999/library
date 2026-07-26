# Library API

A .NET 10 controller-based web API for managing books and exposing their immutable change history.

> The API is currently the original project template. The Book API, PostgreSQL persistence, Docker Compose setup, Swagger UI, and tests are specified in [GitHub issue #1](https://github.com/kevinka999/library/issues/1) and are not all implemented yet.

## Documentation

- [`CONTEXT.md`](CONTEXT.md) defines the domain language.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) describes the target component boundaries and runtime flows.
- [`docs/domain/BOOKS.md`](docs/domain/BOOKS.md) describes Book behavior, invariants, and change-history semantics.
- [`docs/adr/`](docs/adr/) records durable architectural decisions.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker with Docker Compose, once PostgreSQL support is implemented

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

## Run the current template

Start the HTTP development profile:

```sh
dotnet run --launch-profile http
```

The current launch profile listens at:

- `http://localhost:5168`
- OpenAPI document: `http://localhost:5168/openapi/v1.json`

HTTPS is also available through:

```sh
dotnet run --launch-profile https
```

The HTTPS profile listens at `https://localhost:7119` and may require trusting the local development certificate:

```sh
dotnet dev-certs https --trust
```

Swagger UI will be available only in the Development environment after its package and middleware are added. Update this README with its final URL when implemented.

## Database

The target implementation uses PostgreSQL 18 through Npgsql. Docker Compose will run only external dependencies; the API continues to run locally with `dotnet run`.

Once the Compose file is implemented, the expected lifecycle is:

```sh
docker compose up -d
docker compose down
```

Do not use these commands until a Compose file exists. Database connection settings must come from application configuration or environment variables; do not commit credentials.

## Migrations

EF Core migrations are not configured in the template yet. Once the Infrastructure project and EF tooling are present, document the exact project-specific commands here.

The intended operational policy is:

- Development may apply pending migrations automatically at startup.
- Production migrations are an explicit deployment step and must not run automatically when the API starts.

## Tests

Run all configured tests with:

```sh
dotnet test
```

The target test suite uses xUnit and focuses on Domain behavior and Application use-case handlers with hand-written fakes. The project intentionally does not require integration tests.

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
