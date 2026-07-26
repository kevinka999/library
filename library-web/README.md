# Library Web

React and TypeScript frontend for browsing, creating, editing, and viewing the
history of Books. The interface is available in English and German.

## Documentation

- [`AGENTS.md`](AGENTS.md) is the concise entry point for coding agents.
- [`CONTEXT.md`](CONTEXT.md) defines product purpose and domain language.
- [`docs/specs/01-initial-implementation.md`](docs/specs/01-initial-implementation.md)
  defines the first delivery.
- [`docs/plans/01-initial-implementation-plan.md`](docs/plans/01-initial-implementation-plan.md)
  breaks the first delivery into reviewable implementation slices.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) defines module boundaries and data flow.

Read only the documents relevant to the current change.

## Local development

Requirements:

- Node.js compatible with `package.json`
- `pnpm`
- the Library API running at `http://localhost:5168`

```sh
pnpm install
cp .env.example .env.local
pnpm dev
```

`VITE_API_URL` is a public API origin, not a place for secrets. The example
already targets the default local API.

The development server prints its local browser URL, normally
`http://localhost:5173`. The application redirects `/` to `/books`.

## Verification

Before delivery, run the repository checks:

```sh
pnpm lint
pnpm build
```

Manual browse, create, edit, optimistic-concurrency, and history checks require
the API and appropriate seed data.
