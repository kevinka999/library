# Initial Implementation Plan

## How to use this plan

This checklist turns
[`../specs/01-initial-implementation.md`](../specs/01-initial-implementation.md)
into independently implementable vertical slices.

For each slice:

1. Confirm that every entry blocker is resolved.
2. Implement only the work listed for that slice.
3. Run its automated and manual verification.
4. Review the diff against its requirements and acceptance criteria.
5. Mark the slice complete and create the suggested focused commit.

Do not begin the next slice merely because the code compiles. A slice is complete
only when all of its acceptance criteria, verification steps, and pre-commit
checks are marked complete.

## Progress

- [x] Decision gate complete
- [x] Slice 1 — Runnable API foundation
- [ ] Slice 2 — Create a Book
- [ ] Slice 3 — Retrieve a Book
- [ ] Slice 4 — Replace a Book safely
- [ ] Slice 5 — Search and page Books
- [ ] Slice 6 — Browse Book History
- [ ] Slice 7 — Delivery verification

## Dependency order

```text
Decision gate
    |
    v
Slice 1: Runnable API foundation
    |
    v
Slice 2: Create a Book
    |
    +-----------> Slice 3: Retrieve a Book
    |                        |
    |                        v
    +-----------> Slice 4: Replace a Book safely
    |                        |
    |                        v
    +-----------> Slice 5: Search and page Books
                             |
                             v
                  Slice 6: Browse Book History
                             |
                             v
                  Slice 7: Delivery verification
```

The implementation order remains linear even where technical dependencies would
permit parallel work. This keeps every review and commit based on a known-good
repository state.

## Decision gate

The specification deliberately leaves six contract and verification decisions
open. Resolve them before implementation so later slices do not invent
incompatible behavior.

The defaults below are recommendations. Accept them by checking the decision, or
replace the proposed value in this document before checking it.

### D-001 — ETag contract

**Proposed decision**

- Emit a strong ETag containing the decimal Book Version, for example `"1"`.
- Accept exactly one strong `If-Match` value in that format.
- Reject weak ETags, wildcard values, multiple values, and malformed values with
  `400 Bad Request`.
- Return `428 Precondition Required` only when the header is absent.
- Return `412 Precondition Failed` when the header is valid but does not match the
  current version.

- [x] D-001 accepted or replaced with an explicit alternative

### D-002 — HTTP schemas and Problem Details

**Proposed decision**

- Use the ASP.NET Core camel-case JSON convention.
- Keep transport request and response models in the API project.
- Return Book properties as `id`, `title`, `shortDescription`, `publishDate`,
  `authors`, and `version`.
- Represent `publishDate` as `YYYY-MM-DD`.
- Add a stable `code` extension to Problem Details for programmatic handling.
- Use one validation Problem Details response containing all detected input
  errors.
- Add concrete request, success, and failure examples to OpenAPI while the
  affected endpoint is implemented.

- [x] D-002 accepted or replaced with an explicit alternative

### D-003 — Unknown query parameters

**Proposed decision**

- Reject unknown query parameters with `400 Bad Request`.
- Report each unknown parameter in the validation Problem Details response.

- [x] D-003 accepted or replaced with an explicit alternative

### D-004 — Cursor contract

**Proposed decision**

- Encode a versioned JSON cursor payload with unpadded Base64URL.
- Include cursor version, sort direction, normalized Changed Field filters,
  normalized time filters, last Changed At, and last Change Set ID.
- Sort and deduplicate Changed Field filters before placing them in the cursor.
- Normalize timestamps to UTC.
- Validate the full cursor state against the current request.
- Do not add signing or encryption for this company test. Treat cursors as opaque
  client contract, not as trusted input.

- [x] D-004 accepted or replaced with an explicit alternative

### D-005 — Database concurrency

**Proposed decision**

- Configure Book Version as an EF Core concurrency token.
- Execute an effective update with both Book ID and expected Version in the
  database predicate.
- Translate the affected-row/concurrency failure into the Application stale
  result and ultimately `412 Precondition Failed`.
- Perform the HTTP precondition check before no-op detection.
- Persist the Book and its Book Changes in one transaction.

- [x] D-005 accepted or replaced with an explicit alternative

### D-006 — Database verification scope

**Proposed decision**

