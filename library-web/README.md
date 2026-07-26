# Library Web

React and TypeScript frontend for browsing, creating, editing, and viewing the
history of Books. The repository is currently a Vite scaffold; the target
product and architecture are documented before feature implementation begins.

## Documentation

- [`AGENTS.md`](AGENTS.md) is the concise entry point for coding agents.
- [`CONTEXT.md`](CONTEXT.md) defines product purpose and domain language.
- [`docs/specs/01-initial-implementation.md`](docs/specs/01-initial-implementation.md)
  defines the first delivery.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) defines module boundaries and data flow.

Read only the documents relevant to the current change.

## Local development

Requirements: Node.js compatible with `package.json`, `pnpm`, and the API running
at `http://localhost:5168`.

```sh
pnpm install
pnpm dev
```

Before delivery:

```sh
pnpm lint
pnpm build
```

The frontend must read the API origin from `VITE_API_URL`; implementation should
provide a safe local example without committing secrets.
