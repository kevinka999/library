# Initial Web Implementation Plan

## How to use this plan

This checklist turns
[`../specs/01-initial-implementation.md`](../specs/01-initial-implementation.md)
into four independently reviewable vertical slices. The checked scope decisions
below come from the current product request and take precedence over older
frontend documentation.

For each slice:

1. Confirm that every entry blocker is resolved.
2. Implement only the work listed for that slice.
3. Run its automated and manual verification.
4. Review the diff against its requirements and acceptance criteria.
5. Mark the slice complete and create the suggested focused commit.

A slice is complete only when its acceptance criteria, verification steps, and
pre-commit review are complete. Use `pnpm` for every package and script command,
and do not change `library-api`.

## Progress

- [x] Scope decisions confirmed
- [x] Slice 1 — Localized application foundation
- [ ] Slice 2 — Search, page, and create Books
- [ ] Slice 3 — View and safely edit a Book
- [ ] Slice 4 — Browse Book History and verify the delivery

## Dependency order

```text
Scope decisions
       |
       v
Slice 1: Localized application foundation
       |
       v
Slice 2: Search, page, and create Books
       |
       v
Slice 3: View and safely edit a Book
       |
       v
Slice 4: Browse Book History and verify the delivery
```

Keep the order linear. Each slice should leave a runnable, reviewable frontend
and should not depend on uncommitted work from a later slice.

## Scope decisions

These decisions resolve the places where the current request is more specific
than the original frontend specification.

### D-001 — Routes and form placement

- [x] Keep only `/books` and `/books/:bookId` as user-facing feature routes
- [x] Open Book creation from a button on `/books` in a modal; do not add
  `/books/new`
- [x] Edit the current Book inline on `/books/:bookId`; do not add
  `/books/:bookId/edit`
- [x] Render Book History below the current Book information on the same detail
  page

### D-002 — Localization boundary

- [x] Provide English and German for all application-owned interface copy
- [x] Never translate Book titles, descriptions, Author Names, or stored history
  values
- [x] Use locale-aware presentation for dates, times, and number formatting
  without changing the underlying API values
- [x] Present the language control as a select with `🇩🇪 Deutsch` and
  `🇬🇧 English`, including readable and accessible language names
- [x] Persist the selected language locally and fall back to English when there
  is no supported saved or browser language

### D-003 — API and state boundaries

- [x] Treat [`../openapi/v1.json`](../openapi/v1.json) as the HTTP contract
- [x] Keep server state in TanStack Query and shareable search, pagination, and
  history-filter state in the URL
- [x] Keep modal visibility, edit mode, and unsaved form drafts local
- [x] Preserve ETags as opaque strings; only the API layer reads response ETag
  headers or writes `If-Match`
- [x] Use Formik and Yup for both create and edit forms

### Scope-decision exit criteria

- [x] D-001 through D-003 are confirmed
- [x] The feature specification still reflects all three decisions
- [x] No slice below relies on a create route or edit route

---

## Slice 1 — Localized application foundation

### Verifiable outcome

The Vite scaffold is replaced by a responsive application shell. `/` redirects
to `/books`, unknown routes show a localized not-found screen, the language
select switches the entire shell between English and German, and the frontend
has tested routing, HTTP, styling, and server-state foundations for later
slices.

### Requirements

`WEB-017`, `WEB-018`, routing and architectural foundations

### Entry blockers

- **Blocked by:** Scope decisions
- **Blocked by:** A supported Node.js version and `pnpm`

### Implementation checklist

#### Dependencies and scaffold

- [ ] Remove the generated Vite demo component, styles, and unused assets
- [ ] Add React Router DOM, TanStack Query, Axios, i18next, and react-i18next
  using `pnpm`
- [ ] Add Tailwind CSS and initialize shadcn/ui for the existing Vite project
- [ ] Add only the shadcn/ui primitives used by this foundation, then introduce
  feature-specific primitives in the slice that first uses them
