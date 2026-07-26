# Initial Implementation Delivery Verification

## Result

Slice 7 was verified successfully on 2026-07-26 using .NET SDK 10.0.302,
Docker Engine 29.6.1, and the repository's PostgreSQL 18 Compose service.

The verification used an isolated Compose project and named volume. No
repository secret was created or printed. The isolated verification resources
were removed after the checks.

## Automated checks

After cleaning generated build output, the documented restore, build, and test
commands succeeded:

```text
dotnet restore: succeeded
dotnet build: succeeded with 0 warnings and 0 errors
dotnet test: 89 passed, 0 failed, 0 skipped
```

No tracked secret, build output, test result, or machine-specific IDE file was
found. Package references, the migration, runtime configuration, formatting,
and the branch diff were reviewed.

## Runtime and HTTP walkthrough

The Development API started directly with `dotnet run`, applied the initial
migration without manual schema steps, and served Swagger UI at `/swagger` and
OpenAPI at `/openapi/v1.json`. The rendered Swagger UI exposed and successfully
executed all five operations.

The walkthrough verified:

- Book creation and retrieval, including `201`, `Location`, and strong `ETag`.
- A single-field update and a multi-field update.
- A normalized no-op preserving the current version and history.
- A stale update returning `412` without changing state or history.
- Title, Short Description, and Author Name search.
- Two numbered Book pages with deterministic ordering.
- Two history cursor pages in ascending and descending order.
- Changed Field, inclusive `changedFrom`, exclusive `changedBefore`, direction,
  and combined history filters.
- Complete Change Set projection when only one nested change matched a filter.
- Invalid input and queries returning `400`, missing Books returning `404`,
  stale preconditions returning `412`, and missing preconditions returning
  `428`, all as `application/problem+json` with stable `code` values.
- Browser-like CORS preflight and GET requests, including wildcard origin
  access and `Access-Control-Expose-Headers: ETag`.
- OpenAPI request and response bodies, query and header parameters, Problem
  Details responses, `If-Match`, and `ETag` response headers.
- Development-only Swagger and OpenAPI; both returned `404` in Production.

One intentionally interrupted database request returned the centrally handled
generic `500 application/problem+json` response while the verification
recreated the PostgreSQL container. A retry after PostgreSQL became healthy
succeeded.

## PostgreSQL inspection

The schema contained exactly the two domain tables, `Books` and `BookChanges`,
plus EF Core's migration-history table. Inspection confirmed:

- `Books.Authors` is `text[]`.
- `BookChanges.OldValue` and `BookChanges.NewValue` are `jsonb`.
- Primary keys, the restrictive Book foreign key, Changed Field and Book
  invariant checks, and the unique Changed Field-per-Change Set constraint.
- Both history ordering and Changed Field filtering indexes.
- A four-change creation Change Set and correctly sized update Change Sets.
- No additional changes for the no-op or stale update.
- Backward and forward history index scans through `EXPLAIN`.

A forced race sent two updates with the same current ETag. PostgreSQL and EF
Core committed one request as `200` and rejected the other as `412`; the Book
advanced once and exactly one complete Change Set was appended.

The named volume retained all five seeded Books across Compose container
removal and recreation. A Production-mode API was then started against a new
empty database: it started successfully, created no tables, and therefore did
not apply migrations automatically.

## Deviations and follow-up

No accepted deviations or follow-up issues remain for the initial
implementation specification.
