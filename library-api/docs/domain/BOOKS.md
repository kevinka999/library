# Books Domain Model

This document describes the behavioral model for Books and their change history. Terminology is defined in [`../../CONTEXT.md`](../../CONTEXT.md); feature-specific API requirements remain in tracked issues and the implemented HTTP contract belongs in OpenAPI.

## Book

A Book is the current state of a published work. It has:

- A database-generated identifier.
- A title.
- A short description.
- A publication date.
- A non-empty unordered collection of author names.
- A version used for optimistic concurrency.

Book owns its invariants and determines whether a requested replacement is an effective change. Application code coordinates loading and persistence but must not reproduce these business rules.

### Title

- Required and nonblank after trimming.
- No more than 300 characters.

### Short description

- Required and nonblank after trimming.
- No more than 1,000 characters.

### Publication date

- Required.
- Contains only a calendar date, with no time or timezone.
- May be in the future.

### Author names

- At least one author name is required.
- Each name is trimmed, nonblank, and no more than 200 characters.
- Names have no independent identity, lifecycle, or role.
- There is no Author entity or author CRUD.
- Duplicate names are rejected case-insensitively.
- Ordering has no domain meaning.
- The normalized collection uses deterministic alphabetical ordering so equivalent inputs produce equivalent state.

Changing `["Kevin"]` to `["Kevin", "Chris"]` is one change to the author-name collection. The Domain does not classify it as `AuthorAdded` or assign special co-author semantics.

## Updating a Book

An update supplies the complete replacement state. Book compares that state with its current normalized state and reports one change for every field whose effective value differs:

- Title
- Short description
- Publication date
- Author names

All validation succeeds before any new state is accepted.

A request that is semantically identical after normalization is a no-op. It creates no Book Changes and does not advance the version.

## Optimistic concurrency

Every persisted Book representation has a version. An update succeeds only when the version supplied by the client matches the current version.

If the client version is stale:

- The Book is not overwritten.
- Its version is not advanced.
- No Book Changes are recorded.

ETag and `If-Match` are API representations of this rule; they are not Domain concepts.

## Book Change

A Book Change is an immutable record of one Book field transitioning from a previous value to a new value.

It records:

- Its own identifier.
- The Book identifier.
- The Change Set identifier.
- The changed field.
- The previous value.
- The new value.
- The UTC time at which the change occurred.

Previous and new values preserve their natural JSON shape. Text fields use JSON strings, the publication date uses a date string, and author names use a JSON array. The API returns these values as data and does not generate presentation descriptions such as “Title was changed to The Hobbit.”

Book Changes are append-only and cannot be edited or deleted.

## Change Set

A Change Set groups every Book Change produced by one successful creation or update.

- One modification to one field produces one Book Change.
- One request modifying multiple fields produces multiple Book Changes with the same Change Set identifier.
- A no-op update produces no Change Set.
- A Change Set is treated as one complete history item and must not be split across cursor pages.

Creation records the initial values as one Change Set. Each populated Book field receives its own Book Change.

## Book History

Book History is queried separately from the current Book. It is not a collection loaded into or owned by the Book aggregate.

History behavior includes:

- Cursor pagination.
- Ascending or descending chronological direction.
- Optional filtering by changed field and earliest change time.
- Returning complete Change Sets.

When a changed-field filter matches one Book Change, the entire containing Change Set is returned. This preserves the full meaning of the user action that produced it.

Grouping by Change Set is built into the history representation. No additional user-selectable grouping behavior is required.

