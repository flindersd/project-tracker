# Board Data Model

**Product:** [../PRD.md](../PRD.md)

## 1. Business Spec

<!-- gate:ba status=COMPLETE ui_required=false at=2026-10-11T12:31:00Z -->

### Overview

This backend-only feature establishes the authoritative Board-module data model and CRUD capabilities for boards, mixed-type columns, task sub-stages, initiatives, tasks, assignees, and tags. It enables Board Administrators to establish valid board structure and authorized members to create and maintain work records through a future API, with all data persisted in PostgreSQL. Exact REST routes and DTO shapes are intentionally deferred to Technical Approach.

### Problem Being Solved

The product cannot safely represent or evolve its distinctive mixed-column initiative workflow without a stable, authorized, and integrity-preserving source of truth for board structure and work. Without this feature, board rendering, task management, collaboration, comments, and activity/notifications have no reliable entities, identities, ordering, or structural rules on which to operate.

### User Stories

- [ ] **[US-01]** As a Board Administrator, I want to create and maintain boards, ordered columns, and task sub-stages so that the board represents my team's workflow.
- [ ] **[US-02]** As a Board Administrator, I want to create, edit, reorder, and remove initiatives in valid columns so that work is organized by its lifecycle.
- [ ] **[US-03]** As a board member, I want to create and update tasks with their core fields, assignees, and tags so that initiative work is recorded consistently.
- [ ] **[US-04]** As a Board Administrator, I want structural changes to preserve work or be blocked when unsafe so that configuration changes never silently destroy task information.
- [ ] **[US-05]** As an authorized board member, I want all board-scoped changes to use stable identities and durable ordering so that dependent features can reliably load, reference, and position work.

### Acceptance Criteria

- [ ] Every persisted Board-module entity has an application-generated UUIDv7 identifier; IDs, rather than display names or row positions, are used for references.
- [ ] A board can contain an ordered set of columns of type `initiative_only` or `initiative_task`; only `initiative_task` columns can contain task sub-stages.
- [ ] An initiative can be created in either column type and can contain zero tasks.
- [ ] A task belongs to exactly one initiative and board, and has an active `taskSubStageId` only when its initiative is in an `initiative_task` column whose sub-stage is in that same column, except for a task retained in an inactive sub-stage under the carry-over rule.
- [ ] A Board Administrator can mark a task sub-stage inactive. An inactive sub-stage is a board configuration, not a per-task flag; it cannot receive directly created tasks, but may be used as a landing sub-stage for existing tasks.
- [ ] Moving an initiative to a different `initiative_task` column assigns each task not in an inactive sub-stage to the destination column's first ordered sub-stage; a move is rejected if that destination has no sub-stages. A task in an inactive sub-stage remains linked to the moving initiative and retains its active `taskSubStageId` in the original column; it is excluded from the move's carry-over/reassignment and may therefore have a sub-stage in a different column from its initiative.
- [ ] Moving an initiative to an `initiative_only` column preserves each task's prior active sub-stage in `dormantSubStageId` and sets its active `taskSubStageId` to null; returning it to an `initiative_task` column assigns every task to that destination's first ordered sub-stage.
- [ ] Changing an `initiative_task` column containing initiatives/tasks to `initiative_only` applies the dormancy rule; changing an `initiative_only` column to `initiative_task` requires at least one configured task sub-stage before initiatives can be moved into it.
- [ ] A task sub-stage cannot be deleted until it contains no tasks, and a column cannot be deleted until it contains no initiatives and no task sub-stages.
- [ ] Sort order uses lexicographic fractional-index keys. Insertions/reorders update only the affected ordering scope unless keys collide or have no representable value between neighbours, in which case the affected scope is synchronously rebalanced within the write transaction.
- [ ] Every board-scoped handler receives a `BoardAccessContext` resolved from `(principal, boardId)` before any Board-module data access; a development/test interface stub may mint the context until `server-rbac` supplies the production implementation.
- [ ] Unauthorized or inactive membership access does not expose board data; administrator-only structural operations require the Board Administrator role through the authorization boundary.

### Functional Requirements

- **FR-01:** The Board module shall persist Boards with UUIDv7 `id`, `name`, `createdBy`, `createdAt`, `updatedAt`, and soft-delete support.
- **FR-02:** The Board module shall persist ordered BoardColumns with UUIDv7 `id`, `boardId`, `name`, `sortOrder`, `columnType`, audit timestamps, and a type of `initiative_only` or `initiative_task`.
- **FR-03:** The Board module shall persist ordered TaskSubStages with UUIDv7 `id`, `boardId`, `parentColumnId`, `name`, `sortOrder`, and `isInactive` (default `false`), and shall reject creation or retention of a sub-stage whose parent column is not `initiative_task`.
- **FR-04:** The Board module shall persist Initiatives with UUIDv7 `id`, `boardId`, `columnId`, `title`, optional `description`, `sortOrder`, audit timestamps, and soft-delete support.
- **FR-05:** The Board module shall persist Tasks with UUIDv7 `id`, `boardId`, `initiativeId`, nullable active `taskSubStageId`, nullable `dormantSubStageId`, `title`, optional `description`, optional `dueDate`, `priority`, `sortOrder`, audit timestamps, soft-delete support, and `rowVersion` for the separately specified concurrency feature. No task-level inactive or carry-over-exclusion field shall be added; exclusion is derived from the referenced TaskSubStage's `isInactive` value.
- **FR-06:** Task priority shall be a fixed enum with only `Low`, `Medium`, `High`, and `Urgent` values.
- **FR-07:** The Board module shall persist TaskAssignee records (`taskId`, `userId`, `assignedAt`, `assignedBy`) and TaskTag records (`taskId`, `tagId`), allowing zero or more assignees and tags per task without duplicate join pairs.
- **FR-08:** The Board module shall persist board-scoped Tags with UUIDv7 `id`, `boardId`, `name`, and optional `color`.
- **FR-09:** The module shall provide authorized CRUD capabilities for boards, columns, task sub-stages, initiatives, tasks, task assignees, and tags; endpoint paths, request/response DTOs, and error envelope details are deferred to `/tl`.
- **FR-10:** Board Administrators shall be required for board-structure mutations (columns and sub-stages) and initiative mutations; task CRUD shall be available to active authorized board members subject to the `server-rbac` policy implementation.
- **FR-11:** The module shall enforce the task/initiative/sub-stage integrity and dormancy rules described in Acceptance Criteria atomically with the structural mutation.
- **FR-12:** Column and sub-stage deletion shall be rejected until their contained initiatives, sub-stages, or tasks, as applicable, have been manually moved or deleted.
- **FR-13:** The module shall use fractional-index string keys for ordering within each relevant board, column, sub-stage, or initiative scope and shall synchronously rebalance the affected scope inside the write transaction on collision or exhausted precision.
- **FR-14:** All Board-module repositories and handlers shall require a valid `BoardAccessContext` before reading or writing board-scoped data; no handler may make its own authorization decision.
- **FR-15:** EF Core schema migrations for this feature shall follow expand/contract and backward-compatible practices and run as an explicit, gated deployment step, never automatically at application startup.
- **FR-16:** Board Administrators shall be able to set `TaskSubStage.isInactive`. Tasks shall not be created directly in an inactive sub-stage. When an initiative moves to a different `initiative_task` column, tasks whose active sub-stage has `isInactive=true` shall retain that sub-stage and their initiative link, while all other tasks are reassigned under the destination first-sub-stage rule. This carry-over exclusion is separate from `dormantSubStageId`, applies only to that column-to-column move, and does not itself change task rendering, progress/completion counts, or other behavior. The module shall reject clearing `isInactive` while it would leave a retained task in a sub-stage outside its initiative's current column.

