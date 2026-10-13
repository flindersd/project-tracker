# API Design Conventions

**Source:** HLD §4.2, §4.4 · `board-data-model` TD-F (promoted from feature-local
precedent to project standard per HLD TD-11)

The API is REST over JSON, ASP.NET Core minimal APIs, consumed by one React SPA.
GraphQL was explicitly rejected (HLD §5.4) — the surface is ~25 endpoints and
REST is clearer.

## 1. Routes

### 1.1 Board-scoped routes MUST be board-prefixed

```
/api/boards
/api/boards/{boardId}/columns
/api/boards/{boardId}/columns/{columnId}/sub-stages
/api/boards/{boardId}/initiatives
/api/boards/{boardId}/tasks
/api/boards/{boardId}/tags
```

`boardId` **MUST NOT** be inferred from a child id. Carrying it in the path
makes the authorization filter uniform across every endpoint and removes an
entire class of scoping bug. This applies to every module, not just Board.

### 1.2 Naming

- Path segments are **lowercase kebab-case**, plural for collections:
  `sub-stages`, not `subStages` or `SubStage`.
- Query parameters are **camelCase**: `?assignedTo=`, `?includeArchived=`.
- No verbs in resource paths — except intent endpoints (§1.3).

### 1.3 Intent endpoints over generic PATCH

Operations that trigger a state machine **MUST** be expressed as explicit intent
endpoints, not field patches:

```
POST /api/boards/{boardId}/initiatives/{id}:move   { columnId, afterId?, beforeId? }
POST /api/boards/{boardId}/tasks/{id}:move         { initiativeId?, taskSubStageId?, afterId?, beforeId? }
```

Rationale: a move triggers the dormancy/carry-over state machine. Expressing it
as a field patch hides that, and makes `concurrency-control`'s field-level merge
semantics incoherent for position changes.

Use the `:verb` suffix (Google AIP style) so intent endpoints are visually
distinct from resources. Reserve them for genuine state transitions — ordinary
field edits stay as `PATCH`.

### 1.4 Clients never send ordering keys

Reorder requests send **neighbour ids** (`afterId` / `beforeId`). The server owns
fractional-index key generation. `sortOrder` **MUST NOT** appear in any request
body, and **SHOULD NOT** appear in responses except where a client genuinely
needs it for local sorting.

## 2. Payloads

- JSON is **camelCase** (System.Text.Json default — do not add a naming policy).
- Enums serialise as **strings**, never integers
  (`JsonStringEnumConverter`): `"initiative_task"`, `"High"`.
- Timestamps are **ISO 8601 UTC with an explicit offset**: `2026-10-13T09:00:00Z`.
- Ids are **UUIDv7**, serialised as canonical lowercase hyphenated strings.
- Absent vs. null: a field omitted from a `PATCH` means "unchanged"; an explicit
  `null` means "clear it". DTOs **MUST** distinguish these — do not treat a
  missing field as a clear.
- DTOs are `record` types and live in `Modules/{Module}/Endpoints/Dtos/`. Domain
  entities **MUST NOT** be returned directly from an endpoint.
- Shared request/response shapes consumed by the SPA belong in
  `packages/contracts` — see [TYPESCRIPT_AND_REACT.md §5](./TYPESCRIPT_AND_REACT.md).

## 3. Field-scoped patches

Mutations send **only the fields the user actually touched**, plus
`expectedRowVersion`. Clients **MUST NOT** send whole entities. This is what
makes server-side field-level merge possible — whole-record writes are how you
lose the due date someone set while you were dragging a card (HLD §3.3).

## 4. Status codes

| Code | Meaning | Notes |
|---|---|---|
| `200` | Read or mutation succeeded with a body | |
| `201` | Resource created | `Location` header required |
| `204` | Succeeded, no body | Deletes |
| `400` | Malformed request — unparseable, wrong type | Shape errors only |
| `401` | No or invalid bearer token | |
| `403` | Confirmed active member, insufficient **role** | Only after membership is established — see AUTHORIZATION.md §4 |
| `404` | Not found **or** not visible to the caller | Non-member, inactive member, out-of-scope id. Non-discriminating. |
| `422` | Request is well-formed but violates a **structural/domain rule** | See §4.1 |
| `409` | **RESERVED — optimistic concurrency only** | See §4.2 |

### 4.1 `422` is for domain-rule violations

All structural-rule rejections use `422 Unprocessable Entity` — deleting a
non-empty column or sub-stage, moving to a column with no sub-stages, creating a
task in an inactive sub-stage, and similar. The request was understood; the
domain refused it.

### 4.2 `409 Conflict` is reserved

`409` **MUST NOT** be used for anything other than optimistic-concurrency
failures. It belongs to `concurrency-control`, which returns the current server
value, the rejected value, and the identity of the other actor. Overloading
`409` for domain rules now would force a breaking change later.

## 5. Error contract — RFC 9457 `ProblemDetails`

Every non-2xx response **MUST** be `application/problem+json` with a **stable
`type` URI**:

```json
{
  "type": "/problems/board-structure-violation",
  "title": "Board structure violation",
  "status": 422,
  "detail": "Column 'In Progress' cannot be deleted while it contains initiatives.",
  "instance": "/api/boards/0192.../columns/0192...",
  "traceId": "00-abc..."
}
```

- `type` is the **machine-readable** discriminator. Clients branch on `type`,
  never on `detail` or `title`.
- `type` URIs are stable and append-only. Changing one is a breaking change.
- `detail` is human-readable and **MUST NOT** leak the existence of resources
  the caller cannot see.
- Validation failures use the standard `errors` extension keyed by camelCase
  field path.
- Register new `type` URIs in the owning module and list them in that module's
  Technical Approach.

## 6. Versioning

No URL version segment at MVP — there is one client and the monorepo gives
atomic contract changes (HLD §7.1). Changes **MUST** be additive and
backward-compatible with the deployed SPA for the duration of a deploy window,
exactly as migrations are (expand/contract). A genuinely breaking change
requires a `/tl` decision, not a unilateral one.

## 7. Real-time envelopes

SignalR messages are **thin by default** and carry the board `sequence`:

```json
{ "entity": "task", "id": "0192...", "sequence": 4471, "changedFields": ["dueDate"] }
```

Clients re-fetch the entity if it is in view. Envelope types live in
`packages/contracts` alongside REST DTOs.

## 8. Review checklist

- [ ] Board-scoped routes carry `{boardId}` in the path
- [ ] State transitions use `:verb` intent endpoints, not generic PATCH
- [ ] No `sortOrder` in any request body
- [ ] Patches are field-scoped with `expectedRowVersion`
- [ ] Enums serialise as strings; JSON is camelCase
- [ ] `409` used only for optimistic concurrency
- [ ] Structural rejections return `422` with a registered `type` URI
- [ ] Non-member paths return `404` with a non-discriminating `detail`
- [ ] No domain entity returned directly from an endpoint
