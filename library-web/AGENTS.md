# Library Web

React frontend for browsing, creating, editing, and viewing the history of Books

## Load context progressively

Read this file first, then only the document needed by the task:

- Product purpose or domain language: [`CONTEXT.md`](CONTEXT.md)
- Feature behavior and acceptance criteria:
  [`docs/specs/01-initial-implementation.md`](docs/specs/01-initial-implementation.md)
- Initial feature implementation sequence:
  [`docs/plans/01-initial-implementation-plan.md`](docs/plans/01-initial-implementation-plan.md)
- Boundaries, folders, routing, or state:
  [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)

Do not load every document by default. The research notes under `docs/research/`
explain origins; they are not implementation rules.

## Non-negotiable rules

- Work only in `library-web` unless a coordinated API change is explicitly in scope.
- Use `pnpm` and the versions declared in `package.json`.
- Organize each screen under `pages/<page>/`. Implement the route component
  directly in `index.tsx`; do not re-export it from another page component.
  Put auxiliary page-specific React components in
  `pages/<page>/components/`. Move a component to shared `components/` only when
  it is reused across pages.
- Inside every React component, keep declarations in this order: `useState`
  hooks first; remaining hooks followed by effects; handler/helper functions;
  conditional returns; final render. A state initializer that depends on an
  earlier hook may follow that dependency.
- Build basic UI from shadcn/ui and Tailwind. Define Tailwind theme variables in
  `src/index.css` with top-level `@theme`; use `:root` only for regular CSS
  variables that should not create utilities.
- Keep server state in TanStack Query. Keep shareable navigation state in the URL
  and ephemeral interaction state locally.
- Only the API layer may import Axios or know endpoint paths. Keep each operation
  and its DTOs in one file, such as `api/books/get-book.ts`; query hooks call
  that typed function, never `axios.get(...)`.
- Preserve response ETags as opaque values and send the current value in
  `If-Match` on every update.
- Declare the complete route tree in `src/router.tsx`. Keep `router.tsx`,
  `main.tsx`, and global styles at the `src` root; do not add an `app/` layer.
- Do not add `useMemo` or `useCallback` by default. Add memoization only for a
  measured expensive calculation or a proven identity requirement.
- Prefer a small explicit module over a generic abstraction created for one use.