### Non-Functional Requirements

- **Data integrity:** PostgreSQL and EF Core shall preserve referential and board-boundary integrity. Every board-scoped table carries `boardId` directly and must be filtered to the authorized board context.
- **Security:** The Board module must not make authorization decisions. It requires a `BoardAccessContext` before data access; Identity & Access owns production membership and role resolution. Missing or inactive membership must not disclose board existence or content.
- **Reliability:** Structural changes and associated task transitions/dormancy updates are atomic. No destructive structural operation may silently discard work.
- **Performance:** The model must support the HLD MVP target of up to 30 initiatives, 2,000 tasks, 12 columns, and 8 sub-stages per column, with API reads p95 under 250 ms and writes p95 under 400 ms where infrastructure targets are met.
- **Compatibility:** Migrations must remain compatible with the previously deployed API version during deployment.

### Dependencies

Mark each dependency as one of:
- **Hard dependency** — cannot proceed without this. Development is blocked until it exists or is confirmed in scope.
- **Soft dependency** — should ideally exist first, but work can begin with a workaround or assumption.

| Dependency | Type | Notes |
|------------|------|-------|
| `server-rbac` / Identity & Access authorization service | Hard | Production Board handlers require resolved `BoardAccessContext` before data access and role checks. A narrowly scoped interface/test stub permits Board-module development and tests until the production service ships; it is not a production authorization substitute. |
| `platform-foundation` PostgreSQL, EF Core, and migration pipeline | Hard | Required to persist the relational schema and execute explicit gated migrations. The HLD defines PostgreSQL on Railway and the migration approach. |
| `concurrency-control` | Soft | This feature stores `rowVersion` and fractional ordering prerequisites; field-level merge, conflict responses, and broader OCC behavior are explicitly owned by that later feature. |
| `board-rendering`, `task-management`, `realtime-transport`, `comments`, and `activity-notifications` | Soft | These downstream features consume this model but do not block its delivery. |

### Out of Scope

- Any user-facing board, swimlane, sub-stage, responsive/mobile, drag-and-drop, or accessible interaction UI (`board-rendering` and stage-management).
- SignalR publication, real-time delivery, activity-event creation, notification fan-out, offline cache, reconciliation, and conflict UX.
- The field-level merge algorithm, `409 Conflict` contract, and full optimistic-concurrency behavior (`concurrency-control`), beyond persisting `rowVersion`.
- Authentication, BoardMember CRUD, role administration, and production authorization-service implementation (`server-rbac`).
- Comments, views, Sheets export/import/sync, attachments, and membership-management UI or API.
- Exact REST endpoint routes, DTO definitions, serialization, and error-envelope design, which are owned by `/tl`.

### Open Questions

| # | Question | Owner |
|---|----------|-------|
| 1 | When an initiative returns from an `initiative_only` column to an `initiative_task` column, should a future enhancement restore matching dormant sub-stages rather than applying the approved first-sub-stage reset rule? This remains separate from the resolved inactive-sub-stage carry-over exclusion; this feature uses the approved reset rule for all tasks returning from `initiative_only`. | Product owner — should resolve before any restoration enhancement |
| 2 | What maximum fractional-index key length should trigger proactive synchronous rebalance before precision is exhausted? | Tech Lead — should resolve during `/tl` |
| 3 | What task-edit permission nuances, if any, should `server-rbac` apply (for example, whether a User may edit any board task or only tasks they created/are assigned)? This spec assumes active Users may create and update tasks per PRD FR-5. | Product owner + Tech Lead — must resolve before `server-rbac` implementation |
| 4 | `isInactive` sub-stages are intended to represent terminal/archived states (for example, Done). Downstream features that compute active-work counts, progress indicators, or default views should exclude tasks whose active sub-stage has `isInactive=true`. This is out of scope for `board-data-model`, which has no counting or view logic, but should inform the design of `board-rendering`, `custom-board-views`, and future progress/dashboard features. | Product owner + future feature BAs |

## 2. UX Review

_Pending — run `/ux` if this feature has user-facing UI._

## 3. Technical Approach

<!-- gate:tl verdict=APPROVED at=2026-10-11T13:10:00Z -->

### Verdict

**APPROVED.** The Business Spec is technically sound, internally coherent once the three ambiguities resolved in §3.3 below are applied, and sits comfortably within the HLD's architecture and the product owner's approved cost envelope. Estimated effort **~2–2.5 weeks**, below the 5-week threshold. No splitting required. No new cost flags. Proceed to `/write-tests`.

Two conditions attach to this approval, neither of which blocks the start of development:

1. **`platform-foundation` has not shipped.** The API project is a bare skeleton (`Program.cs`, 21 lines, no EF Core, no Npgsql, no `DbContext`, no migrations directory, no CI migration gate). This feature therefore carries the Board-module persistence wiring itself (Chunk 1) and must be developed and tested against a Testcontainers PostgreSQL instance. **FR-15 (explicit, gated, expand/contract migration deployment) cannot be fully satisfied by this feature** — it describes a pipeline that `platform-foundation` owns. This feature satisfies the half it can: migrations are authored expand/contract-compatible and are *never* applied at application startup. See R-1.
2. **`server-rbac` has not shipped.** The `BoardAccessContext` capability token and its minting interface are defined here; the production implementation is not. The development/test stub is explicitly permitted by AC (line 37) and is gated per R-2.

---

### Recommended Architecture

A conventional EF Core 8 + Npgsql relational persistence layer inside the existing `Modules/Board/` boundary of the modular monolith. No new services, no new infrastructure, no deviation from the HLD. The three genuinely non-trivial pieces of engineering are (a) the board-boundary integrity guarantee, (b) fractional ordering with safe rebalance, and (c) the initiative-move / dormancy / carry-over state machine. Everything else is CRUD.

```
src/api/Modules/Board/
├── Domain/           entities, enums, invariants
├── Persistence/      BoardDbContext, entity configurations, migrations
├── Ordering/         fractional-index key generation + scope rebalance
├── Services/         InitiativeMoveService, ColumnTypeChangeService, SubStageService
├── Endpoints/        minimal-API endpoint groups + DTOs
└── Access/           BoardAccessContext, IBoardAuthorizationService (+ dev stub)
```

---

### Key Technical Decisions

**TD-A — One `BoardDbContext`, one `public` schema, cross-module FKs permitted at the database level.**
The HLD's module boundary (§4.2) is a *code* boundary, not a storage boundary. Board entities get a dedicated `BoardDbContext` with its own migrations history table (`__EFMigrationsHistory_Board`) so modules can migrate independently, but they live in the same database and the same `public` schema. FKs from `task_assignees.user_id` and `tasks.created_by` to `users(id)` are declared as **shadow FKs with no navigation property** into Identity & Access entities. Rationale: real referential integrity is worth more than schema purism, and a navigation-free FK preserves the code boundary that actually matters. Rejected: a separate `board` schema with no cross-schema FKs — it buys nothing at one deployable and silently permits orphaned assignee rows.

