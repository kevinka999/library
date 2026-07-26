# Initial Implementation Specification

## Purpose

This document combines product requirements and implementation constraints for the
initial Library API. It is the source for an implementation plan, not the
authoritative definition of domain language or architectural decisions.

The following documents remain authoritative:

- [`../../CONTEXT.md`](../../CONTEXT.md) defines the domain vocabulary.
- [`../domain/BOOKS.md`](../domain/BOOKS.md) defines Book behavior and history semantics.
- [`../ARCHITECTURE.md`](../ARCHITECTURE.md) defines component boundaries and responsibilities.
- [`../adr/`](../adr/) records durable architecture and persistence decisions.
- The generated OpenAPI document describes the implemented HTTP contract.

If this specification requires a change to one of those documents, update the
authoritative document as part of the same change.

## Problem

A frontend needs a web API that can create, update, retrieve, and search Books
while presenting a trustworthy Book History. Consumers need current Book
information and a chronological record of what changed, with pagination,
filtering, and ordering suitable for a conventional book list and an
infinite-scrolling history timeline.

The repository currently contains only the minimal .NET 10 controller template.
It has the sample weather endpoint and basic OpenAPI document generation, but no
library domain, persistence, database container, Swagger UI, concurrency
protection, or tests.

## Goals

- Provide one public .NET 10 controller API for the Book lifecycle that is in scope.
- Store current Book state and immutable field-level Book Changes in PostgreSQL.
- Present all Book Changes produced by one creation or update as one complete
  Change Set.
- Give the frontend structured current-state and history data while leaving
  human-readable descriptions, localization, and presentation to the frontend.
- Make the service straightforward to run, inspect, and verify in local development.

## Users and workflows

### Frontend user

A frontend user can:

- Add a Book and navigate to it through a stable identifier.
- View the complete current Book representation.
- Replace the editable Book representation without silently overwriting a newer
  update.
- Search and page through the Book collection.
- Browse and filter a Book's history in either chronological direction.

### Frontend developer

A frontend developer can:

- Consume consistent success and error representations.
- Use ETag and `If-Match` from a browser for concurrency-safe updates.
- Access the unauthenticated company-test API without origin coordination.
- Generate localized change descriptions from structured old and new values.

### Maintainer and operator

A maintainer or operator can:

- Run the external database dependency locally through Docker Compose.
- Inspect and exercise the API through development-only Swagger UI.
- Test domain and application behavior independently of HTTP and EF Core.
- Apply production migrations explicitly rather than from competing API instances.

## Functional requirements

### Book creation and retrieval

- **BOOK-001:** `POST /api/books` creates a Book that conforms to the rules in
  [`../domain/BOOKS.md`](../domain/BOOKS.md).
- **BOOK-002:** A created Book receives a stable database-generated identifier and
  starts at version 1.
- **BOOK-003:** Creation records one initial Book Change per populated Book field.
  Those changes have null old values and belong to one Change Set.
- **BOOK-004:** Successful creation returns `201 Created`, a `Location` header, the
  complete created Book, and its ETag.
- **BOOK-005:** `GET /api/books/{id}` returns the current Book representation and
  its ETag.
- **BOOK-006:** Retrieving a missing Book returns `404 Not Found`.

### Book replacement and concurrency

- **BOOK-007:** `PUT /api/books/{id}` replaces all editable Book fields.
- **BOOK-008:** Replacement requires `If-Match`. A missing precondition returns
  `428 Precondition Required`.
- **BOOK-009:** A stale precondition returns `412 Precondition Failed` without
  changing the Book, its version, or its history.
- **BOOK-010:** The current precondition is checked before an update is classified
  as a no-op.
- **BOOK-011:** An effective update increments the Book version exactly once and
  records one Book Change for each changed field. Every change produced by that
  update belongs to the same Change Set.