- Keep automated integration tests and Testcontainers out of scope.
- Unit-test Domain and Application behavior with hand-written fakes.
- Manually verify EF mappings, PostgreSQL queries, constraints, transactions, and
  concurrency against the Compose database in every affected slice.
- Record the exact manual commands and representative results in the pull request
  or delivery notes.

- [x] D-006 accepted or replaced with an explicit alternative

### Decision-gate exit criteria

- [x] All six decisions are checked
- [x] The specification is updated if any chosen decision changes its requirements
- [x] A new ADR is added if a decision is architecturally durable
- [x] No slice below has an unresolved decision blocker

---

## Slice 1 — Runnable API foundation

### Verifiable outcome

The repository builds as the target Clean Architecture solution. In Development,
the API starts, serves OpenAPI and Swagger UI, applies the cross-cutting API
policies, and no longer exposes the generated Weather Forecast endpoint.

### Requirements

`API-001`, `API-003`, `API-004`, `API-005`, `API-006`, `OPS-005`

### Entry blockers

- **Blocked by:** D-002
- **Blocked by:** A locally installed .NET 10 SDK
- **Blocked by:** Selection of a .NET 10-compatible Swagger UI package

### Implementation checklist

#### Solution and project boundaries

- [x] Create a solution containing `Library.Domain`, `Library.Application`,
  `Library.Infrastructure`, `Library.Api`, and `Library.UnitTests`
- [x] Move the existing web SDK project into `Library.Api`
- [x] Set project references to match
  [`../ARCHITECTURE.md`](../ARCHITECTURE.md)
- [x] Verify Domain has no project dependencies
- [x] Verify Application references only Domain
- [x] Verify Infrastructure references Application and Domain
- [x] Verify API references Application and Infrastructure
- [x] Verify UnitTests references only the projects it tests
- [x] Preserve nullable reference types and implicit usings

#### HTTP foundation

- [x] Remove `WeatherForecast` and `WeatherForecastController`
- [x] Configure controller discovery and JSON conventions
- [x] Add central unexpected-exception handling with safe Problem Details
- [x] Add reusable expected-result-to-Problem-Details mapping
- [x] Configure open CORS without credentials and expose `ETag`
- [x] Enable OpenAPI at `/openapi/v1.json` only in Development
- [x] Enable Swagger UI at `/swagger` only in Development
- [x] Add dependency-injection composition extension points for Application and
  Infrastructure

#### Tests

- [x] Add a unit test for each pure expected-result mapping
- [x] Add a unit test proving unexpected exception details are not exposed, if
  this can be tested without introducing an HTTP integration-test host
- [x] Keep test doubles hand-written

### Acceptance criteria

- [x] `dotnet build` succeeds
- [x] `dotnet test` succeeds
- [x] The API starts in Development
- [x] `GET /openapi/v1.json` returns the OpenAPI document in Development
- [x] `GET /swagger` loads Swagger UI in Development
- [x] The Weather Forecast route returns `404`
- [x] A representative CORS response allows an arbitrary origin, does not allow
  credentials, and exposes `ETag`
- [x] OpenAPI and Swagger UI are unavailable outside Development
- [x] An unexpected exception returns safe `application/problem+json`
- [x] Project references match the documented dependency direction

### Pre-commit review