> ⚠️ Sequencing note: `users` is owned by `server-rbac`/Identity & Access and does not yet exist. Until it does, declare the user-referencing columns as plain `uuid` with **the FK authored but commented in the migration**, to be enabled by a follow-up expand migration once `users` lands. Do not block on it.

**TD-B — Board-boundary integrity is enforced structurally by composite foreign keys, not by application discipline.**
This is the single most important decision in the feature. Every board-scoped child declares its FK to its parent on `(board_id, parent_id)`, backed by a unique constraint `(board_id, id)` on each parent. Concretely:

| Child | Composite FK | References |
|---|---|---|
| `board_columns` | `(board_id)` | `boards(id)` |
| `task_sub_stages` | `(board_id, parent_column_id)` | `board_columns(board_id, id)` |
| `initiatives` | `(board_id, column_id)` | `board_columns(board_id, id)` |
| `tasks` | `(board_id, initiative_id)` | `initiatives(board_id, id)` |
| `tasks` | `(board_id, task_sub_stage_id)` | `task_sub_stages(board_id, id)` |
| `tasks` | `(board_id, dormant_sub_stage_id)` | `task_sub_stages(board_id, id)` `ON DELETE SET NULL` |
| `task_tags` | `(tag_id)` → `tags`, `(task_id)` → `tasks`, + CHECK via trigger-free composite | board-matched |

Effect: it becomes **physically impossible** to attach a task to an initiative on another board, or to a sub-stage on another board. This directly discharges the "Data integrity" NFR and HLD §2.4's deliberate `board_id` denormalisation, and it turns a whole class of authorization bug into a constraint violation. The cost is a handful of redundant unique indexes — trivially cheap, and they are useful covering indexes anyway.

**What composite FKs deliberately do NOT enforce:** that a task's sub-stage belongs to its initiative's *current column*. AC (line 32) explicitly permits a divergence under the inactive-sub-stage carry-over rule. A database trigger asserting column match would therefore be **wrong**. This invariant is an application-layer rule, enforced in the mutation services, and must be tested as such.

**TD-C — Fractional indexing: own code, base-62, rebalance threshold 48 characters. (Resolves BA Open Question 2.)**
`sort_order` is `varchar(64) NOT NULL`, ordered with the C collation (`COLLATE "C"`) to guarantee byte-lexicographic ordering independent of database locale — this is a real trap and must be set explicitly in the migration. Key generation is a ~100-LOC `LexoKey` utility: alphabet `0-9A-Za-z`, midpoint between two neighbours, `null` neighbour meaning open-ended.

- **Rebalance trigger: generated key length > 48 characters, or no representable midpoint between neighbours, or a unique-constraint collision on the ordering scope.** Rationale for 48: worst-case adversarial repeated insertion at the same position grows the key ~1 character per insert, so 48 permits 48 consecutive worst-case inserts before intervention; realistic ordering scopes here are bounded by the NFR at ≤2,000 tasks, and random-position insertion grows keys logarithmically. 48 leaves 16 characters of headroom inside the `varchar(64)` for the rebalance write itself. This is deliberately conservative — rebalance is cheap at this scale and an exhausted key is a correctness failure.
- **Rebalance is synchronous, inside the write transaction, scoped to the affected ordering scope only** (per FR-13). Implementation: `SELECT ... FOR UPDATE` over the scope ordered by `sort_order`, reassign evenly spaced keys, write, continue the original mutation. Taking the row locks in `sort_order` ascending order makes deadlock between two concurrent rebalances of the same scope impossible.
- **Ordering scopes** (each gets a unique index on scope + `sort_order`):

| Entity | Scope |
|---|---|
| `board_columns` | `(board_id)` |
| `task_sub_stages` | `(board_id, parent_column_id)` |
| `initiatives` | `(board_id, column_id)` |
| `tasks` | `(board_id, initiative_id, task_sub_stage_id)` — `NULL` sub-stage is its own scope (use `NULLS NOT DISTINCT` on the unique index, PG15+, so two nulls collide correctly) |

**TD-D — `BoardAccessContext` is a capability token with an internal constructor.**
```csharp
public sealed class BoardAccessContext
{
    internal BoardAccessContext(Guid boardId, Guid userId, BoardRole role) { ... }
    public Guid BoardId { get; }
    public Guid UserId { get; }
    public BoardRole Role { get; }
    public void RequireAdministrator(); // throws BoardForbiddenException
}
```
Every Board repository method and every mutation service takes a `BoardAccessContext` as its **first parameter**. No Board-module code can construct one — only `IBoardAuthorizationService.ResolveAsync(principal, boardId)` can. This makes "I forgot to authorize" a compile error rather than a code-review miss, exactly as HLD §4.2 requires. The dev/test stub implementation lives behind a configuration flag that **must fail fast at startup in any non-Development environment** (see R-2).

**TD-E — Board scoping is also enforced at the ORM layer as a net.**
`BoardDbContext` applies a global query filter on every `IBoardScoped` entity: `e => e.BoardId == _boardScope.Current && e.DeletedAt == null`, where `_boardScope` is a scoped accessor set from the resolved `BoardAccessContext`. This is HLD §2.4 layer 4. Soft-delete filtering rides the same filter. Note the known EF consequence: **required navigations to a filtered entity produce silently empty results**, so include/join behaviour across soft-deleted parents must be explicit.

**TD-F — API shape and error contract (FR-09 — owned here, as no `DEVELOPMENT_STANDARDS/` exists; see R-5).**
- Routes are **always board-scoped and board-prefixed**, so the authorization filter is uniform and `boardId` is never inferred from a child id:
  `/api/boards/{boardId}/columns`, `/api/boards/{boardId}/columns/{columnId}/sub-stages`, `/api/boards/{boardId}/initiatives`, `/api/boards/{boardId}/tasks`, `/api/boards/{boardId}/tags`, plus `/api/boards` for board CRUD.
- Moves and reorders are **explicit intent endpoints, not generic PATCHes**: `POST .../initiatives/{id}:move` with `{ columnId, afterId?, beforeId? }` and `POST .../tasks/{id}:move` with `{ initiativeId?, taskSubStageId?, afterId?, beforeId? }`. Rationale: the move operation triggers the entire dormancy/carry-over state machine; expressing it as a field patch would hide that and would make the `concurrency-control` field-level merge semantics incoherent for position changes. **Clients send neighbour ids, never `sortOrder` values** — the server owns key generation.
- JSON is **camelCase** (System.Text.Json default). Enums serialise as **strings**.
- Errors are **RFC 9457 `ProblemDetails`** with a stable `type` URI.
- **Status code allocation (important):** `404` for non-member or inactive-member access and for any board-scoped id not visible in scope — never `403`, never a discriminating message (NFR Security). `422 Unprocessable Entity` with `type: /problems/board-structure-violation` for *all* structural-rule rejections (FR-11/FR-12/FR-16: deleting a non-empty column or sub-stage, moving to a column with no sub-stages, creating a task in an inactive sub-stage, clearing `isInactive` that would strand a retained task). **`409 Conflict` is reserved exclusively for optimistic-concurrency failures and must not be used by this feature** — it belongs to `concurrency-control`, and overloading it now would force a breaking change later.

**TD-G — Enums as constrained strings, not integers.** `column_type` and `priority` are `varchar` with EF value conversions and database `CHECK` constraints. Rationale: readable in the database during incident response, no ordinal-renumbering hazard across migrations, and the `CHECK` constraint gives FR-06's "fixed enum with only Low/Medium/High/Urgent" teeth at the storage layer.