- **BOOK-012:** An identical update with the current ETag returns `200 OK`, does
  not increment the version, preserves the ETag, and creates no Change Set.
- **BOOK-013:** A successful replacement returns the complete current Book and its
  ETag.
- **BOOK-014:** Book state and the Change Set produced by an update are committed
  atomically.

### Book collection

- **SEARCH-001:** `GET /api/books` accepts `search`, `page`, and `pageSize`.
- **SEARCH-002:** `search` is optional and trimmed. It performs
  case-insensitive contains matching across Title, Short Description, and every
  Author Name.
- **SEARCH-003:** Pagination is one-based. `pageSize` defaults to 20 and cannot
  exceed 100.
- **SEARCH-004:** The response contains `items`, `page`, `pageSize`,
  `totalCount`, and `totalPages`.
- **SEARCH-005:** Invalid search or paging input returns `400 Bad Request`.
- **SEARCH-006:** Results are ordered deterministically by Title ascending and
  then Book ID ascending.
- **SEARCH-007:** The endpoint exposes no additional filters or client-controlled
  ordering.

### Book History

- **HISTORY-001:** `GET /api/books/{id}/history` returns Book History separately
  from current Book state.
- **HISTORY-002:** The endpoint verifies that the Book exists. A missing Book
  returns `404 Not Found`, rather than an empty history.
- **HISTORY-003:** Each history item represents one complete Change Set and
  contains its Change Set ID and UTC Changed At value once.
- **HISTORY-004:** Each history item's nested `changes` collection exposes the
  stable Book Change ID, Changed Field, Old Value, and New Value.
- **HISTORY-005:** Old and new values retain their natural JSON shapes, including
  strings, date strings, nulls, and Author Name arrays.
- **HISTORY-006:** History uses cursor pagination over Change Sets. `limit`
  defaults to 20 Change Sets and cannot exceed 100.
- **HISTORY-007:** The response contains `items`, `nextCursor`, and `hasMore`.
- **HISTORY-008:** History is ordered by Changed At and then Change Set ID as a
  deterministic tie-breaker. The default direction is descending.
- **HISTORY-009:** `sortDirection` accepts ascending or descending chronological
  order. The endpoint does not support other ordering.
- **HISTORY-010:** Repeated `changedField` filters use any-match semantics.
- **HISTORY-011:** When a Changed Field filter matches any Book Change, the
  response includes the complete containing Change Set.
- **HISTORY-012:** `changedFrom` is an inclusive UTC instant and
  `changedBefore` is an exclusive UTC instant.
- **HISTORY-013:** Cursors are opaque, identify the last Change Set by Changed At
  and Change Set ID, and are bound to the normalized filters and direction used
  to create them.
- **HISTORY-014:** An invalid cursor or a cursor incompatible with the current
  query returns `400 Bad Request`.

### Errors, browser access, and API discovery

- **API-001:** Expected API failures use `application/problem+json`.
- **API-002:** Invalid input, query parameters, or cursors return `400`; a missing
  Book returns `404`; a stale precondition returns `412`; and a missing
  precondition returns `428`.
- **API-003:** Unexpected exceptions are handled centrally, return `500`, and do
  not expose internal details.
- **API-004:** During this unauthenticated company test, CORS permits all origins,
  methods, and headers without credentials and exposes `ETag`.
- **API-005:** OpenAPI and Swagger UI are available only in Development. Swagger
  UI is hosted at `/swagger`, and the document is hosted at
  `/openapi/v1.json`.
- **API-006:** OpenAPI documents request bodies, response bodies, query
  parameters, success and Problem Details responses, `ETag`, and `If-Match`.

### Local development and operation

- **OPS-001:** Docker Compose starts PostgreSQL with a persistent named volume and
  is used only for external dependencies.
- **OPS-002:** The API runs directly with `dotnet run`; an API Dockerfile is not
  part of this implementation.
- **OPS-003:** Database credentials and connection strings are not committed.
  Safe local configuration examples are provided.
