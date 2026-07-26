# Library API Architecture

> This is the target architecture; the projects and components are introduced as the API is implemented.

This is a system map, not a complete feature specification:

- [`../CONTEXT.md`](../CONTEXT.md) defines the domain language.
- [`domain/BOOKS.md`](domain/BOOKS.md) describes Book behavior and history semantics.
- [`adr/`](adr/) records why durable architectural and persistence decisions were made.
- Tracked issues contain feature-specific requirements and acceptance criteria.
- OpenAPI describes the implemented HTTP contract.

## Architectural style

The backend uses Clean Architecture with four project boundaries:

- Domain has no dependency on Application, Infrastructure, ASP.NET Core, or EF Core.
- Application depends on Domain and owns the abstractions required by its use cases.
- Infrastructure depends on Application and Domain to implement those abstractions.
- API depends on Application and Infrastructure and acts as the composition root.

The API-to-Infrastructure reference exists for dependency-injection composition. Controllers communicate with Application use cases, not directly with EF Core.

## Project responsibilities

### Domain

Domain owns `Book` behavior and invariants. `BookChange` represents an immutable historical fact, but history querying and pagination remain outside the Book aggregate.

Domain code contains no HTTP, database, request-model, or serialization concerns. Detailed rules are documented in [`domain/BOOKS.md`](domain/BOOKS.md).

### Application

Application organizes behavior vertically by use case:

- Create Book
- Update Book
- Get Book
- Find Books
- Get Book History

Each feature keeps its input, handler, result model, and feature-specific validation together. History is a separate feature: clients load a Book and request its history independently.

`Application/Abstractions` contains the small set of outbound contracts required by the use cases. These contracts express application needs and must not expose EF Core types or become generic CRUD repositories.

Application handlers coordinate Domain behavior, persistence, transactions, and explicit success or error results.

### Infrastructure

Infrastructure implements Application abstractions using EF Core and PostgreSQL. It owns:

- The EF Core database context and mappings.
- Repository and query implementations.
- Database migrations.
- Transaction and optimistic-concurrency persistence behavior.

Technology choices and storage-specific decisions belong in the relevant ADRs rather than this document.

### API

API owns:

- Controllers and HTTP contracts.
- Dependency-injection composition.
- Problem Details mapping.
- ETag and `If-Match` translation.
- CORS configuration.
- Development-only OpenAPI and Swagger configuration.

Controllers translate between HTTP and Application models. They contain no domain decisions or direct persistence queries.

## Runtime flows

### Commands

For creation and update:

1. API translates the HTTP request into an Application command.
2. Application loads or creates the Domain model and invokes its behavior.
3. Application asks Infrastructure to persist the Book and its resulting history atomically.
4. API translates the Application result into an HTTP response.

A stale update writes neither the Book nor its history.

### Queries

For Book details, search, and history:

1. API translates route and query parameters into an Application query.
2. Application requests the required data through an abstraction it owns.
3. Infrastructure executes and projects the query efficiently.
4. API returns the Application result without creating presentation text for historical changes.

Read-specific projections are allowed when they preserve an Application-owned contract and avoid loading unnecessary Domain state.

## Cross-cutting rules

- Known API failures use Problem Details.
- Optimistic concurrency is exposed through ETag and `If-Match`.
- OpenAPI and Swagger UI are enabled only in Development.
- CORS is intentionally open for this test application, without credentials, and exposes `ETag`.
- Database migrations may run automatically in Development but are an explicit deployment step in Production.

## Testing seams

Unit tests focus on Domain behavior and Application use-case handlers. Application abstractions are replaced with hand-written fakes.

The project intentionally does not require integration tests or Testcontainers. Technology-specific mappings, migrations, queries, and concurrency behavior must therefore be verified manually against the Dockerized development database.

## Keeping documentation current

- Change `CONTEXT.md` when domain vocabulary changes.
- Change `domain/BOOKS.md` when Book behavior or history semantics change.
- Add or supersede an ADR when a durable decision changes.
- Change this document when boundaries, dependency direction, responsibilities, or system flows change.
- Change the API README when build, run, migration, test, or local-environment commands change.
- Keep `AGENTS.md` limited to instructions relevant to nearly every agent task.
