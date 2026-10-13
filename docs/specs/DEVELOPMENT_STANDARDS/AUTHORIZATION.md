# Authorization — Hard Requirements

**Source:** HLD §2.4, §4.2, D-4 · `board-data-model` TD-D, TD-E, R-2

> Server-side authorization is the **highest-severity technical risk in this
> product** (HLD §9.3). A gap here leaks other teams' data. Every rule in this
> document is a **MUST**. A violation blocks code review approval without
> discussion.

## 1. The single rule

> Every request handler that touches board-scoped data **MUST** resolve
> `(principal, boardId) → BoardAccessContext` **before the first data access**,
> via the shared authorization service. There is no second path.

Handlers **MUST NOT** make their own authorization decisions. They *ask*
Identity & Access. Only Identity & Access may decide.

## 2. `BoardAccessContext` is a capability token

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

- The constructor **MUST** remain `internal`. No module outside Identity &
  Access may construct one.
- Only `IBoardAuthorizationService.ResolveAsync(principal, boardId)` mints one.
- Every board-scoped repository method and mutation service **MUST** take
  `BoardAccessContext` as its **first parameter**.

The point: "I forgot to authorize" becomes a **compile error**, not a code
review miss. Authorization that relies on remembering to call a function will
eventually be forgotten. Make the correct path the only compilable path.

## 3. The four enforcement layers

| # | Layer | Mechanism |
|---|---|---|
| 1 | Transport | Authenticated principal resolved from the Auth0 bearer token |
| 2 | Application | Endpoint filter resolves `BoardAccessContext` before any handler logic |
| 3 | Role | `RequireAdministrator()` on protected operations |
| 4 | Data | EF Core global query filter on `board_id` + `deleted_at IS NULL` |

Layers 2 and 3 are the policy. **Layer 4 is the net** — a handler that somehow
forgets the filter still cannot cross a board boundary. Layer 4 is not a
substitute for layers 2–3 and **MUST NOT** be treated as one.

Additionally, board-boundary integrity is enforced *structurally* in the schema
by composite foreign keys — see
[EF_CORE_AND_MIGRATIONS.md §3](./EF_CORE_AND_MIGRATIONS.md). That turns a whole
class of authorization bug into a constraint violation.

## 4. Never confirm existence — 404, not 403

- No membership, or `status != 'active'` → **`404 Not Found`**.
- Any board-scoped id not visible in the caller's scope → **`404 Not Found`**.
- The response body **MUST NOT** discriminate between "does not exist" and "you
  may not see it". No distinguishing message, no distinguishing `type` URI, and
  no observable timing difference beyond normal variance.

`403 Forbidden` is used **only** when the caller is a confirmed active member
whose *role* is insufficient — i.e. `RequireAdministrator()` failing. At that
point board existence is already known to the caller, so there is nothing left
to leak.

## 5. Authorization decisions are never cached

Board membership changes must take effect **on the caller's very next request**.

- **MUST NOT** push board roles into Auth0 roles, Auth0 permissions, or token
  claims. A token claim is stale the moment it is issued; a demoted
  administrator would retain rights until their next token refresh.
- **MUST NOT** cache the outcome of an authorization check.
- Authorization is a database read on every protected request. At our scale
  (HLD §1.6) this is immaterial, and it is the correct trade.

Auth0 authenticates and tells us *who* the user is. The `board_members` table
decides *what they may do*. That split is non-negotiable.

### 5.1 Offline writes are unauthorized until they reach the server

A client may cache *membership* as a rendering hint. It **MUST NOT** cache an
authorization *decision*. A user who goes offline, is demoted, and returns will
have their queued protected mutations rejected server-side, and the UI must
handle that gracefully (HLD §3.6).

## 6. Real-time is a protected resource

- SignalR group join for `board:{boardId}` **MUST** be authorized server-side
  against `board_members`. The hub is not a broadcast bus.
- A removed or demoted member's connection **MUST** be evicted from the group at
  the moment of the membership change (HLD §3.4, PRD §6.2).
- Hub payloads are **thin by default** — `{ entity, id, sequence, changedFields }`
  — so the hub cannot leak fields a recipient is not entitled to. Fat payloads
  are an optimisation requiring a fresh authorization analysis, not a default.

## 7. The development stub

`StubBoardAuthorizationService` exists only until `server-rbac` ships.

- It **MUST** fail fast at startup unless **both** `ASPNETCORE_ENVIRONMENT=Development`
  **and** `Board:AllowStubAuthorization=true` are set.
- It **MUST NOT** be reachable in `staging` or `production` by any configuration.
- "No stub auth outside Development" is a **named, non-negotiable code review
  gate**. (`board-data-model` R-2 — rated Critical.)

## 8. Code review checklist

- [ ] Every new board-scoped handler takes `BoardAccessContext` as the first parameter
- [ ] No handler constructs a `BoardAccessContext` or bypasses `IBoardAuthorizationService`
- [ ] No new repository method reads or writes board-scoped data without the context
- [ ] Protected/structural operations call `RequireAdministrator()`
- [ ] Non-member and inactive-member paths return `404` with a non-discriminating body
- [ ] No role or permission has been added to a token claim or cached
- [ ] New entities implement `IBoardScoped` and are covered by the global query filter
- [ ] Any new SignalR group join is authorized server-side
- [ ] Stub authorization remains Development-gated

Adversarial testing of every protected operation against every role and
non-member is a **requirement** — its design is owned by `/write-tests`.