- **OPS-004:** Pending migrations are applied automatically only in Development.
  Production migrations remain an explicit deployment action.
- **OPS-005:** The generated Weather Forecast endpoint and model are removed.

## Technical constraints and decisions

The implementation follows the domain and architecture documents linked under
[Purpose](#purpose). This section records feature-specific constraints needed by
the implementation plan; it does not redefine those documents.

### Application structure

- Target .NET 10 and retain the controller-based ASP.NET Core API style.
- Implement the Domain, Application, Infrastructure, and API project boundaries
  from [`../ARCHITECTURE.md`](../ARCHITECTURE.md), plus one unit-test project.
- Organize the Application use cases under Handlers as Create Book, Update Book, Get
  Book, Search Books, and Get Book History.
- Keep focused persistence, history-reading, and unit-of-work contracts in
  `Application/Abstractions`; do not introduce a generic CRUD repository.
- Stage Books and Book Changes through separate repositories. Commit all staged
  changes once through the Unit of Work so the use case owns coordination and
  the repositories contain no lifecycle-history policy.
- Every concrete repository owns private `ToPersistence` and `ToDomain`
  mappings. Every persisted Domain entity has a corresponding data-only
  Infrastructure record, including when their fields currently coincide.
- Inject concrete Application handlers into controllers without a mediator library.
- Represent expected Application outcomes with a small explicit Result type.
- Reuse an Application `BookResult` across use cases that return the same complete
  Book representation; do not create operation-specific result models with identical
  fields.
- Use Domain constructors and methods rather than FluentValidation. Domain
  objects guard their own invariants and may report invalid construction through
  a validation exception containing all detected field errors; Application
  handlers translate that exception into an explicit failure result.

### Persistence

- Implement Application abstractions with EF Core 10 and the Npgsql provider.
- Use PostgreSQL 18 as decided by
  [`../adr/0003-use-postgresql-and-native-author-arrays.md`](../adr/0003-use-postgresql-and-native-author-arrays.md).
- Use exactly two domain tables: `Books` and `BookChanges`.
- Store Author Names as `text[]` and Book Change old and new values as `jsonb`.
- Use generated `bigint` identity values for Book and Book Change IDs, an
  incrementing `bigint` Book Version, and a UUID Change Set ID.
- Infrastructure maps Domain Book Fields to stable string discriminators for
  persistence.
- Add foreign-key and uniqueness constraints, including at most one Book Change
  for each Changed Field in a Change Set.
- Add indexes that support history lookup, filtering, and cursor ordering by Book
  ID, Changed At, Change Set ID, and Changed Field.
- Generate and commit EF Core migrations.
- Keep Book Changes append-only and permanently retained.
- Keep EF record types, configurations, repositories, and migrations in focused
  Infrastructure persistence folders.
- After a successful Unit of Work commit, synchronize database-generated Book
  IDs from tracked Book records back to their corresponding Domain objects.

### Domain and presentation boundaries

- The public `Book` constructor owns validation and normalization of the initial
  current state. Invalid construction throws one validation exception containing
  all detected field errors.
- `Book` does not create or own Book Changes.
- The Create Book Application handler produces the initial four-field Change Set
  and coordinates its atomic persistence with the Book.
- `Book.Update` owns validation, normalized comparison, mutation, version
  behavior, and production of transient structured field changes.
- Book History is not a navigation collection on the Book aggregate.
- The frontend owns history descriptions, formatting, grouping presentation, and
  localization. The API does not generate English change descriptions.

## Verification criteria

### Automated tests

- **TEST-001:** Use xUnit and hand-written fakes; do not add a mocking framework.
- **TEST-002:** Treat Application use-case handlers as the primary unit-testing
  seam and exercise real Domain behavior through them.
- **TEST-003:** Assert observable outcomes such as results, persisted Book state,
  emitted Book Changes, version behavior, transaction intent, and calls through
  Application abstractions. Do not assert private methods, EF implementation
  details, or folder structure.
- **TEST-004:** Cover Create Book success and validation failures, including one
  initial Book Change per field in one Change Set.
- **TEST-005:** Cover Update Book field-by-field and multi-field changes, author
  normalization and order independence, casing corrections, and no-op behavior.
- **TEST-006:** Cover missing and stale update preconditions without persistence
  or history mutation.
- **TEST-007:** Cover Get Book success and not-found behavior.
- **TEST-008:** Cover Search Books validation and propagation of search and paging
  criteria through its read abstraction.
- **TEST-009:** Cover Get Book History existence checks, filters, cursor
  validation, complete Change Set projection, and result propagation.
- **TEST-010:** Cover expected-result-to-HTTP mapping where it is pure and does
  not require an ASP.NET host.

### Manual delivery verification

Integration tests, end-to-end tests, Testcontainers, and performance tests are
outside the current scope. Before delivery, manually:

1. Start PostgreSQL through Docker Compose.
2. Confirm Development migrations apply successfully.
3. Exercise all five endpoints through Swagger UI.
4. Confirm the documented ETag and `If-Match` behavior, including no-op and stale
   updates.
5. Verify search, both pagination models, every filter, and both history
   directions.
6. Inspect stored Books and Book Changes to confirm array and JSONB mappings,
   indexes, constraints, atomic writes, immutable history, and correct Change Set
   grouping.

## Out of scope

- Authentication, authorization, users, roles, and Changed By information.
- Security hardening beyond safe secret handling and standard error responses.
- Deleting Books or Book History.
- Editing, deleting, or truncating Book Changes.
- Author entities, profiles, CRUD, cross-Book identity deduplication, or co-author
  roles.
- Event sourcing or reconstructing Books by replaying history.
- `PATCH` or JSON Patch updates.
- Backend-generated natural-language change descriptions.
- User-selectable history grouping beyond complete Change Sets.
- Ordering Book History by Changed Field or old and new values.
- Additional Book filters or client-controlled Book ordering.
- Cursor pagination for Books or numbered-page pagination for Book History.
- Filtering or searching history by old or new JSON values.
- Full-text search infrastructure or a separate search engine.
- Redis, messaging, caching, or other external dependencies.
- API containerization and production deployment configuration.
- Automatic production migrations.
- Integration, end-to-end, and performance tests.
- MediatR, FluentValidation, generic repository frameworks, or a mocking framework.

## Unresolved decisions

These decisions must be resolved explicitly in the implementation plan or before
the affected implementation task begins:

1. **ETag wire format:** Define the exact strong ETag representation, how a Book
   Version maps to it, and whether wildcard or multiple `If-Match` values are
   accepted.
2. **HTTP schemas:** Define concrete request, success-response, and Problem Details
   schemas, including JSON property naming and any stable application error
   codes or extensions.
3. **Unknown query parameters:** Decide whether endpoints reject parameters
   outside their documented query contract or ignore them.
4. **Cursor representation:** Define cursor encoding and versioning, normalized
   filter serialization, and whether tamper detection is required for this
   company test.
5. **Database concurrency enforcement:** Define how EF Core and PostgreSQL enforce
   the version precondition atomically and how a concurrent write is translated
   into `412 Precondition Failed`.
6. **Database test coverage:** Confirm that manual verification is sufficient for
   PostgreSQL-specific mappings, queries, constraints, transactions, and
   concurrency, or bring narrowly scoped automated integration tests into scope.

## Planning and traceability

The implementation plan should:

- Reference the requirement IDs implemented by each task.
- Begin by resolving any Unresolved Decisions required by later tasks.
- Keep changes small enough to build and verify independently.
- Include documentation updates when implementation changes an authoritative
  domain, architecture, ADR, operational, or HTTP-contract document.
- End with the automated and manual verification described above.