- [ ] Add Vitest, React Testing Library, user-event, jest-dom, jsdom, and MSW for
  behavior tests with HTTP boundaries
- [ ] Add `test` and, if useful for local iteration, `test:watch` scripts without
  weakening the existing `lint` or `build` scripts

#### Global layout and styling

- [ ] Define Tailwind theme variables in the top-level `@theme` block in
  `src/index.css`
- [ ] Keep regular CSS variables that should not produce Tailwind utilities in
  `:root`
- [ ] Build a responsive application shell with a main landmark, a localized
  product heading or navigation label, and a consistently placed language
  select
- [ ] Keep the shell usable at narrow mobile widths and avoid a fixed-width root
  container
- [ ] Use visible focus styles and sufficient contrast for every interactive
  primitive

#### Routing and providers

- [ ] Declare the complete route tree in `src/router.tsx`
- [ ] Redirect `/` to `/books`
- [ ] Add `/books` and `/books/:bookId` page routes
- [ ] Add a localized catch-all not-found route
- [ ] Validate `bookId` as a positive integer before enabling any detail request
- [ ] Compose the router, TanStack Query client, and i18n initialization from
  `src/main.tsx`
- [ ] Keep page loading, not-found, and unexpected-error states owned by their
  pages rather than by the router

#### Localization

- [ ] Configure English and German translation resources in a dedicated i18n
  module
- [ ] Establish translation namespaces or key groups for common UI, Books,
  forms, validation, errors, pagination, and history
- [ ] Add a reusable language select with flag, visible language name, and an
  accessible label that does not rely on the flag alone
- [ ] Persist a valid selection and update `<html lang>` whenever it changes
- [ ] Fall back to English for missing keys and unsupported saved or browser
  languages
- [ ] Add locale helpers for dates, timestamps, and numbers without putting
  stored Book data into translation resources

#### HTTP and error foundation

- [ ] Add `VITE_API_URL` to a safe `.env.example` and document that no secret
  belongs in the frontend environment
- [ ] Create the shared Axios client in `src/api/client.ts`
- [ ] Keep base URL, JSON headers, timeout policy, and HTTP error normalization
  inside the API layer
- [ ] Model Problem Details and validation Problem Details sufficiently to
  distinguish field validation, not-found, stale update, precondition, and
  unexpected failures
- [ ] Ensure raw server-authored English text is never the only user-facing error
  feedback; select localized UI feedback from status and structured field data
- [ ] Do not add a generic repository, generated SDK, or page-level Axios calls

#### Tests

- [ ] Configure a shared test setup with jest-dom cleanup and an MSW server
- [ ] Test root redirect and unknown-route behavior
- [ ] Test that an invalid Book ID renders useful feedback without issuing an
  HTTP request
- [ ] Test English/German switching, persistence, fallback, and `<html lang>`
- [ ] Test representative HTTP error normalization without coupling page tests
  to Axios internals

### Acceptance criteria

- [ ] `pnpm lint`, `pnpm test -- --run`, and `pnpm build` succeed
- [ ] `/` redirects to `/books`
- [ ] `/books` and a valid `/books/:bookId` render inside the application shell
- [ ] An invalid Book ID and an unknown route show distinct localized states
- [ ] Invalid Book IDs do not trigger an API request
- [ ] The language select visibly distinguishes German and English with flags
  and readable names
- [ ] Switching languages updates visible shell copy and `<html lang>` and
  survives a reload
- [ ] The layout and language select are keyboard accessible at desktop and
  mobile widths
- [ ] No code outside `src/api` imports Axios or knows an endpoint path
- [ ] The Vite sample UI and unused sample assets are gone

### Manual verification

- [ ] Start the frontend with `pnpm dev`
- [ ] Visit `/`, `/books`, `/books/1`, `/books/not-an-id`, and an unknown route
- [ ] Switch languages with mouse and keyboard, then reload
- [ ] Inspect the page at approximately 375 px and 1280 px widths

### Pre-commit review

