# Board Data Model — Gherkin Scenarios (Phase 1)

Scope: unit-level tests for the genuinely non-trivial logic identified by the Tech Lead
(§3 Recommended Architecture / Key Technical Decisions) — fractional ordering, UUIDv7
generation, the capability-token access boundary, and the initiative move / column
type-change / structural-deletion state machine. These are the pieces most likely to be
subtly wrong and are explicitly called out as requiring dedicated tests (R-8, TD-C, TD-D).

---

## 1. UuidV7 (TD-H)

1.1
Given no arguments
When `UuidV7.NewGuid()` is called
Then the returned Guid's version nibble (byte 7, high nibble) equals `0x7`
And the returned Guid's variant bits (byte 8, high 2 bits) equal `10`

1.2
Given two sequential calls to `UuidV7.NewGuid()` a few milliseconds apart
When the two Guids are compared using byte-lexicographic ordering of their first 6 bytes (the 48-bit timestamp)
Then the second Guid's timestamp bytes are greater than or equal to the first's

1.3
Given 1,000 calls to `UuidV7.NewGuid()` in a tight loop
When the results are collected into a set
Then all 1,000 values are unique

---

## 2. LexoKey fractional ordering (TD-C)

2.1
Given a `null` left neighbour and a `null` right neighbour (first item in an empty scope)
When a midpoint key is generated
Then a valid non-empty key is returned

2.2
Given left key `"a"` and right key `"b"`
When a midpoint key is generated
Then the result sorts strictly between `"a"` and `"b"` under ordinal (byte-lexicographic) comparison

2.3
Given left key `"a"` and `null` right neighbour (inserting at the end)
When a midpoint key is generated
Then the result sorts strictly after `"a"`

2.4
Given repeated insertion at the same position such that the generated key would exceed 48 characters
When the next key is generated
Then the rebalance-needed signal is raised instead of returning an ever-longer key

2.5
Given two adjacent keys with no representable midpoint (e.g. `"a"` and `"a" + "\u0000"`-equivalent minimal successor)
When a midpoint key is generated
Then the rebalance-needed signal is raised

2.6
Given a scope of 5 items with arbitrary existing keys
When the scope is rebalanced
Then the 5 items receive new keys that preserve their original relative order
And no two items share a key

---

## 3. BoardAccessContext capability token (TD-D)

3.1
Given a `BoardAccessContext` cannot be constructed outside the module
When external code attempts to call `new BoardAccessContext(...)`
Then this does not compile (constructor is `internal`) — verified by code inspection / reflection test, not a runtime assertion

3.2
Given a `BoardAccessContext` resolved with role `Member`
When `RequireAdministrator()` is called
Then a `BoardForbiddenException` is thrown

3.3
Given a `BoardAccessContext` resolved with role `Administrator`
When `RequireAdministrator()` is called
Then no exception is thrown

---

## 4. InitiativeMoveService — column-to-column moves (AC line 32, FR-16, §3.3(2))

4.1 — Active-substage task reassigned to destination's first sub-stage
Given an initiative in column A (`initiative_task`) with a task whose active sub-stage is active (not inactive)
And column B (`initiative_task`) has sub-stages ordered `[S1, S2]`
When the initiative is moved to column B
Then the task's active `taskSubStageId` becomes `S1.Id`

4.2 — Inactive-substage task is excluded from reassignment (carry-over exclusion)
Given an initiative in column A with a task whose active sub-stage `isInactive = true`
And column B (`initiative_task`) has sub-stages ordered `[S1, S2]`
When the initiative is moved to column B
Then the task's active `taskSubStageId` is unchanged (still the original inactive sub-stage in column A)
And the task's initiative link is updated to point at the moved initiative in column B

4.3 — Move rejected when destination has no sub-stages
Given column B is `initiative_task` type with zero configured sub-stages
When an initiative is moved into column B
Then the move is rejected with a structural-violation error
And the initiative remains in its original column

