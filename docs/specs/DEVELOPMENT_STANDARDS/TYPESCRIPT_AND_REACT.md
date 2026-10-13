# TypeScript & React Conventions

**Source:** HLD §4.1, §3.6, §7.1 · `docs/specs/design-system.md`

Stack: **React 19**, **TypeScript strict**, **Vite**, ESLint, Vitest.
Package: `@initiative-tracker/web` at `src/web/`.

Per HLD §4.1: TanStack Query for server state, Zustand for UI state, `dnd-kit`
for drag and drop, `@microsoft/signalr`, `idb` for IndexedDB, `react-hook-form`
+ `zod` for forms, `papaparse` + `exceljs` for client-side export.

## 1. Project layout

```
src/web/src/
├── app/            # router, providers, app shell
├── features/       # one folder per product feature — the default home for code
│   └── board/
│       ├── api/        # query/mutation hooks
│       ├── components/
│       ├── hooks/
│       └── model/      # local types, state machines
├── components/     # shared presentational components (design-system primitives)
├── lib/            # framework-agnostic utilities
└── styles/         # tokens, global CSS
```

- **Feature-first.** New code goes in `features/`. It graduates to
  `components/` or `lib/` only when a **second** feature genuinely needs it.
- A feature **MUST NOT** import from another feature's internals. Share through
  `components/`, `lib/`, or `packages/contracts`.
- Absolute imports from `src/` (`@/features/board/...`). No `../../../`.

## 2. Naming

| Element | Convention |
|---|---|
| Component files and components | `PascalCase` — `TaskCard.tsx` |
| Hooks | `useCamelCase` — `useBoardQuery.ts` |
| Other files | `camelCase.ts` |
| Types and interfaces | `PascalCase`, no `I` prefix |
| Constants | `UPPER_SNAKE_CASE` |
| Booleans | `is` / `has` / `can` prefix |

One component per file. Co-locate a component's styles and sub-components with it.

## 3. TypeScript

- `strict: true`. **MUST NOT** be relaxed.
- **MUST NOT** use `any`. Use `unknown` and narrow. `@ts-ignore` is forbidden;
  `@ts-expect-error` is permitted only with a comment explaining why.
- Prefer `type` aliases; use `interface` for extensible public contracts.
- Prefer discriminated unions over optional-field soups — especially for the
  sync-state model (§6).
- Non-null assertion (`!`) requires a justifying comment.
- Derive types from `zod` schemas (`z.infer`) rather than declaring both.

## 4. Components

- **Function components only.** No class components.
- Props are an explicit, exported type. **MUST NOT** spread unknown props into
  DOM elements.
- Keep components presentational where possible; push data fetching into hooks
  under `features/*/api/`.
- Extract a custom hook when a component exceeds ~150 lines or holds more than
  two pieces of related state.
- Lists **MUST** use stable entity ids as `key` — never the array index. Board
  reordering makes index keys actively wrong.
- Conditional rendering: early return over deeply nested ternaries.

## 5. The contracts package

`packages/contracts` is the **single source of truth** for API DTOs and SignalR
event envelopes.

- TypeScript is the source; the C# side is mirrored and held by a contract test.
- The SPA **MUST NOT** hand-declare a type that already exists in `contracts`.
- Changing a contract is an **atomic monorepo commit** touching both sides —
  this is the main reason the monorepo exists (HLD §7.1).

## 6. State ownership

| State | Owner |
|---|---|
| Server data, caching, optimistic updates | **TanStack Query** |
| UI state — viewport, drag, panels, modals | **Zustand** |
| Ephemeral, single-component state | `useState` |
| Form state | `react-hook-form` |
| Offline cache + pending mutation queue | **IndexedDB** via `idb` |

- **MUST NOT** duplicate server data into Zustand. A second copy of the truth is
  a desynchronisation bug waiting to happen.
- Mutations apply **optimistically**; local interaction must never block on the
  network (HLD §1.6 — `<100 ms`).
- New entities created offline get a **client-generated UUIDv7** so they have
  permanent identity immediately. **MUST NOT** use temporary ids with later
  remapping.

### 6.1 Sync state is a first-class model

The sync state set is a **discriminated union**, not scattered booleans:

```
synced | syncing | offline(lastSyncedAt) | pending(n) | failed(n) | reconciled(n) | reconnecting
```

These are **first-class UI components**, not error toasts (HLD §3.6,
design-system §5.5). Every state must be representable and renderable.

## 7. Authorization in the UI

The SPA renders affordances based on role. It **MUST NOT** treat any client-side
check as a security boundary — the server decides, always.

- Membership **MAY** be cached as a rendering hint.
- An authorization **decision MUST NOT** be cached.
- A queued offline mutation rejected on reconnect **MUST** be handled
  gracefully, not as an unexpected error.
- The SPA holds only the Auth0 domain and **public** client id. No secret ever
  enters the bundle.

## 8. Styling and design system

- Design tokens from `docs/specs/design-system.md` are the **only** source of
  colour, spacing, type scale, radii and elevation. **MUST NOT** hard-code a hex
  value, a px spacing outside the scale, or an ad hoc shadow.
- Semantic tokens (§2.2 of the design system) over primitives — both themes must
  work.
- **Accessibility is a hard requirement, not polish** (HLD §11.2, FR-6):
  - WCAG 2.1 AA contrast
  - Every drag-and-drop interaction has an equivalent **keyboard** path
    (`dnd-kit` keyboard sensor) — designed, not retrofitted
  - Visible focus states; semantic HTML before ARIA; ARIA only when semantics
    are insufficient
  - Respect `prefers-reduced-motion`

## 9. Performance

- Mobile is a **first-class target** (FR-17), not a scaled-down desktop.
- Optimise only on evidence. `memo` / `useMemo` / `useCallback` are not defaults.
- Virtualisation (`@tanstack/react-virtual`) is applied **only** if boards exceed
  ~300 visible cards (HLD TD-6) — not pre-emptively.
- Route-level code splitting; keep the initial bundle lean.

## 10. Review checklist

- [ ] No `any`, no `@ts-ignore`
- [ ] Feature code in `features/`; no cross-feature internal imports
- [ ] Shared API types come from `packages/contracts`
- [ ] No server data duplicated into Zustand
- [ ] List keys are entity ids, not indices
- [ ] Offline-created entities use client-generated UUIDv7
- [ ] Sync states modelled as a discriminated union and all states renderable
- [ ] No hard-coded colours, spacing or shadows — tokens only
- [ ] Every drag interaction has a keyboard equivalent
- [ ] No secrets in the bundle; no cached authorization decisions