**TD-H — UUIDv7 is generated in application code, in a shared helper, not by the database.**
`.NET 8` has no `Guid.CreateVersion7()` (added in .NET 9). **Recommendation: implement a ~30-LOC `UuidV7.NewGuid()` in a shared kernel** rather than taking a NuGet dependency or performing a target-framework upgrade in the middle of this feature. Rationale: it is a small, well-specified, fully testable algorithm (48-bit Unix-ms timestamp + 74 random bits + version/variant nibbles), with the one real subtlety being .NET's little-endian `Guid` byte layout — write bytes explicitly, do not round-trip through `new Guid(byte[])` carelessly. Deliberately **no `DEFAULT` on the database column**: the HLD requires client-generated identity for optimistic/offline creation, so the application must always be the generator. Delete the helper and switch to the BCL method when the project moves to .NET 9+/.NET 10 LTS — flagged as a trivial follow-up, not scoped here.

**TD-I — `row_version` is stored and incremented, but no concurrency behaviour is implemented.**
`row_version bigint NOT NULL DEFAULT 1`, incremented by a `SaveChangesInterceptor` on every modification of `Task`. It is **not** configured as an EF concurrency token in this feature. `concurrency-control` owns the `WHERE row_version = @expected` check, the merge, and the 409 contract. Storing and maintaining it now means that feature is a behavioural change with no migration.

---

### §3.3 — Spec Ambiguities Resolved Here

Three points in the Business Spec are under-determined. None warrants returning the spec; all three are resolved below and must be treated as binding for `/write-tests`.

**(1) Column type change `initiative_task` → `initiative_only` vs. the FR-03 "no retention" rule vs. `dormantSubStageId` durability.**
AC (line 34) requires the dormancy rule on type change; FR-03 forbids *retaining* a sub-stage under a non-`initiative_task` column; FR-12 blocks sub-stage deletion while it holds tasks. Applied naively these conflict, and resolving them leaves `dormant_sub_stage_id` pointing at a deleted row.

> **Resolution.** The type change is a single atomic operation: (a) for every task under every initiative in that column, set `dormant_sub_stage_id = task_sub_stage_id` and `task_sub_stage_id = NULL`; (b) delete the column's now-empty sub-stages; (c) set `column_type = 'initiative_only'`. `dormant_sub_stage_id` is declared `ON DELETE SET NULL` and is **explicitly non-load-bearing** — AC (line 33) specifies that return to an `initiative_task` column uses the *first ordered sub-stage* reset rule, so no behaviour reads the dormant value. It is a forensic/audit field only.
>
> **Consequence flagged to the BA and PO:** if BA Open Question 1 is ever resolved in favour of *restoring* dormant sub-stages, an FK is the wrong representation — `dormant_sub_stage_id` must become a durable snapshot (sub-stage name + ordinal + column id), because the row it points at may no longer exist. That is a schema change in the enhancement's own feature, not a flaw here, but it should be recorded against OQ-1.

**(2) Does the inactive-sub-stage carry-over exclusion apply to a move into an `initiative_only` column?**
FR-16 scopes the exclusion to "a different `initiative_task` column" and says it "applies only to that column-to-column move".

> **Resolution: no.** On a move into an `initiative_only` column, **all** tasks go dormant, including those whose active sub-stage has `isInactive = true`. The carry-over exclusion applies exclusively to `initiative_task` → `initiative_task` moves. This also avoids the otherwise-unreachable state of a task retaining an active sub-stage while its initiative sits in a column that has no sub-stages at all.

**(3) Column type change `initiative_only` → `initiative_task` with initiatives already present.**
AC (line 34) requires "at least one configured task sub-stage before initiatives can be moved into it", which addresses moves but not the type change itself.

> **Resolution.** The type change to `initiative_task` is permitted with zero sub-stages (an empty-but-correctly-typed column is a legitimate configuration step — sub-stages cannot be created until the column is `initiative_task`). Any **existing** initiatives in that column have their tasks left dormant (`task_sub_stage_id = NULL`) until the admin performs an explicit re-entry move, which then applies the first-ordered-sub-stage rule. Rationale: the alternative — blocking the type change until sub-stages exist — is a chicken-and-egg deadlock.

---

### Module / File Breakdown

| Path | Change | Notes |
|---|---|---|
| `src/api/ProjectTracker.Api.csproj` | modify | Add `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design` |
| `src/api/Program.cs` | modify | Register `BoardDbContext`, board scope accessor, auth stub guard, endpoint groups, ProblemDetails. **Must not call `Database.Migrate()`** (FR-15) |
| `src/api/Shared/UuidV7.cs` | new | TD-H |
| `src/api/Modules/Board/Access/BoardAccessContext.cs` | new | TD-D |
| `src/api/Modules/Board/Access/IBoardAuthorizationService.cs` + `StubBoardAuthorizationService.cs` | new | Stub is Development-only, fail-fast elsewhere |
| `src/api/Modules/Board/Access/BoardAuthorizationFilter.cs` | new | Endpoint filter resolving `{boardId}` → context; 404 on miss |
| `src/api/Modules/Board/Domain/*.cs` | new | `Board`, `BoardColumn`, `TaskSubStage`, `Initiative`, `Task`, `TaskAssignee`, `Tag`, `TaskTag`, `ColumnType`, `TaskPriority` |
| `src/api/Modules/Board/Persistence/BoardDbContext.cs` + `Configurations/*.cs` | new | TD-A/B/E/G |
| `src/api/Modules/Board/Persistence/RowVersionInterceptor.cs` | new | TD-I |
| `src/api/Modules/Board/Persistence/Migrations/*` | new | Initial migration; `COLLATE "C"` on `sort_order`; composite FKs; CHECK constraints |
| `src/api/Modules/Board/Ordering/LexoKey.cs` + `OrderingService.cs` | new | TD-C |
| `src/api/Modules/Board/Services/InitiativeMoveService.cs` | new | FR-11, FR-16, §3.3(2) |
| `src/api/Modules/Board/Services/ColumnTypeChangeService.cs` | new | §3.3(1), §3.3(3) |
| `src/api/Modules/Board/Services/StructuralDeletionGuard.cs` | new | FR-12 |
| `src/api/Modules/Board/Endpoints/*.cs` + `Dtos/*.cs` | new | TD-F |
| `src/api/Modules/Board/ModuleMarker.cs` | modify | Becomes the module's DI registration entry point |
| `tests/api/ProjectTracker.Api.Tests/*` | new | Testcontainers PostgreSQL fixture. **Test design is owned by `/write-tests` — not specified here.** |

---

### Risks and Mitigations

