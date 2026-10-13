# EF Core & Migration Conventions

**Source:** HLD §2.3, §2.4, §5.1, §7.2 · `board-data-model` TD-A, TD-B, TD-C,
TD-E, TD-G, TD-H, TD-I, R-3

Stack: **PostgreSQL** (Railway managed), **EF Core 8** + `Npgsql.EntityFrameworkCore.PostgreSQL`,
`net8.0`.

## 1. Naming

| Element | Convention | Example |
|---|---|---|
| Table | `snake_case`, plural | `task_sub_stages` |
| Column | `snake_case` | `dormant_sub_stage_id` |
| CLR entity | `PascalCase`, singular | `TaskSubStage` |
| CLR property | `PascalCase` | `DormantSubStageId` |
| Index | `ix_{table}_{columns}` | `ix_tasks_board_id_initiative_id` |
| Unique index | `ux_{table}_{columns}` | `ux_initiatives_board_id_id` |
| Check constraint | `ck_{table}_{rule}` | `ck_tasks_priority` |
| Foreign key | `fk_{table}_{referenced}` | `fk_tasks_initiatives` |

Mapping is configured explicitly in `IEntityTypeConfiguration<T>` classes under
`Modules/{Module}/Persistence/Configurations/`. **No data annotations on
entities** — keep the domain clean of persistence concerns, and keep all mapping
discoverable in one place.

## 2. One DbContext per module

Each module owns a `DbContext` with **its own migrations history table**
(`__EFMigrationsHistory_{Module}`) so modules migrate independently. All contexts
target the **same database and the same `public` schema**.

The HLD §4.2 module boundary is a **code** boundary, not a storage boundary.

### 2.1 Cross-module foreign keys

Cross-module FKs **are permitted at the database level**, declared as **shadow
FKs with no navigation property** into the other module's entities. Real
referential integrity is worth more than schema purism, and a navigation-free FK
preserves the code boundary that actually matters.

A module **MUST NOT** query another module's tables. Cross-module reads go
through that module's public interface.

## 3. Board-boundary integrity is structural

> This is the most important rule in this document.

Every board-scoped table carries `board_id` **directly**, even where it is
derivable. Every board-scoped child declares its FK to its parent on
`(board_id, parent_id)`, backed by a unique constraint `(board_id, id)` on each
parent.

```
task_sub_stages (board_id, parent_column_id) → board_columns (board_id, id)
initiatives     (board_id, column_id)        → board_columns (board_id, id)
tasks           (board_id, initiative_id)    → initiatives   (board_id, id)
tasks           (board_id, task_sub_stage_id)→ task_sub_stages (board_id, id)
```

Effect: it is **physically impossible** to attach a child to a parent on another
board. A whole class of authorization bug becomes a constraint violation. The
cost is a few redundant unique indexes, which are useful covering indexes anyway.

Every authorization check and query filter is then a single predicate on
`board_id` with **no joins to get there**. A redundant column is cheap; a missed
join in an authorization filter is a data breach.

**What composite FKs deliberately do NOT enforce:** cross-entity rules that the
domain legitimately permits to diverge (e.g. a task's sub-stage belonging to its
initiative's *current* column, which the inactive-sub-stage carry-over rule
allows to differ). Such rules are **application-layer invariants**, enforced in
named mutation services. Do not add a trigger that contradicts a domain rule.

## 4. Global query filters are the net

Every board-scoped entity implements `IBoardScoped` and receives a global query
filter:

```csharp
e => e.BoardId == _boardScope.Current && e.DeletedAt == null
```

`_boardScope` is a scoped accessor set from the resolved `BoardAccessContext`.
This is enforcement layer 4 (see [AUTHORIZATION.md §3](./AUTHORIZATION.md)) and
is a **net, not a substitute** for explicit authorization.

> ⚠️ Known EF behaviour: a **required** navigation to a filtered entity produces
> silently empty results. Include/join behaviour across soft-deleted parents
> **MUST** be explicit and deliberate.

## 5. Keys, enums, ordering

### 5.1 UUIDv7 primary keys, generated in application code

- Column type is native Postgres `uuid`.
- **No database `DEFAULT`.** The HLD requires client-generated identity so
  optimistic UI and offline creation get a permanent id before the server sees
  it. The application **MUST** always be the generator.
- `.NET 8` has no `Guid.CreateVersion7()`. Use the shared `Shared/UuidV7.cs`
  helper. Replace it with the BCL method on the move to .NET 9+/.NET 10 LTS.
