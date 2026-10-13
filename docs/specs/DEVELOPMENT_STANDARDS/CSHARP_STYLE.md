# C# / Backend Style

**Source:** HLD §4.2, §7.3 · `board-data-model` TD-D, TD-H, R-8

Target: `net8.0`, `Nullable=enable`, `ImplicitUsings=enable`,
root namespace `ProjectTracker.Api`.

## 1. Project layout

```
src/api/
├── Program.cs
├── Shared/                       # shared kernel — no module may be referenced from here
└── Modules/
    └── {Module}/
        ├── Domain/               # entities, enums, invariants, domain exceptions
        ├── Persistence/          # DbContext, Configurations/, Migrations/, interceptors
        ├── Services/             # named transactional operations
        ├── Endpoints/            # minimal-API groups + Dtos/
        ├── Access/               # authorization boundary types
        └── ModuleMarker.cs       # the module's DI registration entry point
```

- Namespaces mirror folders: `ProjectTracker.Api.Modules.Board.Domain`.
- A module is entered through `ModuleMarker` for DI and through its published
  interfaces for everything else. **MUST NOT** reach into another module's
  `Domain/` or `Persistence/`.
- `Shared/` holds genuinely universal primitives only (e.g. `UuidV7`). If it is
  used by one module, it belongs in that module.

## 2. Naming

| Element | Convention |
|---|---|
| Types, methods, properties, constants | `PascalCase` |
| Parameters, locals | `camelCase` |
| Private fields | `_camelCase` |
| Interfaces | `IPascalCase` |
| Async methods | `...Async` suffix |
| Files | one top-level type per file, named for the type |

Avoid abbreviations except universally understood ones (`Id`, `Dto`, `Db`).

> Where a domain name collides with a BCL type, suffix for clarity rather than
> relying on namespace disambiguation — e.g. `TaskItem`, not `Task`.

## 3. Nullability

- `<Nullable>enable</Nullable>` is on. Nullable warnings **MUST NOT** be
  suppressed project-wide.
- `!` (null-forgiving) is a **last resort** and requires a comment explaining why
  the compiler is wrong.
- Guard public entry points explicitly; do not rely on the annotation alone for
  data crossing a trust boundary.

## 4. Types

- Entities are classes with private setters where practical; invariants are
  maintained through methods, not public mutable state.
- DTOs and value objects are `record` / `readonly record struct`.
- `sealed` by default. Open a type for inheritance only deliberately.
- Prefer composition and small interfaces over inheritance hierarchies.

## 5. Async

- Async all the way down. **MUST NOT** use `.Result`, `.Wait()`, or
  `.GetAwaiter().GetResult()`.
- Every async method that performs I/O **MUST** accept and propagate a
  `CancellationToken`.
- `ValueTask` only where profiling justifies it.

## 6. Services and transactions

- A state machine or multi-step structural operation **MUST** live in a **named
  service with a single transactional entry point**, never inline in an endpoint
  (`board-data-model` R-8 — this is where subtle bugs live).
- Core logic **SHOULD** be expressed as pure operations over in-memory
  collections, wrapped by a thin persistence-aware caller that loads rows,
  invokes the operation, and persists the result. This keeps the hard logic fast
  to test and independent of EF.
- Endpoints are thin: bind → authorize → delegate → map to DTO.

## 7. Errors and exceptions

- Domain-rule violations throw a **single, specific domain exception type per
  problem class** (e.g. `BoardStructuralViolationException`), thrown from
  domain/service code — **not** constructed at the endpoint layer.
- A central exception handler maps domain exceptions to RFC 9457
  `ProblemDetails` with the registered `type` URI. Endpoints **MUST NOT**
  hand-roll error responses.
- **MUST NOT** use exceptions for control flow in expected paths.
- **MUST NOT** catch and swallow. Catch only to add context or to translate, and
  always rethrow or convert deliberately.

## 8. Dependency injection

- Constructor injection only. No service locator, no `IServiceProvider` passed
  around.
- Register in the owning module's `ModuleMarker`, not in `Program.cs`.
- Default lifetime is **scoped**. Singletons must be genuinely stateless or
  thread-safe. **MUST NOT** capture a scoped dependency in a singleton.
- Any dependency that is unsafe outside Development **MUST** fail fast at
  registration — see [AUTHORIZATION.md §7](./AUTHORIZATION.md).

## 9. Logging

- `ILogger<T>`, structured, with named properties — not string interpolation:
  ```csharp
  logger.LogInformation("Initiative {InitiativeId} moved to column {ColumnId}", id, columnId);
  ```
- **MUST NOT log request bodies** — they contain user board content (HLD §7.3).
- **MUST NOT** log tokens, secrets, connection strings, or email addresses.
- Correlation ids on every log line.
- Levels: `Error` = needs a human; `Warning` = degraded or suspicious;
  `Information` = significant state change; `Debug` = development only.

## 10. Configuration and secrets

- Strongly-typed options classes bound from configuration, validated at startup
  with `ValidateOnStart()`.
- Secrets come from Railway environment variables. **MUST NOT** be committed, and
  **MUST NOT** appear in `appsettings.json`.
- A missing required secret **MUST** fail startup loudly, never default silently.

## 11. Review checklist

- [ ] No cross-module reach into `Domain/` or `Persistence/`
- [ ] State machines in named services with one transactional entry point
- [ ] Endpoints are thin; no business logic in the endpoint layer
- [ ] `CancellationToken` accepted and propagated on I/O
- [ ] No `.Result` / `.Wait()`; no swallowed exceptions
- [ ] Domain exceptions thrown from services, mapped centrally
- [ ] Structured logging with no request bodies, secrets, or PII
- [ ] No nullable suppression without justification
- [ ] DI registered in `ModuleMarker` with correct lifetimes