| # | Risk | Severity | Mitigation |
|---|---|---|---|
| **R-1** | `platform-foundation` has not shipped: no provisioned Postgres, no gated migration step, no CI. FR-15 is only half-satisfiable. | **High** | Develop against Testcontainers Postgres. Author migrations expand/contract-compatible and **never** auto-migrate at startup. Treat the gated deployment step as a `platform-foundation` deliverable and record the dependency explicitly. **Do not let this feature invent a bespoke migration runner.** |
| **R-2** | The authorization stub leaks to a deployed environment, producing a total board-data exposure. | **Critical** | `StubBoardAuthorizationService` registration **throws at startup** unless `ASPNETCORE_ENVIRONMENT=Development` *and* an explicit `Board:AllowStubAuthorization=true` flag is set. Add "no stub auth outside Development" as a named, non-negotiable code-review gate. |
| **R-3** | `sort_order` ordering differs from key-generation ordering due to database collation. | High | `COLLATE "C"` declared explicitly on the column in the initial migration, and asserted by a migration-level test. This is a silent, data-corrupting failure if missed. |
| **R-4** | Concurrent rebalance of the same ordering scope deadlocks or interleaves with a concurrent insert. | Medium | `SELECT ... FOR UPDATE` over the scope in ascending `sort_order`; unique index on `(scope, sort_order)` so an interleaved insert fails loudly rather than silently duplicating a position. |
| **R-5** | No `DEVELOPMENT_STANDARDS/` directory exists (HLD TD-11). This feature is setting the project's de-facto API, EF and migration conventions by precedent. | Medium | TD-F and TD-A/G above are the conventions. **Recommend the BA raise TD-11 now** — this feature is the natural source document, and the cost of divergence rises steeply once a second module ships endpoints. |
| **R-6** | `users` table does not exist, so assignee/`createdBy` FKs cannot yet be declared. | Medium | Columns as plain `uuid`; FK authored but disabled, enabled by a follow-up expand migration when `server-rbac` lands. Application-level validation of `userId` via the Identity & Access interface in the interim. |
| **R-7** | Scope creep into `concurrency-control` via the move endpoints. | Medium | `row_version` is stored and incremented only (TD-I). **`409` is forbidden in this feature** (TD-F). Any merge/conflict logic appearing in review is out of scope and must be rejected. |
| **R-8** | The dormancy/carry-over state machine is the feature's real complexity and is easy to get subtly wrong. | Medium | All transitions implemented in two named services with a single transactional entry point each, never inline in endpoints. §3.3 resolutions are binding inputs to `/write-tests`. |

---

### Performance, Security and Scalability

**Performance.** The NFR ceiling (30 initiatives, 2,000 tasks, 12 columns, 8 sub-stages/column per board) is small — a single board fits comfortably in memory and the p95 targets (reads <250 ms, writes <400 ms) are achievable without caching, denormalised read models, or query tuning beyond correct indexing. Required indexes: the composite unique keys `(board_id, id)` on every parent (TD-B, which double as covering indexes), the ordering-scope unique indexes (TD-C), `tasks(board_id, initiative_id)`, `tasks(board_id, task_sub_stage_id)`, and `task_assignees(user_id)` for the future cross-board "assigned to me" query. The one query to watch is the full board load; it should be a small number of flat scoped queries assembled in memory, **not** a deep `Include` graph — EF `Include` across four levels with soft-delete filters produces cartesian explosion. Rebalance is O(scope) with a bounded scope ≤2,000 rows and is expected to be rare.

**Security.** Three layers, all present: the endpoint filter resolving `BoardAccessContext` before any data access (HLD §2.4 layer 2); `RequireAdministrator()` on structural and initiative mutations per FR-10 (layer 3); and the ORM global query filter on `board_id` as the net (layer 4, TD-E). Non-membership and inactive membership return `404` with no discriminating detail. The Board module makes **no** authorization decisions — it asks. R-2 is the dominant security risk and is the one item here that deserves adversarial attention.

**Scalability.** Nothing in this design constrains the HLD's growth path. Composite FKs and fractional indexing both scale to orders of magnitude beyond the MVP ceiling. The board-scoped `board_id` on every table is also the natural future partition key should HLD TD-2 (read replica / partitioning) ever trigger.

---

### Dependencies and Integration Points

| Dependency | Type | State | Handling |
|---|---|---|---|
| `platform-foundation` (Postgres, EF Core, gated migration pipeline) | Hard | **Not delivered** | Chunk 1 carries the EF Core/Npgsql wiring; the deployment gate remains `platform-foundation`'s. See R-1. |
| `server-rbac` (`BoardAccessContext` minting, `users` table) | Hard | **Not delivered** | Interface + guarded Development-only stub. See R-2, R-6. |
| `concurrency-control` | Soft | Not started | `row_version` stored only. `409` reserved. See TD-I, R-7. |
| `board-rendering`, `task-management`, `realtime-transport`, `comments`, `activity-notifications` | Soft | Not started | Consumers. The move endpoints (TD-F) are the contract they will depend on — design them as if they are already public. |

---

### Estimated Complexity / Effort

**~2–2.5 weeks** for one developer, consistent with the HLD §8.1 estimate of M (~2 wks). Roughly: 2–3 days persistence foundation and EF wiring; 3–4 days schema, entity configuration and the composite-FK migration; 2 days ordering and rebalance; 3–4 days the move/dormancy/carry-over services; 3 days endpoints, DTOs and the error contract. Well under the 5-week threshold — **no split required.**

---

### Open Technical Questions

| # | Question | Owner | Blocking? |
|---|---|---|---|
| T-1 | BA OQ-2 (fractional key rebalance threshold) is **resolved**: 48 characters within `varchar(64)`, `COLLATE "C"`. See TD-C. | Tech Lead — closed | No |
| T-2 | When `platform-foundation` lands, does its migration pipeline support **per-module** migration history tables (TD-A)? If it assumes a single `DbContext`, TD-A must be revisited. | Tech Lead + `platform-foundation` | No — flag at integration |
| T-3 | BA OQ-1 (dormant sub-stage restoration) interacts with §3.3(1): if restoration is ever approved, `dormantSubStageId` must become a durable snapshot rather than an FK. Record against OQ-1. | Product owner + future BA | No |
| T-4 | BA OQ-3 (task-edit permission nuance) is deferred to `server-rbac` as the spec states. This feature assumes any active member may create/update tasks and places the check behind `BoardAccessContext` so tightening it later is a policy change, not a refactor. | Product owner + `server-rbac` | No |
| T-5 | Should HLD TD-11 (`DEVELOPMENT_STANDARDS/`) be raised now, using TD-F as its seed? Recommended. | Tech Lead + BA | No |

---

### NFR Confirmation

**Confirmed: yes.** The Business Spec states data integrity, security, reliability, performance (quantified: p95 reads <250 ms, writes <400 ms, at an explicit board-size ceiling) and compatibility requirements, all consistent with HLD §1.6 which the product owner confirmed on 2026-10-11 (D-OPEN-3). **No NFRs were assumed.**

### Cost Flags

**None. No product-owner approval required to proceed.** This feature introduces no paid service and no metered usage. EF Core, Npgsql, PostgreSQL and Testcontainers are free and open source. Fractional indexing and UUIDv7 are own code, deliberately chosen over new dependencies. PostgreSQL hosting is already covered by **CF-1, approved** within the ~$15–30/month pilot envelope; this feature's data volume (≤2,000 tasks/board) is immaterial against it. CF-4 (Redis) and CF-5 (Resend) are not touched.

---

### Implementation Chunks