- [x] Review the complete diff for accidental domain or persistence decisions
- [x] Confirm no sample Weather Forecast files remain
- [x] Confirm no secrets or machine-specific configuration were added
- [x] Confirm `docs/ARCHITECTURE.md` remains accurate
- [x] Mark Slice 1 complete in [Progress](#progress)

**Suggested commit:** `feat(api): establish runnable API foundation`

---

## Slice 2 — Create a Book

### Verifiable outcome

A client can submit a valid Book to `POST /api/books`. The API stores its current
state and one complete initial Change Set in PostgreSQL, then returns the created
Book, its location, and version-backed ETag.

### Requirements

`BOOK-001`, `BOOK-002`, `BOOK-003`, `BOOK-004`, `API-001`, `API-002`,
`OPS-001`, `OPS-003`, `OPS-004`, `TEST-001`, `TEST-002`, `TEST-003`,
`TEST-004`

### Entry blockers

- **Blocked by:** Slice 1
- **Blocked by:** D-002
- **Blocked by:** D-006
- **Blocked by:** Docker with Compose support
- **Blocked by:** A local source for the development database secret

### Implementation checklist

#### Domain behavior

- [ ] Implement the Book identity, current state, and Version
- [ ] Implement Title, Short Description, Publish Date, and Author Name
  invariants from [`../domain/BOOKS.md`](../domain/BOOKS.md)
- [ ] Normalize whitespace and deterministic Author Name ordering
- [ ] Reject duplicate Author Names case-insensitively while preserving display
  casing
- [ ] Implement stable Changed Field values
- [ ] Implement immutable Book Change data
- [ ] Create exactly one initial Book Change per Book field
- [ ] Give all initial Book Changes one generated Change Set ID and one UTC
  Changed At value
- [ ] Keep Book History outside the Book aggregate

#### Application behavior

- [ ] Add the Create Book command, input, result, validator, and handler
- [ ] Add only the persistence, unit-of-work, clock, and ID-generation
  abstractions required by creation
- [ ] Validate all input before accepting any state
- [ ] Return explicit validation outcomes rather than expected exceptions
- [ ] Commit the Book and initial Book Changes atomically

#### PostgreSQL infrastructure

- [ ] Add EF Core 10 and the compatible Npgsql provider
- [ ] Map `Books` and `BookChanges` as the only domain tables
- [ ] Map Author Names to `text[]`
- [ ] Map old and new Book Change values to `jsonb`
- [ ] Configure identity IDs, UUID Change Set IDs, UTC timestamps, Version, stable
  Changed Field strings, and required constraints
- [ ] Add the foreign key and one-change-per-field-per-Change-Set constraint
- [ ] Generate and commit the initial migration
- [ ] Add Docker Compose for PostgreSQL 18 with a named volume
- [ ] Add safe configuration examples without committing credentials
- [ ] Apply pending migrations automatically only in Development

#### HTTP endpoint

- [ ] Add the create request and Book response contracts
- [ ] Add `POST /api/books`
- [ ] Map validation results to the agreed Problem Details contract
- [ ] Return `201 Created`, `Location`, the complete Book, and ETag
- [ ] Document the endpoint, schemas, headers, and responses in OpenAPI

#### Tests

- [ ] Test creation with normalized valid data
- [ ] Test every Book field validation boundary
- [ ] Test blank and duplicate Author Names
- [ ] Test deterministic Author Name ordering
- [ ] Test that four initial field changes share one Change Set and timestamp
- [ ] Test that every initial old value is null and every new value has its
  natural JSON shape
- [ ] Test returned Book state and version
- [ ] Test persistence and transaction intent through hand-written fakes
- [ ] Test create-result HTTP mapping where it remains pure

### Acceptance criteria

- [ ] `dotnet build` succeeds
- [ ] `dotnet test` succeeds
- [ ] Docker Compose starts PostgreSQL 18 with a persistent named volume
- [ ] Starting the API in Development applies the initial migration
- [ ] A valid request returns `201`, `Location`, ETag `"1"`, and the normalized
  complete Book
- [ ] The database contains one Book and exactly four corresponding initial Book
  Changes
- [ ] The four changes share Book ID, Change Set ID, and Changed At
- [ ] Stored Author Names use `text[]`; stored old and new values use `jsonb`
- [ ] Invalid input returns the agreed validation Problem Details and writes
  nothing
- [ ] Restarting PostgreSQL preserves created data
- [ ] Production startup does not apply migrations automatically

### Pre-commit review

- [ ] Compare implemented invariants with `docs/domain/BOOKS.md`
- [ ] Confirm Domain contains no EF Core, HTTP, or serialization dependencies
- [ ] Confirm the transaction cannot persist a Book without its initial changes
- [ ] Inspect the generated migration for the two-table model and constraints
- [ ] Confirm secrets and real connection strings are untracked
- [ ] Update operational documentation with the verified Compose, migration, and
  run commands
- [ ] Mark Slice 2 complete in [Progress](#progress)

**Suggested commit:** `feat(api): create books with initial history`

---

## Slice 3 — Retrieve a Book

### Verifiable outcome

A client can retrieve the current representation of an existing Book by ID,
including its current ETag, and can distinguish a missing Book from all other
results.

### Requirements

`BOOK-005`, `BOOK-006`, `API-001`, `API-002`, `TEST-007`

### Entry blockers

- **Blocked by:** Slice 2
- **Blocked by:** D-002

### Implementation checklist

#### Application and persistence

- [ ] Add the Get Book query, result, and handler
- [ ] Add or extend the focused Book read abstraction without introducing a
  generic repository
- [ ] Return an explicit not-found result
- [ ] Implement an efficient no-tracking current-state query
- [ ] Do not load Book History

#### HTTP endpoint

- [ ] Add `GET /api/books/{id}`
- [ ] Return the complete Book and its version-backed ETag
- [ ] Map a missing Book to the agreed `404` Problem Details
- [ ] Document the endpoint, ETag, success body, and failure body in OpenAPI

#### Tests

- [ ] Test an existing Book result
- [ ] Test the not-found result
- [ ] Test that history is not requested or loaded
- [ ] Test pure success and not-found HTTP mapping

### Acceptance criteria

- [ ] `dotnet build` succeeds
- [ ] `dotnet test` succeeds
- [ ] Retrieving a Book created in Slice 2 returns `200`, its complete normalized
  representation, and ETag `"1"`
- [ ] Retrieving an unknown ID returns `404 application/problem+json`
- [ ] The current-state query does not load Book Changes
- [ ] OpenAPI documents both results and the ETag response header

### Pre-commit review

- [ ] Confirm the controller contains only transport translation
- [ ] Confirm the read abstraction is use-case-focused
- [ ] Confirm no Book History navigation was introduced
- [ ] Mark Slice 3 complete in [Progress](#progress)

**Suggested commit:** `feat(api): retrieve books by id`

---

## Slice 4 — Replace a Book safely

### Verifiable outcome

A client can replace all editable Book fields with the current ETag. Effective
updates advance the version and append one atomic Change Set; identical updates
are no-ops; stale or missing preconditions never mutate state.

### Requirements

`BOOK-007` through `BOOK-014`, `API-001`, `API-002`, `TEST-005`, `TEST-006`

### Entry blockers

- **Blocked by:** Slice 3
- **Blocked by:** D-001
- **Blocked by:** D-002
- **Blocked by:** D-005
- **Blocked by:** D-006

### Implementation checklist

#### Domain behavior

- [ ] Implement complete replacement through `Book.Update`
- [ ] Validate the entire replacement before mutating any current state
- [ ] Detect effective field changes after normalization
- [ ] Treat Author Names as an unordered set
- [ ] Treat display-casing corrections as effective changes
- [ ] Ignore author-order-only changes
- [ ] Return transient field changes with natural old and new value shapes
- [ ] Increment Version once only when at least one effective change exists
- [ ] Produce no Change Set for a no-op

#### Application and persistence

- [ ] Add the Update Book command, input, result, validator, and handler
- [ ] Represent missing, stale, invalid, no-op, and updated outcomes explicitly
- [ ] Check the supplied version before no-op detection
- [ ] Give every effective field change one Change Set ID and timestamp
- [ ] Save current state and Book Changes atomically
- [ ] Configure Version as the agreed concurrency token
- [ ] Translate an actual database concurrency race into the stale result

#### HTTP endpoint

- [ ] Add the complete-replacement request contract
- [ ] Add `PUT /api/books/{id}`
- [ ] Parse and validate `If-Match` according to D-001
- [ ] Return `428` for an absent header
- [ ] Return `400` for a malformed or unsupported header
- [ ] Return `412` for a valid stale header
- [ ] Return `200`, the current Book, and unchanged ETag for a no-op
- [ ] Return `200`, the updated Book, and advanced ETag for an effective update
- [ ] Document every header, body, and outcome in OpenAPI

#### Tests

- [ ] Test each field changing independently
- [ ] Test multiple fields changing in one Change Set
- [ ] Test author trimming, case-insensitive duplicate rejection, deterministic
  ordering, order independence, and casing correction
- [ ] Test full validation before mutation
- [ ] Test current-version no-op behavior
- [ ] Test stale-version behavior even when the submitted body is otherwise a
  no-op
- [ ] Test missing Book behavior
- [ ] Test that invalid, missing, and stale outcomes cause no persistence or
  history mutation
- [ ] Test one Version increment per effective update
- [ ] Test transaction intent through hand-written fakes
- [ ] Test ETag parsing and HTTP result mapping

### Acceptance criteria

- [ ] `dotnet build` succeeds
- [ ] `dotnet test` succeeds
- [ ] An effective update with the current ETag returns `200`, ETag `"2"`, and
  the complete updated Book
- [ ] The database contains one new immutable Book Change per changed field
- [ ] Every change from the request shares one Change Set ID and timestamp
- [ ] The Book update and its changes are committed atomically
- [ ] A current-version no-op preserves Version and creates no history
- [ ] A stale update returns `412` and leaves current state and history unchanged
- [ ] A missing `If-Match` returns `428`
- [ ] Unsupported or malformed `If-Match` syntax returns `400`
- [ ] Two competing updates using the same ETag cannot both succeed
- [ ] Existing Book Changes cannot be updated or deleted through the application

### Pre-commit review

- [ ] Confirm the precondition check precedes no-op detection
- [ ] Confirm Domain owns comparison and invariants
- [ ] Confirm database concurrency, not only an in-memory check, closes the race
- [ ] Inspect one-field and multi-field rows directly in PostgreSQL
- [ ] Confirm controller branches match the agreed status-code contract
- [ ] Mark Slice 4 complete in [Progress](#progress)

**Suggested commit:** `feat(api): replace books with optimistic concurrency`

---

## Slice 5 — Search and page Books

### Verifiable outcome

A client can retrieve deterministic numbered pages of Books and optionally find
Books by a case-insensitive term across title, description, and Author Names.

### Requirements

`SEARCH-001` through `SEARCH-007`, `API-001`, `API-002`, `TEST-008`

### Entry blockers

- **Blocked by:** Slice 4
- **Blocked by:** D-002
- **Blocked by:** D-003
- **Blocked by:** D-006

### Implementation checklist

#### Application behavior

- [ ] Add the Search Books query, result models, validator, and handler
- [ ] Trim an optional search term
- [ ] Validate one-based page and page-size bounds
- [ ] Calculate `totalPages` consistently, including an empty result
- [ ] Add a focused paged Book read abstraction

#### PostgreSQL query

- [ ] Implement case-insensitive contains matching for Title and Short Description
- [ ] Implement case-insensitive any-element contains matching for `text[]`
  Author Names
- [ ] Ensure user search text is treated as text rather than as an unescaped SQL
  pattern
- [ ] Order by Title ascending and then Book ID ascending
- [ ] Apply count and page projection efficiently without loading history

#### HTTP endpoint

- [ ] Add `GET /api/books`
- [ ] Bind only `search`, `page`, and `pageSize`
- [ ] Apply the D-003 unknown-parameter policy
- [ ] Return `items`, `page`, `pageSize`, `totalCount`, and `totalPages`
- [ ] Map invalid input to the agreed validation Problem Details
- [ ] Document defaults, limits, search behavior, ordering, and responses in
  OpenAPI

#### Tests

- [ ] Test default paging input
- [ ] Test valid boundary values
- [ ] Test zero, negative, and over-limit values
- [ ] Test search trimming
- [ ] Test propagation through the read abstraction
- [ ] Test total-page calculation for empty, partial, and exact-multiple results
- [ ] Test the D-003 unknown-parameter policy where it can be isolated

### Acceptance criteria

- [ ] `dotnet build` succeeds
- [ ] `dotnet test` succeeds
- [ ] With no query, the endpoint returns page 1 with a page size of 20
- [ ] Search matches differing case in Title, Short Description, and Author Names
- [ ] Search terms containing `%`, `_`, quotes, or backslashes are handled as
  literal user input and do not broaden or break the query
- [ ] Results remain ordered by Title and Book ID across repeated requests
- [ ] Response totals are correct for empty, partial, and full pages
- [ ] Invalid paging returns `400 application/problem+json`
- [ ] Unknown parameters follow D-003
- [ ] No Book Changes are loaded by the query

### Pre-commit review

- [ ] Inspect generated SQL or database logs for each search target
- [ ] Confirm search input is parameterized and wildcard behavior is intentional
- [ ] Confirm deterministic ordering includes the Book ID tie-breaker
- [ ] Confirm count and page queries use the same filter
- [ ] Mark Slice 5 complete in [Progress](#progress)

**Suggested commit:** `feat(api): search and page books`

---

## Slice 6 — Browse Book History

### Verifiable outcome

A client can cursor-page through complete Change Sets for an existing Book,
newest-first or oldest-first, and filter them by Changed Field and UTC time
without splitting a matching Change Set.

### Requirements

`HISTORY-001` through `HISTORY-014`, `API-001`, `API-002`, `TEST-009`

### Entry blockers

- **Blocked by:** Slice 5
- **Blocked by:** D-002
- **Blocked by:** D-003
- **Blocked by:** D-004
- **Blocked by:** D-006
- **Blocked by:** Creation and update data from Slices 2 and 4 for manual
  verification

### Implementation checklist

#### Application behavior

- [ ] Add the Get Book History query, result models, validator, and handler
- [ ] Verify Book existence before reading history
- [ ] Validate limit, sort direction, Changed Fields, UTC bounds, and cursor
- [ ] Reject an invalid time range
- [ ] Normalize filters exactly once
- [ ] Add a focused history read abstraction that returns complete Change Sets
- [ ] Implement cursor encode/decode as an isolated Application-owned contract
- [ ] Bind the cursor to every normalized filter and the sort direction

#### PostgreSQL query

- [ ] Add indexes supporting Book ID, Changed At, Change Set ID, and Changed Field
- [ ] Select matching Change Set IDs before projecting their complete changes
- [ ] Apply repeated Changed Field filters with any-match semantics
- [ ] Apply inclusive `changedFrom` and exclusive `changedBefore`
- [ ] Apply keyset comparison consistently for ascending and descending order
- [ ] Use Changed At and Change Set ID as the complete cursor key
- [ ] Fetch one extra Change Set to determine `hasMore`
- [ ] Return at most `limit` complete Change Sets
- [ ] Keep change ordering within a Change Set deterministic
- [ ] Generate and inspect the index migration

#### HTTP endpoint

- [ ] Add `GET /api/books/{id}/history`
- [ ] Bind repeated `changedField`, `changedFrom`, `changedBefore`,
  `sortDirection`, `limit`, and `after`
- [ ] Apply the D-003 unknown-parameter policy
- [ ] Return `items`, `nextCursor`, and `hasMore`
- [ ] Represent Changed At once per item and changes as a nested collection
- [ ] Preserve natural JSON types for old and new values
- [ ] Return `404` for a missing Book
- [ ] Return `400` for invalid filters, ranges, cursors, or cursor/query mismatch
- [ ] Document filters, defaults, cursor opacity, schemas, and responses in
  OpenAPI

#### Tests

- [ ] Test missing Book without invoking the history reader
- [ ] Test default and boundary limits
- [ ] Test ascending and descending propagation
- [ ] Test inclusive lower and exclusive upper time bounds
- [ ] Test repeated Changed Field normalization and any-match behavior
- [ ] Test valid cursor round-trip
- [ ] Test malformed, unsupported-version, and incompatible cursors
- [ ] Test complete Change Set projection when only one nested change matches a
  field filter
- [ ] Test `nextCursor` and `hasMore` result propagation

### Acceptance criteria

- [ ] `dotnet build` succeeds
- [ ] `dotnet test` succeeds
- [ ] Missing Book history returns `404`, not an empty page
- [ ] Default history returns at most 20 complete Change Sets newest-first
- [ ] Ascending history returns the same qualifying Change Sets in reverse
  chronological order
- [ ] No Change Set is split between pages
- [ ] Following `nextCursor` produces no duplicate or omitted Change Set in a
  stable dataset
- [ ] Inserting a newer Change Set does not destabilize traversal from an existing
  descending cursor
- [ ] A Changed Field match returns every Book Change in the matching Change Set
- [ ] Time filters obey inclusive-from and exclusive-before boundaries
- [ ] Invalid and incompatible cursors return `400`
- [ ] Response old and new values retain string, date-string, null, and array
  JSON shapes
- [ ] Query plans use the intended history indexes on a representative dataset

### Pre-commit review

- [ ] Inspect ascending and descending keyset predicates for symmetry
- [ ] Confirm pagination counts Change Sets rather than Book Change rows
- [ ] Confirm every filter is included in cursor compatibility validation
- [ ] Confirm malformed cursor contents never become trusted query input
- [ ] Inspect filtered results directly against PostgreSQL rows
- [ ] Mark Slice 6 complete in [Progress](#progress)

**Suggested commit:** `feat(api): query complete book change sets`

---

## Slice 7 — Delivery verification

### Verifiable outcome

The complete API can be started from a clean local checkout using documented
commands, all automated checks pass, and every specified endpoint and
PostgreSQL-specific behavior has a recorded manual verification result.

### Requirements

`OPS-001` through `OPS-005`, `API-001` through `API-006`, `TEST-001` through
`TEST-010`, and all manual verification criteria in the specification

### Entry blockers

- **Blocked by:** Slices 1 through 6
- **Blocked by:** All Decision-gate choices being reflected in implementation and
  documentation
- **Blocked by:** Docker and the .NET 10 SDK on the verification machine

### Implementation checklist

#### Clean-environment verification

- [ ] Follow the README from a clean checkout or equivalent clean working copy
- [ ] Supply the development database secret through the documented mechanism
- [ ] Start PostgreSQL with Docker Compose
- [ ] Start the API directly with `dotnet run`
- [ ] Confirm Development migrations apply without manual schema steps
- [ ] Confirm the named volume retains data across container recreation
- [ ] Confirm production-mode startup does not apply migrations

#### Full behavior walkthrough

- [ ] Create a Book
- [ ] Retrieve the created Book
- [ ] Perform one single-field update
- [ ] Perform one multi-field update
- [ ] Repeat an identical current-version update
- [ ] Attempt a stale update
- [ ] Search by title
- [ ] Search by description
- [ ] Search by Author Name
- [ ] Traverse at least two numbered Book pages
- [ ] Traverse at least two history cursor pages in each direction
- [ ] Apply each history filter independently and in combination
- [ ] Verify every expected failure status and Problem Details content type
- [ ] Verify CORS and ETag exposure from a browser-like request
- [ ] Exercise every operation through Swagger UI

#### Persistence inspection

- [ ] Confirm exactly two domain tables exist
- [ ] Confirm the expected foreign key, uniqueness constraints, and indexes exist
- [ ] Confirm Author Names are stored as `text[]`
- [ ] Confirm Book Change values are stored as `jsonb`
- [ ] Confirm creation and update Change Sets are complete
- [ ] Confirm no-op and stale updates created no Book Changes
- [ ] Confirm a forced concurrent update cannot partially commit
- [ ] Confirm history query plans use the intended indexes

#### Documentation and contract review

- [ ] Update README build, test, Compose, migration, run, and secret-setup commands
- [ ] Confirm `CONTEXT.md` matches implemented vocabulary
- [ ] Confirm `docs/domain/BOOKS.md` matches implemented behavior
- [ ] Confirm `docs/ARCHITECTURE.md` matches actual project boundaries and flows
- [ ] Confirm ADRs match the implemented persistence choices
- [ ] Confirm the OpenAPI contract matches every implemented route and outcome
- [ ] Remove obsolete sample requests from `library-api.http` or replace them with
  representative Book requests
- [ ] Record manual verification evidence in the pull request or delivery notes

### Acceptance criteria

- [ ] `dotnet build` succeeds from a clean checkout
- [ ] `dotnet test` succeeds from a clean checkout
- [ ] All five endpoints satisfy their slice-level acceptance criteria
- [ ] Swagger UI exposes all five operations and their complete contracts
- [ ] No secret, generated build output, or machine-specific file is tracked
- [ ] No implementation is left dependent on an unchecked manual assumption
- [ ] All authoritative documentation is accurate
- [ ] Every requirement ID is covered by an implemented slice
- [ ] Every item in [Progress](#progress) is checked

### Pre-commit review

- [ ] Review the full branch diff against the implementation specification
- [ ] Review package references and remove unused dependencies
- [ ] Review migrations and runtime configuration one final time
- [ ] Confirm formatting and repository naming are consistent
- [ ] Confirm all prior commits are focused and use `type(api): description`
- [ ] Mark Slice 7 complete in [Progress](#progress)

**Suggested commit:** `docs(api): finalize delivery instructions`

## Final completion record

Fill this section when Slice 7 is complete.

- **Implementation completed by:**
- **Completion date:**
- **Final commit:**
- **Automated verification:**
- **Manual verification evidence:**
- **Accepted deviations from the specification:**
- **Follow-up issues:**
