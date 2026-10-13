# Development Standards — Initiative Tracker

**Status:** Active · **Owner:** Tech Lead · **Created:** 2026-10-13 · **Source:** HLD TD-11

These are the project's binding development conventions. They exist because
`board-data-model` was setting API, EF Core and migration conventions by
precedent (see that spec's R-5 / T-5), and the cost of divergence rises steeply
once a second module ships endpoints.

## Scope

| Document | Covers |
|---|---|
| [AUTHORIZATION.md](./AUTHORIZATION.md) | **Hard requirement.** `BoardAccessContext`, the four enforcement layers, 404-not-403 rule. Read this first. |
| [API_DESIGN_CONVENTIONS.md](./API_DESIGN_CONVENTIONS.md) | Route shape, intent endpoints, JSON serialization, status codes, RFC 9457 error contract |
| [EF_CORE_AND_MIGRATIONS.md](./EF_CORE_AND_MIGRATIONS.md) | DbContext-per-module, composite FKs, enums, ordering keys, expand/contract migrations |
| [CSHARP_STYLE.md](./CSHARP_STYLE.md) | Backend naming, nullability, async, exceptions, DI, logging |
| [TYPESCRIPT_AND_REACT.md](./TYPESCRIPT_AND_REACT.md) | Frontend structure, state ownership, component patterns, contracts package |

## Requirement levels

- **MUST** — a hard requirement. A violation blocks code review approval.
- **SHOULD** — the default. Deviating requires a one-line rationale in the PR.
- **MAY** — discretionary.

## How these are enforced

1. **Spec review (`/tl`)** — the Technical Approach must reference the relevant
   standards rather than restating or re-deciding them.
2. **Code review (`/review`)** — the reviewer cross-references against these
   documents and cites the specific clause when flagging a violation.
3. Violations are graded **Critical** (MUST — blocks), **Important** (SHOULD —
   fix before merge), **Minor** (style — author's discretion).

## What is deliberately NOT here

**Testing standards.** Test design, coverage expectations and test strategy are
owned exclusively by the tester via `/write-tests`. Documenting them here would
unduly constrain the tester's independent judgement. This file records only that
the decision is deliberate.

## Repository layout (actual)

```
project-tracker/
├── src/
│   ├── api/                   # ASP.NET Core 8, ProjectTracker.Api
│   │   ├── Modules/           # IdentityAccess, Board, Collaboration, Activity, Integrations
│   │   └── Shared/            # shared kernel (UuidV7, primitives)
│   └── web/                   # React 19 + Vite SPA (@initiative-tracker/web)
├── packages/
│   └── contracts/             # shared DTO + event envelope types
├── tests/
└── docs/specs/
```

> ⚠️ The HLD §7.1 sketch shows `apps/web` and `apps/api`. The repository actually
> uses `src/`. **`src/` is correct**; the HLD should be amended, not the code.

## Amending these standards

Standards change through `/tl` with a stated rationale, not ad hoc in a PR. If a
feature needs to deviate, raise it in that feature's Technical Approach and
record the deviation here if it becomes the new default.