| # | Name | Scope | Depends on | Size |
|---|------|-------|------------|------|
| 1 | Persistence foundation & access boundary | Npgsql/EF Core packages; `BoardDbContext` skeleton; board-scope accessor and global query filters (TD-E); `UuidV7` helper (TD-H); `BoardAccessContext` + `IBoardAuthorizationService` + guarded Development-only stub (TD-D, R-2); `RowVersionInterceptor` (TD-I); `Program.cs` wiring with no startup migration; Testcontainers Postgres fixture wiring. Leaves the app building and healthy with an empty Board schema. | — | small–medium |
| 2 | Domain entities, schema & integrity constraints | All eight entities and two enums; EF entity configurations; composite FKs and `(board_id, id)` unique constraints (TD-B); CHECK constraints for `column_type`/`priority` (TD-G); soft-delete columns; `sort_order varchar(64) COLLATE "C"` (R-3); ordering-scope unique indexes; initial expand/contract migration. | Chunk 1 | medium |
| 3 | Ordering & structural state machine | `LexoKey` + `OrderingService` with 48-char rebalance threshold and `FOR UPDATE` scope locking (TD-C, R-4); `InitiativeMoveService` (FR-11, FR-16, carry-over exclusion, §3.3(2)); `ColumnTypeChangeService` (§3.3(1), §3.3(3)); `StructuralDeletionGuard` (FR-12); `isInactive` set/clear rules (FR-16). All transactional, all behind `BoardAccessContext`. | Chunk 2 | medium |
| 4 | API surface | Board-scoped endpoint groups and DTOs for boards, columns, sub-stages, initiatives, tasks, assignees and tags (FR-09); explicit `:move` intent endpoints; `BoardAuthorizationFilter` with 404-on-miss; RFC 9457 ProblemDetails with 422 for structural violations and **no 409** (TD-F, R-7); administrator assertions per FR-10. | Chunk 3 | medium |

Each chunk leaves the codebase compiling and committable.

## 4. Test Plan

<!-- gate:tests status=WRITTEN_AND_FAILING authored_by=tester at=2026-10-11T08:03:56Z -->

### Test Plan Summary

| Layer | Framework | # Tests | Areas covered |
|---|---|---|---|
| Unit — C# backend | xUnit (net8.0) | 23 | `UuidV7` generation (TD-H); `LexoKey` fractional ordering, rebalance threshold (TD-C); `BoardAccessContext` capability-token boundary, stub fail-fast (TD-D, R-2); `InitiativeMoveService` move/dormancy/carry-over state machine (AC lines 32–34, FR-16, §3.3(2)); `ColumnTypeChangeService` type-change dormancy rules (§3.3(1), §3.3(3)); `StructuralDeletionGuard` (FR-12, AC line 35); domain invariants — sub-stage parent-column rule (FR-03), fixed `TaskPriority` enum (FR-06) |

This round covers the **genuinely non-trivial logic** the Tech Lead called out in §3 (Key Technical Decisions, R-8) as the real complexity of this feature — the fractional-ordering algorithm, the UUIDv7 generator, the capability-token access boundary, and the initiative-move/column-type-change/structural-deletion state machine. These are pure in-memory logic tests with no database dependency, chosen deliberately so Chunk 1–3 development (Persistence foundation → Domain/schema → Ordering/state-machine, per the Implementation Chunks table) has a concrete, fast-running red suite to build against immediately, without first standing up `BoardDbContext`.

### Test files added

All new files are under `tests/api/ProjectTracker.Api.Tests/Board/` (existing xUnit project; no new test framework introduced):

- `gherkin-scenarios.md` — plain-English Given/When/Then source for every test below
- `UuidV7Tests.cs` — 3 tests
- `LexoKeyTests.cs` — 6 tests
- `BoardAccessContextTests.cs` — 4 tests
- `InitiativeMoveServiceTests.cs` — 5 tests
- `ColumnTypeChangeServiceTests.cs` — 2 tests
- `StructuralDeletionGuardTests.cs` — 5 tests
- `DomainInvariantTests.cs` — 3 tests

### Acceptance Criteria → Test Case mapping

| Acceptance Criterion (Section 1) | Test(s) |
|---|---|
| UUIDv7 identifiers for every persisted entity | `UuidV7Tests.NewGuid_sets_version_7_and_variant_bits`, `..._timestamp_is_monotonic_non_decreasing`, `..._produces_unique_values_under_rapid_generation` |
| Only `initiative_task` columns can contain task sub-stages | `DomainInvariantTests.TaskSubStage_create_rejects_parent_column_that_is_not_initiative_task`, `..._succeeds_for_initiative_task_column` |
| Task's active sub-stage tied to its initiative's current column, except inactive-sub-stage carry-over | `InitiativeMoveServiceTests.Move_reassigns_active_substage_task_to_destination_first_substage`, `..._excludes_inactive_substage_task_from_reassignment_carry_over` |
| Admin can mark a sub-stage inactive; inactive sub-stage can't receive new tasks directly but can be a landing stage | `InitiativeMoveServiceTests.Move_excludes_inactive_substage_task_from_reassignment_carry_over` (landing-stage behaviour); direct-creation rejection is **not yet covered** — see Gaps |
| Move to different `initiative_task` column reassigns to destination's first sub-stage; rejected if destination has none; inactive-sub-stage tasks excluded | `Move_reassigns_active_substage_task_to_destination_first_substage`, `Move_is_rejected_when_destination_has_no_substages`, `Move_excludes_inactive_substage_task_from_reassignment_carry_over` |
| Move to `initiative_only` column sets all tasks dormant (including inactive-sub-stage tasks, per §3.3(2)) | `Move_into_initiative_only_column_sends_all_tasks_dormant_including_inactive_substage_tasks` |
| Return from `initiative_only` resets every task to destination's first sub-stage | `Move_from_initiative_only_resets_every_task_to_destination_first_substage` |
| Column type change `initiative_task→initiative_only` applies dormancy; `initiative_only→initiative_task` requires sub-stage before *moving* initiatives in (type change itself permitted with zero) | `ChangeToInitiativeOnly_applies_dormancy_and_removes_substages`, `ChangeToInitiativeTask_is_permitted_with_zero_substages_and_leaves_tasks_dormant` |
| Sub-stage/column cannot be deleted while non-empty | `StructuralDeletionGuardTests` — all 5 tests |
| Sort order uses fractional-index keys; synchronous rebalance on collision/exhaustion | `LexoKeyTests` — all 6 tests |
| `BoardAccessContext` resolved before any data access; dev/test stub permitted | `BoardAccessContextTests.BoardAccessContext_has_no_public_constructor`, `StubBoardAuthorizationService_throws_at_construction_outside_development_without_flag` |
| Administrator-only structural operations | `BoardAccessContextTests.RequireAdministrator_throws_when_role_is_member`, `..._does_not_throw_when_role_is_administrator` |
| Task priority fixed enum | `DomainInvariantTests.TaskPriority_enum_has_exactly_the_four_fixed_values` |

### Test command

```bash
dotnet test tests/api/ProjectTracker.Api.Tests/ProjectTracker.Api.Tests.csproj
```

### TDD gate status: confirmed RED

The suite currently **fails to compile** — captured verbatim in this run's output (18 `CS0234`/`CS0246` errors, e.g. `The type or namespace name 'Domain' does not exist in the namespace 'ProjectTracker.Api.Modules.Board'`). This is the expected and correct red state for this feature: the Board module is presently only a `ModuleMarker.cs` stub (confirmed by inspection — no `Domain/`, `Ordering/`, `Services/`, `Access/`, or `Shared/UuidV7.cs` exist in `src/api`), and per the guard rail against writing production code, no placeholder types were added to make the suite merely assertion-red instead of compile-red. The test files define the exact public contracts (namespaces, type names, member shapes) the build agent must produce — see each test file's `using` statements and constructed-object shapes for the authoritative contract. Once Chunks 1–3 (Persistence foundation; Domain entities/schema; Ordering & structural state machine, per §3 Implementation Chunks) land, this suite must go from compile-red → assertion-green with no test-file changes other than genuine scope bugs found along the way.

