# Frontend Architecture

> Architecture of the implemented frontend.

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
    book-form.tsx         # shared Formik field layout and validation feedback
    ui/                   # local shadcn/ui primitives
  hooks/
    use-debounce.ts       # reusable delayed-value hook for reactive inputs
  pages/
    books/
      index.tsx           # Books route component implementation
      search-params.ts
      components/
        create-book-dialog.tsx
    book-details/
      index.tsx           # Book Details route component implementation
      history-search-params.ts
      components/
        edit-book-dialog.tsx
        history-section.tsx
    not-found/
      index.tsx           # Not Found route component implementation
  types/
    book.ts               # shared Book entity interfaces
  router.tsx              # complete route configuration
  main.tsx                # providers and application entry point
  index.css               # Tailwind import, theme variables, and global styles
```

Every page has its own folder and implements its route component directly in
`index.tsx`; the entry file must not re-export that implementation from another
component. Put auxiliary React components owned by that page in its
`components/` subfolder. Keep non-React page modules, such as search-param
parsing, in the page folder. Move a component to the shared `src/components/`
folder only when it is actually reused across pages and page-agnostic.

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
- Implement each route component in `pages/<page>/index.tsx`.
- Put auxiliary one-page React components in `pages/<page>/components`.
- Extract a component only when it makes a complex page easier to understand or
  when it is reused.

Within every React component, organize declarations in this order:

1. `useState` hooks.
2. Other hooks, followed by `useEffect` and other effect hooks.
3. Handler and helper functions.
4. Conditional returns.
5. The final render.

When a state initializer requires a value returned by another hook, keep that
dependency immediately before the state declaration, then continue with the
same order. Derived values may stay beside the hook or state they describe.

## Hooks

Put reusable React behavior in `hooks/`. `useDebounce` delays propagation of a
changing value; the Books search uses it to update URL-backed search state after
typing pauses, while immediate input text remains local to the page.

## Performance

Render code should remain direct and readable. Do not add `useMemo`,
`useCallback`, or `memo` as a precaution. Use them only when profiling shows a
meaningful expensive path or when a stable reference is required by a proven
integration. Record the reason beside non-obvious memoization.