4.4 — Move into an initiative_only column sends all tasks dormant, including inactive-substage tasks (§3.3 resolution 2)
Given an initiative in column A with two tasks: one with an active (non-inactive) sub-stage, one with an inactive sub-stage
And column C is `initiative_only`
When the initiative is moved to column C
Then both tasks have `dormantSubStageId` set to their prior active sub-stage
And both tasks have active `taskSubStageId` set to `null`

4.5 — Returning from initiative_only to initiative_task resets to first sub-stage for every task
Given an initiative currently in an `initiative_only` column with tasks holding only `dormantSubStageId` values
And destination column D (`initiative_task`) has sub-stages ordered `[S1, S2]`
When the initiative is moved to column D
Then every task's active `taskSubStageId` becomes `S1.Id`
And `dormantSubStageId` is left as-is (non-load-bearing / forensic only)

---

## 5. ColumnTypeChangeService (§3.3(1), §3.3(3))

5.1 — Changing initiative_task → initiative_only applies dormancy and removes sub-stages
Given a column of type `initiative_task` with sub-stages `[S1, S2]` containing initiatives whose tasks are split across both sub-stages
When the column type is changed to `initiative_only`
Then every task in that column has `dormantSubStageId` set to its prior active sub-stage and active `taskSubStageId` set to `null`
And the column's sub-stages `S1` and `S2` no longer exist afterward

5.2 — Changing initiative_only → initiative_task is permitted with zero sub-stages
Given a column of type `initiative_only` containing initiatives
When the column type is changed to `initiative_task`
Then the operation succeeds even though the column has zero sub-stages
And every task belonging to initiatives in that column has active `taskSubStageId = null` (left dormant) until an explicit re-entry move

---

## 6. StructuralDeletionGuard (FR-12, AC line 35)

6.1 — Sub-stage with tasks cannot be deleted
Given a task sub-stage that currently has at least one task assigned (active `taskSubStageId` pointing at it)
When deletion of that sub-stage is attempted
Then the deletion is rejected with a structural-violation error

6.2 — Empty sub-stage can be deleted
Given a task sub-stage with zero tasks assigned
When deletion of that sub-stage is attempted
Then the deletion succeeds

6.3 — Column with initiatives cannot be deleted
Given a column containing at least one initiative
When deletion of that column is attempted
Then the deletion is rejected with a structural-violation error

6.4 — Column with sub-stages cannot be deleted
Given an `initiative_task` column with zero initiatives but at least one configured sub-stage
When deletion of that column is attempted
Then the deletion is rejected with a structural-violation error

6.5 — Empty column (no initiatives, no sub-stages) can be deleted
Given a column with zero initiatives and zero sub-stages
When deletion of that column is attempted
Then the deletion succeeds

---

## 7. Domain invariants (FR-03, FR-06)

7.1 — Task sub-stage cannot be created under a non-initiative_task column
Given a column of type `initiative_only`
When a `TaskSubStage` is created with that column as its parent
Then creation is rejected (domain validation failure)

7.2 — Task priority is restricted to the fixed enum set
Given the `TaskPriority` enum
When its defined members are enumerated
Then they are exactly `Low`, `Medium`, `High`, `Urgent` — no more, no fewer

---

## Out of scope for this test round (documented gap — see Test Plan §"Not covered")

- EF Core composite-FK board-boundary enforcement, `COLLATE "C"` ordering, CHECK constraints,
  and the global query filter (TD-A/B/E/G) — these require a real `BoardDbContext` against
  Testcontainers PostgreSQL and are deferred until the persistence layer exists (Chunk 1/2).
  Flagged as a required follow-up integration-test pass once `BoardDbContext` is implemented.
- `BoardAuthorizationFilter` / endpoint-level 404-on-miss behaviour — requires minimal API
  endpoints to exist (Chunk 4).
- Full API/DTO/ProblemDetails contract tests (TD-F) — deferred until endpoints exist (Chunk 4).