### Assumptions made

- `TaskItem` (named to avoid colliding with `System.Threading.Tasks.Task`) is modelled as an immutable `record` with `init`-only properties, supporting non-destructive `with` updates in test arrange code — the build agent may implement it as a class or record provided the shown property shape and immutable-update semantics are preserved.
- `InitiativeMoveService` and `ColumnTypeChangeService` are modelled as stateless operations over explicit in-memory collections (`IReadOnlyList<TaskItem>`, etc.) rather than taking a `DbContext` directly — this keeps the core state-machine logic unit-testable and fast; the build agent is expected to wrap these with a thin persistence-aware caller (loading the relevant rows, invoking the pure operation, and persisting the result) rather than inlining the state-machine logic into a DB-aware method.
- `BoardStructuralViolationException` is the single exception type used for all `422 /problems/board-structure-violation` rejections (TD-F), thrown directly by domain/service code rather than constructed at the endpoint layer.
- `StubBoardAuthorizationService`'s fail-fast check (R-2) is exercised via its constructor in these tests; the build agent may instead enforce this at DI-registration time as long as the equivalent guarantee holds (stub unusable outside Development without the explicit flag) — if so, this test must be adapted to match the real enforcement point, which is an acceptable, expected adjustment during `/build`, not a spec deviation.
- Byte-layout assertions in `UuidV7Tests` assume the big-endian 48-bit timestamp occupies the first 6 bytes of the `Guid`, per TD-H's explicit note on avoiding careless `new Guid(byte[])` round-tripping. If the build agent's layout differs (while still being a valid, monotonic UUIDv7), the test's extraction helper — not its intent — would need updating.

### Not covered and why (gaps for a follow-up test round)

- **EF Core / PostgreSQL integration tests** (composite board-boundary FKs per TD-B, `COLLATE "C"` byte-lexicographic ordering per R-3, `CHECK` constraints per TD-G, the global query filter per TD-E, soft-delete filtering): all require a real `BoardDbContext` and migrations, which don't exist yet (Chunks 1–2). Docker/Testcontainers is confirmed available in this environment (`docker version 29.3.1`, daemon running) so there is no infrastructure blocker — this is purely sequencing. **Recommended follow-up:** re-invoke `/write-tests` (or extend this suite directly) once `BoardDbContext` and the initial migration exist, before `/review` signs off Chunks 1–2, specifically to prove the composite-FK board-boundary guarantee (TD-B's core integrity claim) and the collation trap (R-3) with an actual database rather than in-memory logic alone.
- **`BoardAuthorizationFilter` endpoint-level 404-on-miss behaviour** and the full API/DTO/`ProblemDetails` error-envelope contract (TD-F): deferred until minimal-API endpoints exist (Chunk 4).
- **Direct task creation into an inactive sub-stage being rejected** (AC line 31, FR-16): the carry-over/landing-stage behaviour is covered via the move tests, but the "cannot receive directly created tasks" rejection path has no dedicated test yet, since task-creation endpoints/services aren't designed in this spec round. Flagged for a follow-up test when Chunk 3/4 task-creation logic is scoped.
- **`isInactive` clear-guard rejection** ("reject clearing `isInactive` while it would leave a retained task in a sub-stage outside its initiative's current column" — FR-16 final sentence): not covered; no service signature for this exists yet in the TL's breakdown beyond "SubStageService" being named in the module tree with no further detail. Flagged for a follow-up round once that service's contract is defined.
- **Fractional-index rebalance under real concurrent writers** (`SELECT ... FOR UPDATE` deadlock-freedom, R-4): the `LexoKeyTests.Rebalance_...` test proves the algorithm's correctness single-threaded; proving the locking/deadlock-avoidance claim requires an integration test against a real database and is deferred alongside the other persistence-layer gaps above.
- **UUIDv7 reflection-based constructor-access test** (Gherkin 3.1 — "does not compile") was intentionally **not** written as code: a test asserting something doesn't compile cannot itself run in the normal suite. `BoardAccessContext_has_no_public_constructor` substitutes a runtime reflection check for the equivalent guarantee.

## 5. Implementation Notes

<!-- gate:build status=COMPLETE authored_by=developer-implementer at=2026-10-12T00:00:00Z -->

### Chunks completed
- Chunk 1 — Persistence foundation & access boundary: EF Core/Npgsql wiring, `UuidV7`, access boundary, row-version plumbing, and API startup wiring.
- Chunk 2 — Domain entities, schema & integrity constraints: Board domain types, EF configurations, composite FKs, constraints, and initial migration.
- Chunk 3 — Ordering & structural state machine: fractional ordering, initiative moves, column type changes, deletion guards, and inactive sub-stage rules.
- Chunk 4 — API surface: board-scoped minimal API endpoint groups, CRUD scaffolding for boards/columns/sub-stages/initiatives/tasks/tags/assignees, explicit initiative/task move endpoints, authorization filter wiring, and RFC 9457 `ProblemDetails` mapping for structural rule failures.

### Round 2 fixes
- Added administrator gating to initiative mutations in `BoardEndpointMap` so `CreateInitiativeAsync`, `UpdateInitiativeAsync`, and initiative `:move` all enforce `BoardAccessContext.RequireAdministrator()` per FR-10 / TD-F.
- Corrected the column type-change task selection query to load tasks from initiatives whose `ColumnId` matches the changed column, rather than comparing `Task.InitiativeId` to the column id.
- Removed the unused `ResolveAccessContextAsync` helper from `BoardEndpointMap`, eliminating the CS1998 warning path.
- Kept `src/web/package.json` aligned with the main-branch workspace contract dependency (`"@initiative-tracker/contracts": "workspace:*"`); no branch-only drift remains.
- Changed `BoardProblemDetailsExceptionHandler` so `BoardStructuralViolationException` maps to `422 /problems/board-structure-violation`, while `BoardForbiddenException` maps to `403 /problems/forbidden`.
- Added `<UseAppHost>false</UseAppHost>` to `src/api/ProjectTracker.Api.csproj` so local build/test execution is stable on this environment.

### Files changed
- `src/api/Program.cs` — registered board services and startup wiring.
- `src/api/ProjectTracker.Api.csproj` — added EF Core / Npgsql packages and disabled apphost generation for local CLI stability.
- `src/api/Shared/UuidV7.cs` — UUIDv7 helper.
- `src/api/Modules/Board/Access/*` — access context, authorization service, scope accessor, stub guard, and exception types.
- `src/api/Modules/Board/Domain/*` — board entities, enums, and invariants.
- `src/api/Modules/Board/Persistence/*` — `BoardDbContext`, interceptor, configurations, and initial migration.
- `src/api/Modules/Board/Ordering/*` — fractional key generation and ordering service.
- `src/api/Modules/Board/Services/*` — initiative move, column type change, and deletion guard logic.
- `src/api/Modules/Board/Endpoints/*` — board endpoint map, DTOs, and `ProblemDetails` handler updates for initiative authorization and forbidden mapping.
- `src/api/Modules/Board/ModuleMarker.cs` — board endpoint registration extension.
- `tests/api/ProjectTracker.Api.Tests/Board/*` — approval-gate tests and compile targets written by tester.
- `tests/api/ProjectTracker.Api.Tests/TestInfrastructure/PostgresFixture.cs` — PostgreSQL fixture wiring.
- `src/web/package.json` — workspace test/runtime dependency repair.

