# Initial Web Implementation

## Purpose

Build the first browser interface for managing Books. This document defines
frontend behavior; [`../openapi/v1.json`](../openapi/v1.json) defines HTTP
shapes and [`../../CONTEXT.md`](../../CONTEXT.md) defines product language.

## Routes

| Route | Responsibility |
| --- | --- |
| `/` | Redirect to `/books` |
| `/books` | Search and numbered Book pages, with Book creation in a modal |
| `/books/:bookId` | Current Book details, inline editing, and history |

Unknown routes show a useful not-found screen. Invalid Book IDs never trigger an
API request.

## Requirements

### Browse

- **WEB-001:** `/books` shows API-ordered results with title, author names,
  Publish Date, and a link to the stable detail route.
- **WEB-002:** `search`, `page`, and `pageSize` live in URL search params.
  Changing search or page size resets `page` to 1.
- **WEB-003:** Loading, no-results, invalid-query, and unexpected-error states are
  distinct. Pagination uses the API totals and stays keyboard accessible.

### Create and view

- **WEB-004:** A button on `/books` opens the create form in a modal. The form
  requires Title, Short Description, Publish Date, and at least one Author Name,
  and submits the complete API input.
- **WEB-005:** Client validation improves feedback but does not replace server
  validation. API field errors appear beside matching controls.
- **WEB-006:** A successful create caches the returned Book and ETag, then
  navigates to `/books/{id}`.
- **WEB-007:** The detail route shows the complete current representation and
  allows editing in the same page with a Formik form and Yup validation. Book
  History appears below the current Book information.

### Replace safely

- **WEB-008:** The inline edit form starts from one Book response and retains
  that response's exact ETag.
- **WEB-009:** Update sends every editable field with `If-Match`; a successful
  response replaces cached Book data and ETag.
- **WEB-010:** On `412`, the draft is preserved and the UI explains that a newer
  version exists. Reloading current data is an explicit user action; stale data
  is never silently resubmitted.
- **WEB-011:** A missing Book shows not-found. A `428` or missing successful
  response ETag is reported as an integration failure.

### History

- **WEB-012:** History is loaded separately from current Book state and uses an
  infinite query with the opaque `nextCursor` as `after`.
- **WEB-013:** Filters for Changed Field, inclusive start, exclusive end, and
  chronological direction live in the URL. Changing a filter starts a new cursor chain.
- **WEB-014:** Each history item renders one complete Change Set as one item in
  a vertical timeline. Its marker aligns with the item title and a line connects
  it to the next item. Field filters never hide the other Book Changes returned
  in that Change Set.
- **WEB-015:** The frontend converts structured old/new values into localized,
  human-readable descriptions and handles null, strings, date strings, and
  Author Name arrays.
- **WEB-016:** History has no numbered pages. When `hasMore` is true, a localized
  Load More button at the end requests the next opaque cursor page and appends
  complete Change Sets without replacing earlier items.

### Localization

- **WEB-017:** All application-owned interface copy is available in English and
  German, including navigation, controls, form feedback, empty/loading/error
  states, pagination, and history descriptions. Book field values and history
  values are never translated.
- **WEB-018:** A language select is always available and presents `🇩🇪 Deutsch`
  and `🇬🇧 English`. Each option also has an accessible language name; changing
  language updates the document language and locale-sensitive formatting.

## Out of scope

Authentication, deletion, author management, editing history, custom Book
sorting, offline mutation, and realtime updates are not part of this delivery.

## Delivery slices

1. Root providers, two-route shell, theme tokens, shadcn/ui foundation, HTTP
   client, test harness, and English/German localization.
2. Typed Book API, query keys, searchable numbered table, and create modal.
3. Detail page and concurrency-safe inline Formik/Yup edit flow.
4. Cursor-paged timeline, filters, localized change descriptions, accessibility
   pass, behavior tests, error recovery, and responsive verification.
