# Initial Web Implementation

## Purpose

Build the first browser interface for managing Books. This document defines
frontend behavior; [`../openapi/v1.json`](../openapi/v1.json) defines HTTP
shapes and [`../../CONTEXT.md`](../../CONTEXT.md) defines product language.

## Routes

| Route | Responsibility |
| --- | --- |
| `/` | Redirect to `/books` |
| `/books` | Search and numbered Book pages |
| `/books/new` | Create a Book |
| `/books/:bookId` | Current Book details and history |
| `/books/:bookId/edit` | Replace the current Book safely |

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

- **WEB-004:** The create form requires Title, Short Description, Publish Date,
  and at least one Author Name. It submits the complete API input.
- **WEB-005:** Client validation improves feedback but does not replace server
  validation. API field errors appear beside matching controls.
- **WEB-006:** A successful create caches the returned Book and ETag, then
  navigates to `/books/{id}`.
- **WEB-007:** The detail route shows the complete current representation and
  provides explicit edit and history actions.

### Replace safely

- **WEB-008:** The edit form starts from one Book response and retains that
  response's exact ETag.
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
- **WEB-014:** Each history item renders one complete Change Set. Field filters
  never hide the other Book Changes returned in that Change Set.
- **WEB-015:** The frontend converts structured old/new values into localized,
  human-readable descriptions and handles null, strings, date strings, and
  Author Name arrays.

## Out of scope

Authentication, deletion, author management, editing history, custom Book
sorting, offline mutation, and realtime updates are not part of this delivery.

## Delivery slices

1. Root providers, routes, theme tokens, shadcn/ui foundation, and HTTP client.
2. Typed Book API, query keys, search page, and detail page.
3. Shared Book form, create flow, and concurrency-safe edit flow.
4. Cursor-paged history, filters, and localized change descriptions.
5. Accessibility pass, behavior tests, error recovery, and responsive verification.