### Key decisions
- `BoardAccessContext` is a capability token with an internal constructor so authorization becomes structurally unavoidable.
- Fractional ordering uses a custom base-62 key generator with synchronous rebalance inside the write transaction.
- UUIDv7 is implemented in application code to avoid a .NET 9 dependency.
- `409 Conflict` remains reserved for later concurrency-control work; structural violations use `422`.

### Tests
- Test commands:
  - `dotnet build src/api/ProjectTracker.Api.csproj`
  - `dotnet test tests/api/ProjectTracker.Api.Tests/ProjectTracker.Api.Tests.csproj`
- Build status: PASS
- Test status: PASS (29/29) with 1 warning from the existing Testcontainers PostgreSQL fixture (`PostgreSqlBuilder` obsolete constructor)
- New tests added beyond Section 4: no
- Final build/test timestamp: 2026-10-12T00:00:00Z

### Deviations from spec
- Chunk 4 added API surface scaffolding rather than a full persistence-backed write path for every endpoint. The minimal API routes are registered and compile cleanly; the endpoint layer delegates structural mutations to the existing board services and emits `ProblemDetails` for board-structure violations.

### Open risks
- `platform-foundation` still owns the gated production migration pipeline; this feature only provides compatible migrations.
- `server-rbac` is still stubbed in Development; the stub guard remains a deployment risk if misconfigured.

## 6. Review Outcome

<!-- gate:review verdict=FAIL reviewer=code-reviewer at=2026-10-11T09:03:46Z -->

### Verdict

FAIL — implementation is not releasable yet.

### Blocking issues

1. **Feature scope is incomplete vs approved spec:** Section 5 explicitly states Chunk 4 (API surface) was not implemented, but FR-09/FR-10 require authorized CRUD capabilities for boards/columns/sub-stages/initiatives/tasks/assignees/tags. There are no Board endpoints in this changeset, so the feature does not yet satisfy the approved acceptance scope.
2. **Board-boundary integrity is broken for join tables:** `task_assignees` and `task_tags` are not board-scoped in schema/domain (no `board_id` column, no composite board FK enforcement), which violates Section 1 data-integrity requirements and allows cross-board linkage risk.
3. **UUIDv7 generation is not reliably spec-compliant:** `UuidV7.NewGuid()` writes version/timestamp bytes and then calls `new Guid(bytes)`, which applies .NET Guid endianness transforms. This can invalidate canonical UUIDv7 layout/ordering expectations required by AC/FR UUIDv7 requirements.
4. **Initiative move carry-over logic depends on process-global mutable registry:** `InitiativeMoveService` resolves sub-stage inactivity through `TaskSubStage.TryGetRegistered(...)` static state instead of explicit operation inputs/persisted lookup. This is non-deterministic and can misapply FR-16 carry-over rules when registry state is incomplete/stale.

### Non-blocking suggestions

- Remove unrelated `src/web/package.json` workspace dependency change from this feature branch unless it is explicitly required by board-data-model scope.
- Replace broad `npm test` reporting in Section 5 with the feature-targeted API test command to keep gate evidence precise.

### Files of concern

- `src/api/Program.cs`
- `src/api/Shared/UuidV7.cs`
- `src/api/Modules/Board/Services/InitiativeMoveService.cs`
- `src/api/Modules/Board/Domain/TaskSubStage.cs`
- `src/api/Modules/Board/Persistence/Configurations/TaskAssigneeConfiguration.cs`
- `src/api/Modules/Board/Persistence/Configurations/TaskTagConfiguration.cs`
- `src/api/Modules/Board/Persistence/Migrations/20261011193000_InitialCreate.cs`

<!-- gate:review verdict=FAIL reviewer=code-reviewer at=2026-10-12T04:46:49Z -->

### Verdict

FAIL — re-review found prior blockers 2–4 resolved, but the new API surface still violates the approved contract and authorization semantics.

### Blocking issues

1. **Initiative mutations are not consistently administrator-gated (FR-10 / TD-F):** `CreateInitiativeAsync` and `UpdateInitiativeAsync` do not call `RequireAdministrator(...)` and are mapped without admin enforcement, while initiative mutations are explicitly admin-only in the approved scope.
2. **Authorization errors are mapped to the wrong problem type/status:** `BoardProblemDetailsExceptionHandler` maps `BoardForbiddenException` to `422 /problems/board-structure-violation`. Structural-violation 422 is reserved for board-structure rule failures; authorization failures must not be emitted as structural violations.
3. **Column type-change task selection is incorrect, breaking structural transition logic:** `UpdateColumnAsync` loads tasks with `x.InitiativeId == columnId` (column id compared to initiative id). This fails to select actual tasks in initiatives within that column, so dormancy/carry-over mutations can be skipped incorrectly during column type changes.

### Non-blocking suggestions

- Remove or justify unrelated `src/web/package.json` changes before release if they are not required for board-data-model scope.
- Drop unused `ResolveAccessContextAsync` in `BoardEndpointMap` (CS1998 warning) to keep endpoint code clean.

### Files of concern

- `src/api/Modules/Board/Endpoints/BoardEndpointMap.cs`
- `src/api/Modules/Board/Endpoints/BoardProblemDetailsExceptionHandler.cs`
- `src/api/Program.cs`

<!-- gate:review verdict=FAIL reviewer=code-reviewer at=2026-10-12T10:30:00Z -->

### Verdict

FAIL — third-round review confirms the claimed round-2 fixes landed, but the endpoint contract still has blocking TD-F/FR violations.

### Blocking issues

1. **Wrong status code for missing board-scoped ids in sub-stage creation:** `CreateSubStageAsync` returns `401 Unauthorized` when the parent column is absent (`BoardEndpointMap.cs`, line 278). TD-F requires `404` for board-scoped ids not visible/in-scope, with no auth-leaking status variance.
2. **Wrong error classification in task move endpoint for missing destination ids:** `MoveTaskAsync` converts missing destination initiative/sub-stage ids into `BoardStructuralViolationException` (`BoardEndpointMap.cs`, lines 612-624), producing `422`. Under TD-F, missing/invisible board-scoped ids must be `404`; `422 /problems/board-structure-violation` is reserved for structural-rule rejections.
3. **Initiative can still be moved via generic update, bypassing explicit `:move` intent/state machine:** `UpdateInitiativeAsync` directly mutates `initiative.ColumnId` (`BoardEndpointMap.cs`, lines 420-423). This violates TD-F’s explicit intent endpoint rule and can bypass FR-11/FR-16 transition logic that must run through the move path.

### Non-blocking suggestions

- Tighten `dotnet test` output hygiene by resolving the existing obsolete Testcontainers constructor warning in `tests/api/ProjectTracker.Api.Tests/TestInfrastructure/PostgresFixture.cs`.

### Files of concern

- `src/api/Modules/Board/Endpoints/BoardEndpointMap.cs`
- `src/api/Modules/Board/Endpoints/BoardProblemDetailsExceptionHandler.cs`

### Next step

return to implementer

## 7. Release Notes

_Pending — run `/release`._