- [ ] Confirm the folder layout matches
  [`../ARCHITECTURE.md`](../ARCHITECTURE.md)
- [ ] Confirm all application-owned copy introduced in this slice has English
  and German keys
- [ ] Confirm no secret, real credential, or machine-specific URL was committed
- [ ] Confirm no speculative memoization or one-use generic abstraction was
  introduced
- [ ] Mark Slice 1 complete in [Progress](#progress)

**Suggested commit:** `feat(web): establish localized application foundation`

---

## Slice 2 — Search, page, and create Books

### Verifiable outcome

`/books` displays the API-ordered Books in a responsive, numbered table. Search
and pagination are shareable through the URL, each row links to stable details,
and a localized button opens a Formik/Yup modal that creates a Book and
navigates to its detail route.

### Requirements

`WEB-001`, `WEB-002`, `WEB-003`, `WEB-004`, `WEB-005`, `WEB-006`, `WEB-017`,
`WEB-018`

### Entry blockers

- **Blocked by:** Slice 1
- **Blocked by:** A reachable API implementing `GET /api/books` and
  `POST /api/books` for manual verification

### Implementation checklist

#### Typed Book API and server state

- [ ] Add the shared `Book` entity interface in `src/types/book.ts`
- [ ] Add `api/books/get-books.ts` with the exact `search`, `page`, and
  `pageSize` inputs and paged output from OpenAPI
- [ ] Add `api/books/create-book.ts` with the complete create input, returned
  Book, and response ETag
- [ ] Treat a missing ETag on a successful create as an integration failure
- [ ] Define explicit Book query keys whose list key includes normalized search,
  page, and page size
- [ ] Add focused query and mutation hooks that call typed API functions and do
  not expose Axios to the page

#### URL-backed search and pagination

- [ ] Parse `search`, `page`, and `pageSize` from URL search params with explicit
  defaults of page 1 and page size 20
- [ ] Reject or visibly recover from non-integer, out-of-range, duplicate, or
  otherwise invalid paging params without issuing a misleading request
- [ ] Build a labeled search form that submits the current text to the URL
- [ ] Reset `page` to 1 whenever search or page size changes
- [ ] Preserve search and page size while changing pages
- [ ] Use the API's `page`, `pageSize`, `totalCount`, and `totalPages` response
  rather than deriving totals from the current items
- [ ] Keep pagination keyboard accessible and disable impossible previous/next
  actions

#### Books table

- [ ] Render title, Author Names, Publish Date, and an explicit localized detail
  action for every result
- [ ] Link rows or their detail actions to `/books/{id}` with React Router APIs
- [ ] Preserve the API order exactly; do not sort again in the browser
- [ ] Localize labels and format Publish Date for the selected locale without
  translating the underlying value
- [ ] Provide distinct loading, no-results, invalid-query, API-validation, and
  unexpected-error presentations
- [ ] Make wide table content horizontally usable or adapt it for narrow screens
  without hiding the detail action

#### Create Book modal

- [ ] Add Formik and Yup with `pnpm`, and add the shadcn/ui dialog, input, label,
  textarea, table, and pagination primitives used by this slice
- [ ] Place a localized Add Book button with the table page actions
- [ ] Open an accessible shadcn/ui dialog without changing routes
- [ ] Build the form with Formik and a Yup schema for Title, Short Description,
  Publish Date, and at least one nonblank Author Name
- [ ] Use a Formik `FieldArray` or an equally explicit Formik-backed control to
  add and remove Author Name inputs
- [ ] Match the client validation boundaries in the API contract where those
  boundaries are known, while keeping the server authoritative
- [ ] Map API validation keys to the corresponding Formik fields and show
  localized feedback beside them; show unmatched errors in a localized summary
  without exposing raw server English as the only explanation
- [ ] Prevent duplicate submissions while the mutation is pending
- [ ] Preserve entered values after validation or recoverable network failures
- [ ] On success, cache the returned Book together with its opaque ETag,
  invalidate affected list queries, close the modal, and navigate to
  `/books/{id}`
- [ ] Reset the form only after successful creation or an explicit discard

#### Tests

- [ ] Test default and non-default URL query parsing
- [ ] Test that search and page-size changes reset the page
- [ ] Test that page navigation preserves the other URL state
- [ ] Test loading, results, empty, invalid-query, API-validation, and
  unexpected-error states
- [ ] Test that row order matches the API response and detail links are stable
- [ ] Test opening and dismissing the create modal with keyboard interaction
- [ ] Test Yup validation, dynamic Author Names, server field errors, and draft
  preservation
- [ ] Test successful creation, ETag caching, list invalidation, modal closure,
  and detail navigation
- [ ] Test missing-success-ETag handling as an integration failure
- [ ] Run representative behavior tests once in English and once in German

### Acceptance criteria

- [ ] `pnpm lint`, `pnpm test -- --run`, and `pnpm build` succeed
- [ ] `/books` requests and renders the API's default first page
- [ ] A user can search title, Short Description, or Author Name through the one
  API-supported search input
- [ ] `search`, `page`, and `pageSize` are shareable and restorable from the URL
- [ ] Table rows show Title, Author Names, Publish Date, and a detail action
- [ ] Pagination agrees with API totals and is usable by keyboard
- [ ] Add Book opens a modal on `/books`; no `/books/new` route exists
- [ ] The Formik/Yup form submits every editable field and displays client and
  server validation beside matching controls
- [ ] A successful create navigates to the created Book with its ETag already
  available to the detail cache
- [ ] Book content remains byte-for-byte untranslated when the language changes
- [ ] The table, modal, and all their states work at mobile and desktop widths

### Manual verification

- [ ] Run the API and frontend, then search by title, Short Description, and
  Author Name
- [ ] Move between pages, change page size, reload, and use browser back/forward
- [ ] Open and close the modal with mouse, Escape, and keyboard focus traversal
- [ ] Submit invalid data and compare displayed field errors with the API
- [ ] Create a valid Book and confirm navigation to its stable detail URL
- [ ] Repeat the main flow in English and German

### Pre-commit review

- [ ] Compare request and response types with
  [`../openapi/v1.json`](../openapi/v1.json)
- [ ] Confirm URL state is not duplicated as competing component state
- [ ] Confirm the table does not reorder or translate Book data
- [ ] Confirm the modal is accessible and no create page or route was added
- [ ] Confirm every new interface string exists in both languages
- [ ] Mark Slice 2 complete in [Progress](#progress)

**Suggested commit:** `feat(web): browse and create books`

---

## Slice 3 — View and safely edit a Book

### Verifiable outcome

`/books/:bookId` displays the complete current Book and lets the user enter an
inline Formik/Yup edit mode. Updates replace every editable field with the exact
ETag from the loaded representation, and stale edits remain intact until the
user explicitly chooses how to recover.

### Requirements

`WEB-007`, `WEB-008`, `WEB-009`, `WEB-010`, `WEB-011`, `WEB-017`, `WEB-018`

### Entry blockers

- **Blocked by:** Slice 2
- **Blocked by:** A reachable API implementing `GET /api/books/{id}` and
  `PUT /api/books/{id}` for manual verification

### Implementation checklist

#### Typed detail and update API

- [ ] Add `api/books/get-book.ts` with the returned Book and opaque response ETag
- [ ] Add `api/books/update-book.ts` with the complete replacement input,
  required current ETag, returned Book, and replacement ETag
- [ ] Send the ETag unchanged in `If-Match`; never parse, construct, weaken, or
  quote it in UI code
- [ ] Distinguish `400`, `404`, `412`, `428`, missing-success-ETag, and
  unexpected failures through typed API results or normalized errors
- [ ] Add a detail query key by Book ID and focused detail/update hooks

#### Current Book details

- [ ] Render Title, Short Description, Publish Date, and all Author Names in one
  clear current-information section
- [ ] Keep the future history region below the current-information section
- [ ] Add localized navigation back to the Books table
- [ ] Show distinct loading, invalid-ID, not-found, and unexpected-error states
- [ ] Format dates for the active locale without mutating or translating Book
  values

#### Inline edit flow

- [ ] Add an explicit localized Edit action that switches the details section
  into inline edit mode without navigating
- [ ] Initialize one Formik form from one successful Book response
- [ ] Use the same Yup rules and Author Name interaction as the create form
  without forcing an abstraction that obscures Formik state or ETag ownership
- [ ] Keep the exact response ETag paired with the values used to initialize the
  draft
- [ ] Send every editable field on update, including unchanged values
- [ ] Map API field errors to Formik fields and preserve the complete draft
- [ ] Disable duplicate submissions and provide localized pending feedback
- [ ] On success, replace the detail cache with the returned Book and ETag,
  invalidate affected Book list queries, leave edit mode, and keep the user on
  the detail route
- [ ] Allow canceling edit mode with an explicit warning or confirmation when
  doing so would discard a dirty draft

#### Concurrency and integration failures

- [ ] On `412 Precondition Failed`, keep every draft value and explain that a
  newer representation exists
- [ ] Provide an explicit Reload Current Book action that discards the stale
  baseline only after the user's confirmation when the draft is dirty
- [ ] Never automatically retry a stale update with a newer ETag
- [ ] Treat `428 Precondition Required` as an integration failure, not a field
  validation error
- [ ] Treat a successful GET or PUT without an ETag as an integration failure
  and do not enable unsafe editing
- [ ] If the Book disappears, render the localized not-found state without
  showing stale current information as authoritative

#### Tests

- [ ] Test loading, current details, invalid ID, not-found, and unexpected error
- [ ] Test entering and canceling inline edit mode
- [ ] Test that the Formik form starts from exactly one Book/ETag pair
- [ ] Test complete replacement input and unchanged opaque `If-Match`
- [ ] Test client validation and mapped/unmatched server validation errors
- [ ] Test a successful update replaces the Book and ETag caches and invalidates
  list queries
- [ ] Test `412` draft preservation, explicit reload, and absence of automatic
  stale resubmission
- [ ] Test `428` and missing GET/PUT ETags as integration failures
- [ ] Run the edit and concurrency feedback behaviors in English and German

### Acceptance criteria

- [ ] `pnpm lint`, `pnpm test -- --run`, and `pnpm build` succeed
- [ ] A valid detail route displays the complete current Book
- [ ] Editing occurs on `/books/:bookId`; no edit page or route exists
- [ ] Both create and edit use Formik and Yup
- [ ] PUT sends every editable field and the exact loaded ETag in `If-Match`
- [ ] A successful update immediately displays the returned Book and retains its
  new ETag for the next edit
- [ ] A `412` keeps the draft visible and requires an explicit reload choice
- [ ] `404`, `428`, missing ETag, validation, and unexpected failures remain
  distinguishable
- [ ] Language switching translates controls and feedback but never Book values
- [ ] The current-information and inline-form layouts work by keyboard and at
  mobile and desktop widths

### Manual verification

- [ ] Open a Book directly by URL and through the Books table
- [ ] Edit every field, including adding and removing Author Names
- [ ] Cancel a dirty edit and confirm that discarding is explicit
- [ ] Complete a valid edit and confirm the updated table state after navigating
  back
- [ ] Produce a stale ETag with two browser tabs and confirm `412` preserves the
  losing draft
- [ ] Switch languages while viewing and editing without changing field values

### Pre-commit review

- [ ] Confirm only the API layer reads or sends ETag headers
- [ ] Confirm no stale mutation can retry with a refreshed ETag
- [ ] Confirm the detail query does not fetch Book History
- [ ] Confirm no route-only components were moved into shared folders without
  actual reuse
- [ ] Confirm every new interface string exists in both languages
- [ ] Mark Slice 3 complete in [Progress](#progress)

**Suggested commit:** `feat(web): view and safely edit books`

---

## Slice 4 — Browse Book History and verify the delivery

### Verifiable outcome

The detail page loads Book History independently below the current information.
Each complete Change Set is one localized vertical-timeline item containing all
of its Book Changes. URL-backed filters restart the cursor chain, and a Load
More button appends additional items until the API reports no more history. The
complete delivery is behavior-tested, accessible, responsive, and documented.

### Requirements

`WEB-012`, `WEB-013`, `WEB-014`, `WEB-015`, `WEB-016`, `WEB-017`, `WEB-018`,
plus final cross-feature verification

### Entry blockers

- **Blocked by:** Slice 3
- **Blocked by:** A reachable API implementing
  `GET /api/books/{id}/history` for manual verification
- **Blocked by:** Seed data with multiple Change Sets and at least one Change
  Set containing multiple Book Changes

### Implementation checklist

#### Typed History API and cursor state

- [ ] Add `api/books/get-book-history.ts` with `changedField`, `changedFrom`,
  `changedBefore`, `sortDirection`, `limit`, and opaque `after` inputs
- [ ] Model each API history item as one Change Set containing its complete
  `changes` array
- [ ] Add a history query key containing Book ID and every normalized
  non-cursor filter
- [ ] Implement `useInfiniteQuery` with only the API's opaque `nextCursor` as
  the next `after` value
- [ ] Stop pagination when `hasMore` is false or `nextCursor` is null
- [ ] Never decode, edit, concatenate, or retain a cursor across changed filters

#### URL-backed history filters

- [ ] Add repeatable Changed Field filters for `title`, `shortDescription`,
  `publishDate`, and `authors`
- [ ] Add an inclusive Changed From input, exclusive Changed Before input, and
  ascending/descending chronological direction
- [ ] Store the normalized filter state in URL search params without colliding
  with Books-table params
- [ ] Convert local date/time controls to valid UTC instants for the API and show
  clear localized validation for invalid or inverted ranges
- [ ] Start a new infinite-query chain when any filter changes
- [ ] Keep the page limit fixed unless a user-facing limit control is explicitly
  added; never expose the cursor in a control

#### Grouped vertical timeline

- [ ] Render one timeline item per Change Set, never one item per Book Change
- [ ] Use the Change Set timestamp and a localized Created or Updated title as
  the item heading
- [ ] Align a circular marker vertically with each item heading
- [ ] Draw a vertical line from each marker to the next marker and stop the line
  after the final rendered item
- [ ] Render every returned Book Change inside its parent item even when only
  one Changed Field matched the filter
- [ ] Use semantic list and heading structure so the visual line and markers are
  decorative rather than required to understand grouping
- [ ] Keep the timeline readable at narrow widths without separating changes
  from their Change Set

#### Localized change descriptions

- [ ] Map stable API Changed Field values to localized field labels
- [ ] Render null, string, date-string, and Author Name array old/new values
  explicitly
- [ ] Use locale-sensitive presentation for dates and timestamps while keeping
  the source values unchanged
- [ ] Show additions, removals, and replacements with localized connective copy
  that does not imply hidden changes
- [ ] Never pass Book values through translation lookup or machine translation
- [ ] Fall back safely for an unknown Changed Field or unexpected structured
  value while retaining enough raw information to avoid data loss

#### Load More and independent states

- [ ] Show a localized Load More button only when another cursor page exists
- [ ] Change the button to a disabled localized loading state while fetching
- [ ] Append the next response after existing complete Change Sets
- [ ] Prevent duplicate concurrent Load More requests
- [ ] Keep initial loading, empty history, initial error, next-page error, and
  end-of-history states distinct
- [ ] Allow retrying a failed next page without discarding already rendered
  history
- [ ] Keep current Book details usable when only the History request fails

#### Cross-feature tests and documentation

- [ ] Test filter parsing, URL serialization, invalid ranges, and cursor-chain
  reset
- [ ] Test that next pages receive the exact opaque cursor and append in order
- [ ] Test that one multi-field Change Set renders as one timeline item with all
  returned changes
- [ ] Test that a Changed Field filter never hides sibling changes in a returned
  Change Set
- [ ] Test null, string, date-string, Author Name array, and unknown-value
  descriptions in English and German
- [ ] Test Load More pending, success, terminal, duplicate-click, and retry
  behavior
- [ ] Test that a History failure does not replace usable current Book details
- [ ] Add an end-to-end smoke test only if the chosen local test stack can run it
  deterministically; otherwise record the full manual flow below
- [ ] Update `README.md` with install, environment, run, test, lint, build, and
  API prerequisite commands verified in this delivery
- [ ] Update architecture or feature documentation if implementation introduced
  a durable boundary not already documented

### Acceptance criteria

- [ ] `pnpm lint`, `pnpm test -- --run`, and `pnpm build` succeed
- [ ] History appears below current Book information on `/books/:bookId`
- [ ] One Change Set with several changed fields appears as one timeline item
- [ ] Each marker aligns with its heading and connects vertically to the next
  marker, with no trailing line after the last item
- [ ] Filters are shareable in the URL and changing any filter starts at the
  first cursor page
- [ ] Load More appends a page and disappears when no further cursor exists
- [ ] A next-page failure preserves earlier items and can be retried
- [ ] Current Book details remain usable if History alone fails
- [ ] All application-owned copy and generated descriptions work in English and
  German
- [ ] Book and history values are never translated
- [ ] The complete browse, create, view, edit, stale-update, filter, and Load
  More flows are keyboard accessible and responsive
- [ ] No authentication, deletion, author management, history editing, custom
  sorting, offline mutation, or realtime behavior was added

### Manual verification

- [ ] Run the API and frontend from clean dependency installs
- [ ] Complete browse, search, pagination, create, detail, edit, and stale-update
  flows
- [ ] Verify a multi-field update appears as one timeline item containing every
  change
- [ ] Exercise every history filter, reload the URL, and use browser back/forward
- [ ] Load at least two cursor pages and verify item order and terminal behavior
- [ ] Simulate a failed next page and confirm earlier history remains visible
- [ ] Complete the primary flow in English and German
- [ ] Navigate the complete interface by keyboard and inspect focus order
- [ ] Inspect all screens at approximately 375 px, 768 px, and 1280 px widths
- [ ] Check the browser console for uncaught errors and React warnings

### Pre-commit review

- [ ] Compare every History request and DTO with
  [`../openapi/v1.json`](../openapi/v1.json)
- [ ] Confirm Change Set grouping is preserved from API response to UI
- [ ] Confirm cursors stay opaque and are not stored as shareable filter state
- [ ] Confirm History errors cannot make current Book details unusable
- [ ] Confirm all interface copy has English and German translations and all
  Book/history values bypass translation lookup
- [ ] Review the complete frontend diff against
  [`../specs/01-initial-implementation.md`](../specs/01-initial-implementation.md)
- [ ] Confirm `AGENTS.md`, `CONTEXT.md`, `ARCHITECTURE.md`, and `README.md` remain
  accurate
- [ ] Mark Slice 4 complete in [Progress](#progress)

**Suggested commit:** `feat(web): add localized book history timeline`

---

## Final completion record

- [ ] All scope decisions are checked
- [ ] All four slices are checked in [Progress](#progress)
- [ ] Every requirement from `WEB-001` through `WEB-018` is covered by at least
  one passing automated test or recorded manual verification
- [ ] `pnpm lint`, `pnpm test -- --run`, and `pnpm build` pass from the final
  worktree
- [ ] The final implementation matches the checked-in OpenAPI contract
- [ ] The frontend contains no create or edit feature routes
- [ ] English and German cover all application-owned copy
- [ ] The Books table, creation modal, details, inline edit form, grouped
  timeline, and Load More flow have been verified together
- [ ] No file outside `library-web` was changed