- Random UUIDv4 **MUST NOT** be used for primary keys — it fragments the B-tree.

### 5.2 Enums as constrained strings

Enums map to `varchar` via an EF value conversion, **plus a database `CHECK`
constraint** listing the permitted values.

Rationale: readable in the database during incident response, no
ordinal-renumbering hazard across migrations, and the `CHECK` gives the domain
rule teeth at the storage layer. Integer-backed enum columns **MUST NOT** be used.

### 5.3 Ordering: fractional index keys

- Column is `varchar(64) NOT NULL` and **MUST** declare `COLLATE "C"` explicitly
  in the migration, to guarantee byte-lexicographic ordering independent of
  database locale.

  > ⚠️ **This is a silent, data-corrupting failure if missed** (`board-data-model`
  > R-3). Default locale collation does not order these keys the same way the
  > key generator does.

- Alphabet is base-62 (`0-9A-Za-z`), generated by the shared `LexoKey` utility.
- **Rebalance trigger:** key length > 48 characters, no representable midpoint
  between neighbours, or a unique-constraint collision on the ordering scope.
- Rebalance is **synchronous, inside the write transaction, scoped to the
  affected ordering scope only**. Lock with `SELECT ... FOR UPDATE` over the
  scope in **ascending `sort_order`** — taking locks in a consistent order makes
  deadlock between two concurrent rebalances impossible.
- Each ordering scope gets a unique index on `(scope columns..., sort_order)`.
  Where a scope column is nullable, use `NULLS NOT DISTINCT` (PG15+) so two
  nulls collide correctly.

### 5.4 `row_version`

`row_version bigint NOT NULL DEFAULT 1`, incremented by a
`SaveChangesInterceptor` on every modification of a concurrently-editable entity.

Until `concurrency-control` ships, it is **stored and maintained only** — not
configured as an EF concurrency token. That feature then becomes a behavioural
change with no migration.

### 5.5 Soft deletes

Soft delete (`deleted_at`) on Board, Initiative, Task, Comment — entities that
need undo, audit integrity, and resolvable activity-event targets. Join tables
and membership-status changes are **hard** deletes.

Soft delete is not a retention policy. A purge path is required (HLD TD-5) — a
soft-delete-only system with no hard delete is a GDPR problem waiting to happen.

## 6. Migrations

- **MUST** be authored **expand/contract** and backward-compatible with the
  **previously deployed API version**. A deploy runs migrations before the new
  API, so the old API must survive the new schema.
- **MUST NOT** run at application startup. `Database.Migrate()` **MUST NOT**
  appear in `Program.cs`. Startup migrations plus multiple replicas is a
  well-known way to corrupt a schema.
- **MUST** run as an explicit, gated pipeline step before the API deploys
  (owned by `platform-foundation`).
- A destructive change (drop column, drop table, narrow a type, tighten a
  constraint) is a **separate, later migration** shipped only after the code
  that depended on it is gone.
- Generated migrations **MUST** be reviewed by hand before commit. Check for
  unintended drops, missing `COLLATE "C"`, and table rewrites on large tables.
- **MUST NOT** write a bespoke migration runner. EF Core migrations are the
  mechanism.
- Data migrations that touch many rows **SHOULD** be batched and idempotent.

## 7. Querying

- Board load **MUST** be a small number of flat, scoped queries assembled in
  memory — **not** a deep `Include` graph. `Include` across four levels with
  soft-delete filters produces cartesian explosion.
- Read-only queries **SHOULD** use `AsNoTracking()`.
- Projections to DTOs **SHOULD** be done in the query, not after materialisation.
- Raw SQL is permitted for genuine performance needs but **MUST** be
  parameterised and **MUST** retain the `board_id` predicate.
- Every structural mutation and its dependent state transitions **MUST** be
  atomic in a single transaction.

## 8. Review checklist

- [ ] New board-scoped table carries `board_id` directly
- [ ] Composite `(board_id, parent_id)` FK with matching `(board_id, id)` unique constraint
- [ ] Entity implements `IBoardScoped` and is covered by the global query filter
- [ ] Mapping is in an `IEntityTypeConfiguration<T>`, not annotations
- [ ] Enum columns are `varchar` with a `CHECK` constraint
- [ ] New `sort_order` column declares `COLLATE "C"`
- [ ] Ids are UUIDv7 from the shared helper, no database default
- [ ] Migration reviewed by hand; expand/contract; no destructive change in the same migration
- [ ] No `Database.Migrate()` anywhere
- [ ] No deep `Include` graph on a hot read path
