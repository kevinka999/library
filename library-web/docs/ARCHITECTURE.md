# Frontend Architecture

> Target architecture. The current application is still the Vite scaffold.

## Structure

Keep the structure flat and add folders only when they contain real files:

```text
src/
  api/
    client.ts             # Axios configuration and shared HTTP errors
    books/
      get-books.ts        # GET /api/books and its query/output DTOs
      get-book.ts         # GET /api/books/:id and its output DTO
      create-book.ts      # POST /api/books and its input/output DTOs
      update-book.ts      # PUT /api/books/:id and its input/output DTOs
      get-book-history.ts # GET /api/books/:id/history and its query/output DTOs
  components/
    ui/                   # local shadcn/ui primitives
  pages/
    books/
      index.tsx
    book-details/
      index.tsx
  types/
    book.ts               # shared Book entity interfaces
  router.tsx              # complete route configuration
  main.tsx                # providers and application entry point
  index.css               # Tailwind import, theme variables, and global styles
```

Every page has its own folder and an `index.tsx`. A complex component used by
only one page stays inside that page folder. Move it to `components/` only when
it is actually reused and agnostic.

Keep shared entity interfaces, such as `Book`, in `types/`. Each API operation
has its own file. Define its function, query inputs, request DTO, and response DTO
together in that file so the complete contract is easy to find. Do not create a
shared DTO or contracts folder.

## Routing

Declare all routes in `src/router.tsx` with React Router DOM. This includes the
index redirect, page routes, and the not-found route. Pages own their loading,
not-found, and unexpected-error states. Use router APIs for links and navigation.

## Components

- Put shadcn/ui primitives in `components/ui`.
- Put other reused components directly in `components`.
- Keep one-page components in that page's folder.
- Extract a component only when it makes a complex page easier to understand or
  when it is reused.

## Performance

Render code should remain direct and readable. Do not add `useMemo`,
`useCallback`, or `memo` as a precaution. Use them only when profiling shows a
meaningful expensive path or when a stable reference is required by a proven
integration. Record the reason beside non-obvious memoization.
