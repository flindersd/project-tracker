<!-- gate:hld status=COMPLETE at=2026-10-11T12:00:00Z -->

# High-Level Design — Initiative Tracker (Backend-Authoritative)

| Field | Value |
|---|---|
| Source spec | `docs/specs/PRD.md` (gate:prd status=COMPLETE — verified) |
| Mode | New Product Inception (Step 2) — **full rewrite following architecture pivot** |
| Supersedes | HLD v1 (2026-10-11, pure client-side / Sheets-as-source-of-truth) |
| Author | Full Stack Tech Lead |
| Document date | 2026-10-11 (decisions locked 2026-10-11) |
| Status | **COMPLETE — CONFIRMED.** All cost flags (CF-1..CF-5) and all open decisions (D-OPEN-1..D-OPEN-4) have been signed off by the product owner. No outstanding blockers to implementation. |

### Product Owner Sign-Off Record (2026-10-11)

| Item | Decision |
|---|---|
| **CF-1 / CF-2 — Railway + Postgres + Auth0 cost** | ✅ **APPROVED.** Budget envelope confirmed: **~$15–30/month pilot → ~$35–75/month production.** |
| **D-OPEN-1 / OQ-1 — Sheets scope** | ✅ **RESOLVED: export-only for MVP.** No import, no two-way sync in MVP. |
| **D-OPEN-2 — Repo visibility** | ✅ **RESOLVED: private repository.** |
| **D-OPEN-3 — Availability target** | ✅ **RESOLVED: 99.5% monthly for MVP/launch** (not 99.9%). **CF-4 (Redis backplane) is therefore NOT required for MVP launch** — deferred to TD-1. |
| **D-OPEN-4 — Delivery approach** | ✅ **RESOLVED: staged M1 → M2 → M3** (~5 + 5 + 4 weeks). Big-bang rejected. |
| **OQ-4 (part) — Comment threading** | ✅ **RESOLVED: single-level threading, no @mentions in MVP.** |
| **OQ-5 — "my team"** | ✅ **RESOLVED in the PRD:** a user-selected list of board members saved on the View. |
| **OQ-7 — Retention / compliance** | ⚠️ **MVP default proposed and adopted** (see §9.1) — still requires product/legal confirmation before public launch. Not a build blocker. |

---

## 0. Spec Sufficiency Check

`gate:prd status=COMPLETE` is **present** in `docs/specs/PRD.md` (both at the head of the document, line 3, and at the foot, line 262). The PRD is sufficient to proceed.

| Step 1 prerequisite | Status |
|---|---|
| Underlying problem clearly stated | ✅ §1 — explicit "Underlying problem" paragraph |
| Personas | ✅ §2 — three personas with distinct needs |
| Core conceptual entities | ✅ §6.1 — 13 entities with required fields |
| MVP defined | ✅ §3.3 in-scope / §3.4 out-of-scope |
| Full-scale capabilities | ✅ §3.4, §9 (P1/P2 feature plan) |
| Scale characteristics | ⚠️ **Partial** — "small-team board" is qualitative; PRD §7.4 and OQ-6 explicitly delegate concrete targets to the Tech Lead. **Resolved here**: §1.6 proposes measurable targets for PO confirmation. This is a delegation, not a gap, so the spec is not returned to the BA. |
| NFRs | ✅ §7 — responsive/accessible (§7.1), offline/reliability (§7.2), privacy/security (§7.3), performance (§7.4, deferred to TL) |
| Constraints | ✅ §8.1 dependencies, §7.3 security/privacy constraints |
| Success definition | ✅ §3.2 with measurable metrics |

**NFRs confirmed: yes.** Performance, availability, board-size and latency targets were *deliberately delegated* to this document by PRD §7.4 and OQ-6. They are set out in §1.6 and have been **confirmed by the product owner** (D-OPEN-3 resolved — availability target 99.5%).

**Open questions resolved by this HLD:** OQ-1 (Sheets scope — export-only, PO confirmed), OQ-2 (backend/database/hosting/recovery), OQ-3 (real-time transport, concurrency, offline reconciliation), OQ-4 (comment rules — single-level threading, no @mentions, PO confirmed), OQ-5 ("my team" — user-selected list, resolved in the PRD), OQ-6 (§1.6 targets confirmed). **Remaining:** OQ-7 (retention/compliance) — an MVP-appropriate default is adopted in §9.1 and is sufficient to build against; final product/legal confirmation is a **pre-launch** item, not a build blocker.

---

## 0.1 What the Pivot Invalidated — Read This First

The previous HLD designed a **client-side, Google-Sheets-as-source-of-truth** system. The PRD has pivoted to a **backend-authoritative hybrid**. This is not a patch; roughly 70% of the prior design is void. For transparency, here is exactly what was invalidated and why.

| Prior HLD decision | Status | Why |
|---|---|---|
| **D-1: Google Sheets is the durable source of truth** | ❌ **INVALIDATED** | PRD FR-3 now makes the application backend the authoritative persistence and authorization layer. Sheets is demoted to a portability/integration surface (PRD §3.3, FR-16). |
| **D-2: Bespoke field-level LWW CRDT over an append-only op log, Lamport clocks, ~800 LOC of custom convergence code** | ❌ **INVALIDATED** | The CRDT existed *only* because there was no authoritative server to arbitrate. With a real database and a single serialization point, this entire subsystem is unnecessary. Replaced by server-authoritative optimistic concurrency with field-level merge (§3). **This is the single largest simplification of the pivot — it removes the project's highest technical risk.** |
| **D-3: Transient, non-persisting, in-memory-only SignalR relay** | ⚠️ **SUBSTANTIALLY REVISED** | SignalR survives as the transport, but the "must not persist anything" constraint is gone. It is now a normal application hub, publishing authorized state changes from the backend (§3.4). The elaborate ring-buffer catch-up is replaced by a simple server-side change-sequence cursor. |
| **D-4: Multi-tab normalised Google Sheet schema with hidden `_ChangeLog` tab, tombstones, sheet-level compaction** | ❌ **INVALIDATED** | There is no board spreadsheet schema anymore. Replaced by a relational PostgreSQL schema (§2). The Sheets *export* mapping is a far simpler, one-directional concern (§6). |
| **D-5: Drive sharing is the access gate; board roles are a UI guardrail, not a security boundary** | ❌ **INVALIDATED — and this is the most important change** | PRD FR-9 now explicitly states "A client-side UI guardrail alone shall not satisfy this requirement." Board membership and roles are now real, server-enforced access control backed by a `board_members` table (§4). The prior HLD's "honesty constraint" (B-5) — telling users that roles were only advisory — is withdrawn. This was the weakest point of the old architecture and the pivot fixes it properly. |
| **D-6: `drive.file` scope + Google Picker + metadata-only registry for board discovery** | ❌ **INVALIDATED** | Board discovery is now a trivial `SELECT ... FROM board_members WHERE user_id = @me` (PRD FR-8). No Picker, no Drive scope, no registry workaround. Google Drive scope is needed *only* for the optional Sheets export feature, and only at the moment the user invokes it. |
| **D-7: Single C# service, three modules (Auth / Registry / Relay), no BFF** | ✅ **RETAINED, re-scoped** | Still one C# service, no BFF. The modules change: Registry is gone; Board, Collaboration, Activity and Integration modules replace it (§4). |
| **D-8: Google OAuth directly, not Auth0** | ❌ **REVERSED** | The old rationale was "single IdP, and we need direct control of the incremental Drive token." Both legs collapse: the backend is now authoritative so auth must broaden beyond Google-only users, and Drive scope is now an optional per-feature connector, not the core access path. **Auth0 is back as the recommendation** (§5.2). |
| **D-9: Monorepo with an isolated, pure `board-core` TS package** | ⚠️ **PARTLY RETAINED** | Monorepo retained. `board-core` as a *CRDT engine* is deleted. A much smaller shared `contracts` package survives for DTO/event-envelope types. |
| **D-10: Staged M0/M1/M2 MVP** | ✅ **RETAINED, re-planned** | Staging is still right. The milestones are re-cut in §1.2 because the risk profile has changed completely. |
| **§1.6 performance targets** | ✅ **RETAINED at 99.5%** | This HLD initially proposed raising availability from 99.5% to 99.9%. **The PO has confirmed 99.5% for MVP/launch** (D-OPEN-3). The targets otherwise stand as written in §1.6. 99.5% keeps single-replica deployment viable and defers the Redis backplane cost (CF-4 → TD-1). |
| **TD-1..TD-10 tech debt specs** | ⚠️ **MOSTLY VOID** | TD-2 (alternative durable store), TD-4 (Yjs for descriptions over CRDT), TD-5 (`drive.readonly` + CASA assessment), TD-8 (`_ChangeLog` compaction) are void. TD-1 (Redis backplane), TD-3 (now reversed — Auth0 is the default), TD-6 (virtualisation), TD-7 (service split), TD-9 (Resend), TD-10 (dev standards) survive in revised form (§8.2). |

**Net effect of the pivot on delivery risk:** materially *lower*. The old design's dominant risk was a bespoke distributed-systems algorithm with no safety net. That risk is gone. The new dominant risks are conventional and well-understood: operational reliability of a hosted service, correctness of server-side authorization, and the privacy/compliance obligations of centrally storing user content.

---

## 1. MVP Strategy

### 1.1 The biggest risk now

The old biggest risk ("can we build conflict-free collaboration on top of a spreadsheet?") is dissolved. The new architecture is conventional, which is the point. The remaining risks, in order:

1. **Product risk, not technical.** We are now building a hosted kanban tool in a crowded market. The differentiator is the configurable mixed-column model (initiative-only columns alongside initiative+task columns with swimlanes). If that model is not comprehensible to users — especially on mobile — the product has no reason to exist. **This is now the number-one thing to de-risk, and it is a UX risk.**
2. **Authorization correctness.** FR-9 and §7.3 demand that every mutation is authorized server-side. Getting this subtly wrong leaks other teams' data. It is well-understood work but it is unforgiving.
3. **Real-time + offline reconciliation UX.** The algorithm is now easy; making the *states* legible to users (pending / stale / failed / reconciled) is the real work, and it is mostly design.
4. **Compliance posture.** We now hold user content centrally. PRD §8.2 flags this; OQ-7 is unresolved. This is a launch blocker, not a build blocker.

### 1.2 What we build first — staged MVP

The PRD's P0 list is roughly 13–18 weeks for a small team. Shipping it as one milestone means learning nothing for four months. I recommend three milestones.

> **SR-1 (scope recommendation — ✅ CONFIRMED by the product owner, D-OPEN-4 resolved):** the MVP is staged as M1 → M2 → M3 below. Big-bang delivery of the full P0 list was considered and rejected.

| Milestone | Scope | Est. | What it proves |
|---|---|---|---|
| **M1 — Authoritative board, single-session** | Auth + sign-in; board CRUD; server-enforced membership and roles; configurable columns (both types), sub-stages, initiatives, swimlanes; tasks with all MVP fields and multiple assignees; drag-and-drop + accessible move; responsive layout; CSV/XLSX export. No real-time, no comments, no offline. Changes appear on refresh. | ~5 weeks | The domain model and the mixed-column UX. **Pilot this.** If the board model does not land, nothing downstream matters. |
| **M2 — Collaboration** | SignalR live sync; optimistic concurrency + field-level merge; comments; activity feed + in-app notifications; sync-state UX. | ~5 weeks | The collaboration guarantee and the notification signal-to-noise ratio. |
| **M3 — Resilience & portability** | Offline cache, pending-write queue, reconnect reconciliation; custom views with assignee filters; **Google Sheets export only** (no import, no two-way sync — D-OPEN-1 resolved). | ~4 weeks | Daily-driver robustness and the portability promise. |

**Total MVP: ~14 weeks across three milestones.** Each milestone is at or under the 5-week threshold. M1 at ~5 weeks is at the limit — see §8.1 for how its constituent features split.

### 1.3 Why this ordering

M1 deliberately ships **without** real-time. That looks wrong given FR-11, but it is correct: a single-session authoritative board is a genuinely usable product for a solo knowledge worker (the PRD's *primary* persona, §2.1), and it answers the number-one risk — is the mixed-column board comprehensible? — four weeks earlier than a big-bang delivery would. Adding SignalR to an already-correct backend-authoritative system is additive and low-risk; it does not require rework. That was emphatically *not* true in the old architecture, where the sync model dictated the storage schema.

Comments and notifications land in M2 rather than M1 because they are only interesting once more than one person is on a board, and the notification *rules* (OQ-4) are still open with the PO.

### 1.4 How we validate

- Pilot M1 with 3–5 small teams and 5–10 solo users.
- PRD §3.2 metrics, plus three technical metrics instrumented from M1 day one: p95 board load time by task count; observed concurrent-edit frequency (same board, same 30-second window); mobile vs desktop session share.
- M2 adds: notification volume per user per day (the leading indicator of the §8.2 "noisy notifications" risk), and conflict-rejection rate.

### 1.5 What we are deliberately deferring

| Deferred | Why |
|---|---|
| Google Sheets import and two-way sync | **Confirmed out of MVP** (D-OPEN-1 resolved — export only). Fast-follow if pilot demand appears. See §6 and TD-12. |
| Live cursors / presence (PRD P2) | Polish. The channel exists after M2; the feature is cheap to add later and adds nothing to validation. |
| Email / push / Slack notifications (PRD §3.4) | Explicitly out of scope. In-app only. Resend is the known answer when this arrives (§8.2, TD-7). |
| Event sourcing / full audit replay | The `activity_events` table gives us an append-only trail sufficient for the feed and for forensics. Full event sourcing is a large commitment for no MVP benefit. |
| Microservice split | One service. See §4. |
| Server-side rendering / Next.js | A board behind auth gains nothing from SSR. Plain React SPA. |
| Read replicas, caching layer, CQRS | No. At MVP scale a single Postgres instance is several orders of magnitude over-provisioned. Triggers documented in §8.2. |
| Multi-region / data residency | Single region. Becomes a real question under OQ-7 if EU residency is required — flag it early if so. |

### 1.6 Performance, scale and availability targets (resolves OQ-6 — ✅ **CONFIRMED by the product owner**, D-OPEN-3 resolved)

These are the numbers the architecture below is designed to meet.

| Target | MVP value | Note |
|---|---|---|
| Max board size | 30 initiatives, 2,000 tasks, 12 columns, 8 sub-stages per column | Higher than the old Sheets-bound limits — a relational DB removes the constraint |
| Max concurrent editors per board | 15 |
| Boards per workspace / user | 50 |
| Total pilot users | 500 registered, ~50 daily active |
| Board load, p95 (2,000 tasks, warm) | < 1.5 s to interactive |
| Board load, p95 (cached/offline) | < 0.8 s to interactive |
| Local interaction (drag, open task, filter) | < 100 ms — optimistic, never blocked on network |
| Write confirmation, p95 | < 400 ms server round trip |
| Real-time propagation to other clients, p95 | < 750 ms from commit to peer render |
| API read p95 / p99 | < 250 ms / < 600 ms |
| **Availability (service)** | **99.5% monthly** | **Confirmed by the PO for MVP/launch** (≈3.6 hours/month budget). Deliberately *not* 99.9%: at 99.5% a single API replica is acceptable, so the Redis backplane (CF-4) is **not required for MVP** and its cost is deferred. See TD-1 for the trigger that reverses this. |
| RPO (max data loss) | ≤ 5 minutes | Met by Postgres point-in-time recovery |
| RTO (max restore time) | ≤ 4 hours | Documented, rehearsed restore runbook |
| Browsers | Current and previous major of Chrome, Edge, Safari, Firefox; iOS Safari, Android Chrome |

**Note on 99.5% and what it buys us:** ≈3.6 hours/month of error budget is comfortably achievable on a single-region, single-replica Railway deployment with disciplined deploys. This is the right MVP posture: it avoids the Redis backplane cost (CF-4) and the operational complexity of multi-replica SignalR, while remaining honest about what a pilot service can deliver. **No contractual SLA is published at MVP** — 99.5% is a target, not a promise. Offering an SLA, or raising the target to 99.9%, triggers TD-1 (Redis backplane + multi-replica, ~$5–15/mo) and eventually TD-10 (multi-region). Data *safety* is handled independently of availability by PITR and the rehearsed restore runbook (§7.3) — an outage degrades access, it does not risk data.

---

## 2. Conceptual Data Model

PostgreSQL. The PRD §6.1 entity set is sound and maps almost directly onto a normalised relational schema — which is itself evidence the pivot is the right call.

### 2.1 Entities

| Entity | Key fields | Tech lead notes |
|---|---|---|
| **User** | `id`, `auth_subject`, `email`, `display_name`, `avatar_url`, `created_at`, `last_seen_at` | `auth_subject` is the IdP subject claim (unique, indexed). One row per human, provider-agnostic. Users are global; board access is via `board_members`. |
| **Board** | `id`, `name`, `created_by`, `created_at`, `updated_at`, `deleted_at?` | Soft delete. All board-scoped tables carry `board_id` — see §2.4. |
| **BoardMember** | `board_id`, `user_id`, `role`, `status`, `invited_by`, `created_at`, `updated_at` | **PK `(board_id, user_id)`. This table IS the authorization model.** `role` ∈ `BoardAdministrator` \| `User`. `status` ∈ `active` \| `invited` \| `removed`. Every authorization check reads this. |
| **BoardColumn** | `id`, `board_id`, `name`, `sort_order`, `column_type`, `created_at`, `updated_at` | `column_type` ∈ `initiative_only` \| `initiative_task`. |
| **TaskSubStage** | `id`, `board_id`, `parent_column_id`, `name`, `sort_order` | DB constraint: parent column must be `initiative_task`. Enforced by a trigger or by an application invariant with a check in the service layer — see §2.3. |
| **Initiative** | `id`, `board_id`, `column_id`, `title`, `description?`, `sort_order`, `created_at`, `updated_at`, `deleted_at?` | |
| **Task** | `id`, `board_id`, `initiative_id`, `task_sub_stage_id?`, `title`, `description`, `due_date?`, `priority`, `sort_order`, `created_at`, `updated_at`, `deleted_at?`, **`row_version`** | `row_version` drives optimistic concurrency (§3.2). `task_sub_stage_id` nullable to support the dormancy rule (§2.3). |
| **TaskAssignee** | `task_id`, `user_id`, `assigned_at`, `assigned_by` | Join table. PK `(task_id, user_id)`. Deliberately a table, not an array column — it is queried (`assigned to me` views) and it is an event source (assignment notifications). |
| **Tag** | `id`, `board_id`, `name`, `color?` | |
| **TaskTag** | `task_id`, `tag_id` | Join table. |
| **View** | `id`, `board_id`, `owner_user_id`, `scope`, `name`, `filters` (jsonb), `display` (jsonb), `created_at`, `updated_at` | `scope` ∈ `personal` \| `shared`. **Personal views are creatable by any board member; shared views are Board-Administrator-only.** This resolves the PRD FR-7 ambiguity ("personal versus shared-view authority must be defined") — carried forward from the prior HLD, which got this right. |
| **Comment** | `id`, `board_id`, `target_type`, `target_id`, `parent_comment_id?`, `author_user_id`, `body`, `created_at`, `updated_at`, `deleted_at?` | `target_type` ∈ `task` \| `initiative`. See §2.5 for the threading model. Soft delete preserves thread structure. |
| **ActivityEvent** | `id`, `board_id`, `event_type`, `actor_user_id`, `target_type`, `target_id`, `metadata` (jsonb), `created_at`, `sequence` | **Append-only. Never updated, never hard-deleted.** `sequence` is a board-scoped monotonic counter — this is the backbone of both the feed and the real-time catch-up cursor (§3.4). |
| **Notification** | `id`, `board_id`, `recipient_user_id`, `activity_event_id`, `event_type`, `read_at?`, `created_at` | A per-recipient projection of an activity event. Unique index on `(recipient_user_id, activity_event_id)` gives idempotent fan-out — this is what satisfies the PRD §3.2 "no duplicate notifications" metric mechanically rather than by discipline. |
| **SheetsConnection** | `id`, `board_id`, `created_by`, `spreadsheet_id`, `direction`, `mapping_version`, `last_sync_at?`, `last_sync_status`, `credential_ref` | Integration metadata only. `credential_ref` points at an encrypted token record; **no OAuth token is stored in plaintext and none is stored in this table**. |

### 2.2 Relationships

```
User 1──* BoardMember *──1 Board
Board 1──* BoardColumn 1──* TaskSubStage
Board 1──* Initiative      (Initiative *──1 BoardColumn)
Initiative 1──* Task       (Task *──1 TaskSubStage)
Task 1──* TaskAssignee *──1 User
Task *──* Tag  (via TaskTag)
Board 1──* View            (View *──1 User as owner)
Board 1──* Comment         (Comment *──1 User as author; Comment *──1 Comment as parent)
Board 1──* ActivityEvent 1──* Notification *──1 User as recipient
Board 1──0..1 SheetsConnection
```

**IDs are UUIDv7** (time-ordered). Rationale: generated client-side so optimistic UI and offline task creation get a permanent identity immediately with no coordination; time-ordered so they index well in Postgres as primary keys, avoiding the random-UUID B-tree fragmentation problem. The prior HLD used ULIDs for the same reason — UUIDv7 is the better choice here purely because it is a native `uuid` column type in Postgres with first-class EF Core support, which ULID-as-string is not.

### 2.3 Integrity rules

**The one real structural rule** (unchanged in substance from the prior HLD, because it is a domain rule, not an architecture rule): a task's `task_sub_stage_id` must resolve to a sub-stage whose `parent_column_id` equals its initiative's `column_id`. It breaks when an admin moves an initiative between columns or changes a column's type.

- Initiative moved between two `initiative_task` columns → tasks remap to the destination sub-stage **at the same ordinal position**, falling back to the first.
- Initiative moved into an `initiative_only` column → `task_sub_stage_id` is preserved in a `dormant_sub_stage_id` shadow column and the live value set null. Tasks are not displayed at sub-stage level and are **not destroyed**; they are restored if the initiative moves back.
- Column type changed `initiative_task` → `initiative_only` while holding tasks → admin is warned; same dormancy rule.
- Sub-stage deleted while holding tasks → **blocked** until the admin nominates a destination. This one is genuinely destructive, so prevent rather than resolve.

This satisfies FR-2's "prevent or explicitly resolve". The principle: **dormancy over deletion, everywhere. Never silently destroy user work.**

**Ordering.** `sort_order` is a **fractional index** (lexicographic string, LexoRank-style), not an integer. Reason: inserting between two neighbours is a single-row update rather than renumbering the whole column, which matters enormously under concurrent drag-and-drop and under offline replay. Periodic rebalance when key length exceeds a threshold. This is one of the few prior-HLD decisions that survives the pivot entirely intact, because the reasoning never depended on Sheets.

**Soft deletes** on Board, Initiative, Task, Comment. Hard deletes on join tables and on `board_members` status changes. Reason: undo, audit trail integrity, and the fact that activity events reference targets that must remain resolvable.

### 2.4 Tenancy and the authorization boundary

Every board-scoped table carries `board_id` **directly**, even where it is derivable (e.g. `task.board_id` is derivable through `initiative_id`). This is deliberate denormalisation with one purpose: **every authorization check and every query filter is a single predicate on `board_id`, with no joins to get there.** It makes the security-critical path trivial to read, trivial to test, and trivial to review. A redundant column is cheap; a missed join in an authorization filter is a data breach.

Enforcement is layered:
1. **Transport:** authenticated principal resolved from the bearer token.
2. **Application:** every request handler resolves `(user_id, board_id) → BoardMember` before touching data. No membership, or `status != active` → 404 (not 403 — do not confirm the existence of boards the caller cannot see).
3. **Role:** protected operations additionally require `role = BoardAdministrator`.
4. **Data:** repository-level global query filter on `board_id`, so a handler that forgets the filter still cannot cross boards.

Layer 4 is the net. Layers 2 and 3 are the policy. See §4.2.

### 2.5 Comments model (resolves OQ-4 — ✅ **CONFIRMED by the product owner**)

**Decision: single-level threading, no @mentions in MVP.** A comment targets a task or an initiative. A reply sets `parent_comment_id` to a top-level comment. **Replies to replies are flattened to the same top-level parent.** 

Why: arbitrary-depth threads are a UI problem on mobile (which is a first-class target per FR-17), they complicate read-state and notification fan-out, and in a tool of this size the conversation per task is typically under ten messages. Single-level gives the "reply to a specific point" affordance that users actually want, at a fraction of the complexity. Slack and Linear both landed here for the same reasons.

**Edit/delete policy** (confirmed with OQ-4): author may edit their own comment indefinitely with an "edited" marker; author or Board Administrator may soft-delete, which replaces the body with a tombstone marker and preserves thread position. **@mentions are confirmed out of MVP** — they imply a notification rule, an autocomplete UI, and a parsing/storage format, and the PRD does not require them. Flagged as the most likely first fast-follow request.

---

## 3. Collaboration, Concurrency and Offline (resolves OQ-3)

This is where the pivot pays off. The decision first, then the reasoning.

### 3.1 Decision

> **PostgreSQL is the single authoritative serialization point. Concurrency is handled by optimistic concurrency control with a per-row version token plus server-side field-level merge of non-overlapping changes. Real-time fan-out is ASP.NET Core SignalR over WebSockets, driven by a transactional outbox so that a client is never told about a change that did not commit. Offline is an IndexedDB cache plus a pending-mutation queue that replays through the exact same API and the exact same merge rules as an online write. There is no CRDT, no operational transform, and no distributed consensus anywhere in this system.**

| Layer | Role | Technology |
|---|---|---|
| Authority | Durable store, serialization point, authorization | PostgreSQL (Railway managed) |
| Concurrency | Lost-update prevention, field-level merge | `row_version` token + `UPDATE ... WHERE row_version = @expected` |
| Change capture | Reliable "what changed" stream | Transactional outbox → `activity_events` with board-scoped `sequence` |
| Real-time | Sub-second fan-out to peers | SignalR WebSocket hub, one group per board |
| Catch-up | Reconnect without full reload | Client holds last-seen `sequence`; server replays the delta |
| Local durability | Offline read + pending writes | IndexedDB |

### 3.2 Why this, and not a CRDT

The prior HLD needed a CRDT because there was **no authority** — every client was a peer writing to a dumb store that could not arbitrate. That is a genuinely hard distributed-systems problem and it demanded a genuinely complex solution.

With an authoritative database, that problem does not exist. Postgres serializes writes. There is exactly one order of events and it is the order the database commits them in. Reaching for a CRDT now would be solving a problem we no longer have, at the cost of several thousand lines of code that must never have a bug.

| Option | Verdict |
|---|---|
| **Optimistic concurrency + field-level merge** (recommended) | ✅ Boring, well-understood, supported natively by EF Core, trivially testable, and sufficient for structured relational data where genuine same-field collisions are rare. |
| **Pessimistic locking / explicit record checkout** | ❌ Rejected. Terrible UX on a kanban board (locks held by someone who closed their laptop), needs lock timeouts and lock-stealing UI, and solves a problem that OCC already solves. Reconsider only for a future rich-text field. |
| **CRDT (Yjs / Automerge) over the DB** | ❌ Rejected for MVP. Buys character-level merge of concurrent *text* editing. We have no collaborative text editing — task descriptions are short and single-author in practice. Costs a binary state blob that does not map onto relational columns, defeating querying, reporting, and the Sheets export. **Revisit trigger: if collaborative rich-text editing of descriptions or comments is ever required, adopt Yjs for that single field only — not for the document.** |
| **Operational Transform** | ❌ Rejected. All the cost of a CRDT, plus a central transform server, plus a harder correctness proof. OT is for text. |
| **Last-write-wins, whole record** | ❌ Rejected. FR-12 explicitly forbids silent loss, and whole-record LWW is exactly how you lose the due date someone set while you were dragging the card. |
| **Managed realtime DB (Firebase / Supabase Realtime)** | ❌ Rejected. Would mean a second datastore alongside Postgres or replacing it, fragments the C# backend, and the authorization model would live in two places. SignalR is native to the stack and free. |

### 3.3 How concurrency actually works

Every mutable entity carries `row_version` (a `bigint`, or Postgres `xmin` mapped via EF Core — prefer an explicit column for clarity).

1. Client reads a task; it holds `row_version = 7`.
2. Client submits a **field-scoped patch**: only the fields the user actually touched, plus `expected_row_version = 7`. This is the critical design point — the client sends `{ "due_date": "..." }`, never the whole entity.
3. Server executes `UPDATE tasks SET ... , row_version = row_version + 1 WHERE id = @id AND row_version = 7`.
4. **0 rows affected → someone else got there first.** The server re-reads the current row and compares *field sets*:
   - **Disjoint fields** (they changed `task_sub_stage_id`, I changed `due_date`): **auto-merge.** Apply my patch against the current version and retry once. Both changes survive. This is the overwhelmingly common real-world case on a kanban board and it resolves invisibly and correctly.
   - **Overlapping fields** (we both set `title`): **reject with `409 Conflict`**, returning the current server value, the rejected value, and the identity of the other actor. The client surfaces a non-destructive "Sam changed this to X while you were editing — keep yours / keep theirs" prompt. **Nothing is discarded without the user seeing it**, which is precisely what FR-12 demands.
5. Retry is bounded (one automatic retry, then surface). Never loop.

**Moves are a special case worth calling out.** Dragging a task changes `task_sub_stage_id` and `sort_order` together. Two users dragging the same card to different places is a genuine overlapping conflict — last commit wins the position and the loser is told. Two users dragging *different* cards into the same sub-stage is not a conflict at all: fractional indexing means they write different `sort_order` values on different rows with no contention. This is why fractional indexing earns its place.

### 3.4 Real-time delivery

**Transactional outbox, then SignalR.** The sequence for every mutation:

```
BEGIN
  authorize (board_members)
  apply mutation (with row_version check)
  INSERT INTO activity_events (..., sequence = nextval per board)
  INSERT INTO notifications (one row per computed recipient)   -- idempotent, unique index
COMMIT
  → publish to SignalR group "board:{board_id}"
```

The activity event and the notifications are written **inside the same transaction** as the data change. Publication to SignalR happens **after** commit. This ordering is the whole point: it is impossible to notify a peer about a change that was rolled back, and it is impossible to commit a change that produces no activity record. The feed is therefore complete by construction rather than by diligence.

**Hub design:**
- One SignalR group per board: `board:{boardId}`.
- **Group join is authorized server-side** against `board_members` — the hub is a protected resource, not a broadcast bus. A removed member's connection is evicted from the group on role/membership change (PRD §6.2: "removed users must not receive or access subsequent board content").
- Messages carry the board `sequence` number. Clients track `last_seen_sequence`.
- **Reconnect/catch-up is trivial:** client sends its `last_seen_sequence`; server replays `activity_events` beyond it (capped — beyond ~500 events behind, instruct a full reload). This replaces the prior HLD's in-memory ring buffer with something durable, simpler, and already needed for the activity feed. One mechanism, two features.
- Payloads are **thin by default**: `{ entity, id, sequence, changed_fields }`. The client re-fetches the entity if it is in view. Rationale: avoids leaking fields the recipient may not be entitled to, keeps the hub decoupled from DTO versioning, and keeps messages small. Fat payloads are an optimisation to apply later if measurement justifies it.

**Degradation:** if the WebSocket drops, SignalR falls back to Server-Sent Events then long-polling automatically. If all transports fail, the client polls `GET /boards/{id}/events?since={sequence}` on a backoff. The UI shows "reconnecting" and the board remains fully usable for reads. **Real-time is an enhancement over a correct request/response API, never a prerequisite for correctness.**

### 3.5 Notifications and the activity feed

Two distinct concepts, deliberately separated:

- **ActivityEvent** — board-scoped, append-only, "what happened". One row per change. Powers the board activity feed and the audit trail.
- **Notification** — user-scoped, "what happened *that you should care about*". Zero or more rows derived from one activity event.

The **recipient resolution rules** for MVP (per PRD FR-15), computed in the same transaction:

| Event | Recipients |
|---|---|
| User assigned to a task | The newly assigned user(s), excluding the actor |
| Task sub-stage / status changed | Current assignees of that task, excluding the actor |
| Comment created on a task or initiative | Assignees of the target + authors of other comments in the same thread, excluding the actor |

**Always exclude the actor.** Never notify someone about their own action. Idempotency is structural, via the unique index on `(recipient_user_id, activity_event_id)` — a retried publish cannot double-notify.

**Delivery for MVP is in-app only.** The feed is a paged query; unread count is a `COUNT(*) WHERE read_at IS NULL`; live badge updates ride the existing SignalR channel on a per-user group `user:{userId}`. **No email, no web push, no third-party notification service.** PRD §3.4 puts external channels out of scope, so there is no notification vendor cost at MVP — this is a deliberate avoidance of a cost flag, not an oversight.

**On notification noise** (PRD §8.2 risk): the rules above are intentionally narrow. The single most important early metric is notifications-per-user-per-day. If the M2 pilot shows it above ~15, the fix is per-board mute and per-event-type preferences, not broader rules. Build the measurement in M2; build the controls only if the measurement demands it.

### 3.6 Offline (FR-13, §7.2)

- **IndexedDB** holds the last successfully loaded board (entities + `last_seen_sequence`) and a **pending-mutation queue** of field-scoped patches.
- Offline edits apply optimistically to local state and enqueue. New entities get a client-generated UUIDv7, so they have permanent identity before the server ever sees them — no ID reconciliation, no temp-ID remapping.
- **On reconnect:** fetch events since `last_seen_sequence` → apply remote changes to local state → replay the pending queue through the **normal API**, in order. Each replayed patch goes through the same `row_version` check and the same merge rules as a live write. A patch that now conflicts surfaces in a **reconciliation review** — "3 of your 12 offline changes need your attention" — rather than being silently dropped or silently applied.
- There is deliberately **no separate offline merge algorithm**. Replay is the online path. One code path, one set of tests, one place for a bug to hide. This was the strongest property of the prior design and it survives the pivot — for better reasons.
- **Required UI states** (map directly onto FR-10/FR-11/§7.2): `synced` · `syncing` · `offline (last synced HH:MM)` · `pending (n)` · `failed (n — retry)` · `reconciled (n changes to review)` · `reconnecting`. These are a design-system concern and are flagged to the UX stage as a first-class component, not an afterthought.
- **Offline writes are optimistic and therefore unauthorized until they reach the server.** A User who goes offline, is demoted, and comes back will have their queued protected mutations **rejected server-side**. The UI must handle this gracefully. Authorization is never cached as a decision — only membership is cached, as a hint for rendering.

---

## 4. Key Services

Right-sized for MVP: **one React SPA, one ASP.NET Core service, one Postgres database.** Splitting further now would be premature — but the module seams below are drawn so that a later split is a deployment change, not a rewrite.

### 4.1 Frontend — React SPA

**Owns:** board rendering and interaction; optimistic local state; IndexedDB cache and pending queue; SignalR client; export file generation (client-side).

**Does NOT:** hold secrets; make authorization decisions (it renders affordances based on role, but the server decides); talk to Google APIs directly for board data.

| Concern | Choice | Note |
|---|---|---|
| Framework | React 18+, TypeScript strict, Vite | Stack default |
| Routing | React Router | |
| Server state | TanStack Query | **Now the primary data layer** — the old architecture had a CRDT document owning board state; that is gone, and a normal query cache with optimistic updates is exactly right |
| UI state | Zustand | Board viewport, drag state, panel state |
| Drag and drop | `dnd-kit` | Actively maintained; built-in keyboard sensor delivers FR-6's accessible non-drag alternative. Not `react-beautiful-dnd` (unmaintained). |
| Realtime | `@microsoft/signalr` | |
| Offline storage | `idb` | Thin IndexedDB wrapper |
| Virtualisation | `@tanstack/react-virtual` | Apply only if pilot boards exceed ~300 visible cards |
| Export | `papaparse` + `exceljs` | Client-side CSV/XLSX; no server round trip, no server-side file handling |
| PWA | `vite-plugin-pwa` | Service worker for offline shell + installability |
| Forms/validation | `react-hook-form` + `zod` | `zod` schemas shared with the contracts package |

### 4.2 Backend — single ASP.NET Core service

ASP.NET Core (latest LTS), C#, EF Core against Postgres. **Modular monolith** — one deployable, five internally bounded modules with explicit interfaces and no cross-module data access except through those interfaces.

| Module | Responsibility | Explicit boundary |
|---|---|---|
| **Identity & Access** | Token validation, principal resolution, `board_members` CRUD, role administration, the central authorization service | **The only module permitted to make an authorization decision.** Every other module calls it. Owns no board content. |
| **Board** | Boards, columns, sub-stages, initiatives, tasks, assignees, tags, views. The structural integrity rules of §2.3. | Does not decide authorization — it *asks*. Does not publish real-time messages — it emits domain events. |
| **Collaboration** | Comments; SignalR hub; group membership and eviction; the catch-up endpoint | Does not own task data. Consumes domain events. |
| **Activity & Notifications** | Activity event append; recipient resolution; notification fan-out; feed and unread queries | Write path is transactional with the originating mutation (§3.4). Never mutates board data. |
| **Integrations** | Sheets export; Google OAuth token handling for the connector; CSV/XLSX server fallback if ever needed | **Fully optional.** The service runs correctly with this module disabled. No other module may depend on it. |

**The authorization rule, stated once, enforced everywhere:**

> Every request handler that touches board-scoped data resolves `(principal, board_id) → BoardMember` **before** the first data access, via a single shared authorization service. Protected operations additionally assert `role = BoardAdministrator`. There is no second path. A handler that does not call it does not compile against the repository interface, because the repository requires a `BoardAccessContext` token that only the authorization service can mint.

That last clause matters: make the correct thing the *only* thing the type system permits. Authorization that relies on remembering to call a function will eventually be forgotten. Make it structurally unforgettable, and make "no handler bypasses `BoardAccessContext`" an explicit, non-negotiable code-review gate.

**Testing note:** authorization is the highest-risk surface in this system and it needs a dedicated, adversarial test suite covering every protected operation against every role and non-member. **I am flagging this as a requirement, not specifying the tests — test design is the tester's responsibility via `/write-tests`.**

### 4.3 No BFF

The SPA talks directly to the ASP.NET Core API. A BFF would add a hop, a deployment, and a DTO translation layer to solve a problem we do not have: there is one frontend, the API is already shaped for it, and auth is a bearer token. **Revisit trigger:** a second client (native mobile, public API) with materially different data-shaping needs.

### 4.4 Service interaction

```
┌───────────────────────┐     HTTPS / REST (JSON)      ┌──────────────────────────────┐
│                       │ ───────────────────────────► │   ASP.NET Core API           │
│     React SPA         │                              │  ┌────────────────────────┐  │
│  ┌─────────────────┐  │     SignalR (WebSocket)      │  │ Identity & Access      │  │
│  │ TanStack Query  │  │ ◄──────────────────────────► │  │ Board                  │  │
│  ├─────────────────┤  │                              │  │ Collaboration (Hub)    │  │
│  │ IndexedDB cache │  │                              │  │ Activity & Notif.      │  │
│  │ + pending queue │  │                              │  │ Integrations (opt.)    │  │
│  └─────────────────┘  │                              │  └───────────┬────────────┘  │
└───────────┬───────────┘                              └──────────────┼───────────────┘
            │ OIDC redirect                                           │
            ▼                                                         ▼
  ┌───────────────────┐                                   ┌───────────────────────┐
  │  Auth0            │                                   │   PostgreSQL          │
  │ (Google, MS,      │                                   │ (authoritative store) │
  │  email/password)  │                                   └───────────────────────┘
  └───────────────────┘                                               │
                                                                      │ optional, user-initiated
                                                                      ▼
                                                           ┌───────────────────────┐
                                                           │  Google Sheets API    │
                                                           │  (export / connector) │
                                                           └───────────────────────┘
```

---

## 5. Technology Recommendations

| Capability | Recommendation | Stack status | Cost | Revisit trigger |
|---|---|---|---|---|
| Frontend | React 18 + TypeScript + Vite | ✅ Default | Free | — |
| Backend | ASP.NET Core (latest LTS), C#, EF Core | ✅ Default | Free | — |
| BFF | **None** | Deviation (omission) | — | Second client with different data needs |
| **Database** | **PostgreSQL (Railway managed)** | ✅ Default-adjacent | **CF-1 ✅ APPROVED** | >100 GB, or sustained >70% CPU, or p99 reads >600 ms → read replica, then partitioning |
| Hosting | Railway | ✅ Default | **CF-1 ✅ APPROVED** | >500 concurrent connections, non-US/EU latency complaints, or an SLA commitment → multi-region |
| **Auth** | **Auth0** | ✅ **Default — reverting to it** | **CF-2 ✅ APPROVED** (free to 25k MAU) | >25,000 MAU, or enterprise SSO connections needed |
| Real-time transport | ASP.NET Core SignalR | ✅ Native to stack | Free | **Deferred (CF-4 → TD-1):** >~500 concurrent connections, >1 API replica, or availability target raised above 99.5% → Redis backplane |
| Concurrency control | OCC + field-level merge (own code, ~200 LOC) | New (own code) | Free | Collaborative rich-text required → Yjs for that field only |
| Ordering | Fractional indexing (LexoRank-style) | New (own code, ~100 LOC) | Free | — |
| Notifications (MVP) | **In-app only, own tables + SignalR** | — | **Free — no vendor** | External channels enter scope → Resend (**CF-5**) |
| Comments | **Own tables — no third-party service** | — | Free | — |
| Drag and drop | `dnd-kit` | New library | Free | — |
| Offline cache | IndexedDB via `idb` | New library | Free | — |
| Export | `papaparse` + `exceljs`, client-side | New libraries | Free | — |
| Sheets integration | Google Sheets API v4 + Google OAuth (connector scope only) — **export only** | New (required by product) | Free (quota-limited, not metered) | Import / two-way sync requested → TD-12 |
| CI/CD | GitHub Actions | ✅ Default | **CF-3 ✅ APPROVED** — private repo, 2,000 free minutes/mo | Sustained usage >~1,600 min/mo → upgrade plan or self-hosted runner |
| Error monitoring / APM | Sentry | New | **CF-3 ✅ APPROVED** (start on free Developer tier) | Event volume exceeds free tier |
| File/object storage | **None needed** | — | — | Attachments on tasks enter scope → Railway Object Storage |
| Payments | **None needed** | — | — | Monetisation enters scope |

### 5.1 Why PostgreSQL

Not a close call, but worth stating the reasoning rather than asserting it.

- **The data is relational.** Boards contain columns contain sub-stages; tasks belong to initiatives and sub-stages; members have roles; comments thread; notifications reference events. This is a textbook relational model with real referential integrity requirements (§2.3). A document store would mean hand-maintaining every one of those invariants in application code.
- **The access patterns are relational.** "Tasks assigned to me across my boards", "unread notifications", "comments on this task with authors" — these are joins. They are cheap in Postgres and painful anywhere else.
- **Transactional guarantees are load-bearing.** The outbox pattern in §3.4 — mutation, activity event, and notification fan-out committing atomically — is the mechanism that makes the feed complete and notifications non-duplicative. It requires a real multi-row ACID transaction.
- **`jsonb` covers the genuinely schemaless parts** (view filters, activity metadata) without a second datastore.
- **It is boring, it is managed on Railway, and EF Core support is first-class.** Minimal ongoing maintenance, which is the point.

Rejected: MySQL (no advantage, weaker `jsonb`); SQL Server (licence cost, no benefit here, heavier on Railway); MongoDB/DynamoDB (loses the transaction and the joins — the two things we actually need); SQLite (single-writer, not viable for a multi-user hosted service).

### 5.2 Why Auth0 — reversing the prior HLD's decision

The prior HLD argued *against* Auth0 and for self-managed Google OAuth. Both of its reasons were products of the old architecture and both have now collapsed:

1. *"The product requires Google OAuth specifically."* **No longer true.** Google sign-in existed because the user's data lived in their Google Drive — a Google identity was structurally required. The backend is now authoritative, so there is no reason to turn away a user without a Google account. Restricting sign-in to Google is now an arbitrary and costly limitation on the addressable market.
2. *"We need direct control of the incremental Drive/Sheets access token."* **Largely dissolved.** Drive/Sheets access is now needed only for the optional Sheets connector, invoked explicitly by a user who wants it. That is a standalone OAuth flow handled by the Integrations module, with its own token storage, entirely decoupled from sign-in. It does not need to be the authentication mechanism.

So: **Auth0 (stack default) for authentication, with Google, Microsoft and email/password connections enabled.** It gives us MFA, breach detection, password reset, account linking, and session management that we would otherwise build and maintain ourselves — all security-critical, all unglamorous, all things that go wrong quietly. The free tier covers 25,000 MAU, which is far beyond any plausible MVP (§1.6 targets 500 registered users).

**Authorization stays entirely ours.** Auth0 authenticates and tells us *who* the user is. The `board_members` table decides *what they may do*. We deliberately do **not** push board roles into Auth0 roles/permissions or into token claims: board membership changes frequently and must take effect immediately, whereas a token claim is stale the moment it is issued. A demoted administrator must lose administrator rights on their very next request, not on their next token refresh. **Authorization is a database read on every protected request. No caching of authorization decisions.**

The Google OAuth token for the Sheets connector is stored encrypted at rest, scoped to `https://www.googleapis.com/auth/drive.file` only (non-sensitive, no Google verification programme required), requested incrementally at the moment the user connects a sheet, and revocable from the app. This preserves the single genuinely good idea from the prior HLD's §4.4: **avoid restricted Drive scopes entirely, and therefore avoid the CASA security assessment and its multi-thousand-dollar annual fee.**

### 5.3 Cost flags — ✅ ALL APPROVED BY THE PRODUCT OWNER (2026-10-11)

**The hard stop is cleared. The product owner has approved an envelope of ~$15–30/month during pilot, rising to ~$35–75/month at production launch. Work may proceed. Any proposal that would take the project outside this envelope is a new cost flag and must be raised again.**

| Flag | Scope | Status |
|---|---|---|
| **CF-1** | Railway hosting + managed PostgreSQL | ✅ **APPROVED** |
| **CF-2** | Auth0 | ✅ **APPROVED** (start Free; Essentials at launch within envelope) |
| **CF-3** | GitHub Actions (private repo) + Sentry | ✅ **APPROVED** (both start free) |
| **CF-4** | Redis backplane for SignalR | ⏸️ **NOT REQUIRED FOR MVP** — deferred to TD-1 (see below) |
| **CF-5** | Resend transactional email | ⏸️ **NOT REQUIRED FOR MVP** — out of scope, recorded for planning only |

**CF-1 — Railway hosting + managed PostgreSQL. ✅ APPROVED.** *Verified at railway.com/pricing, 2026-10-11.*
- Plans: Free ($0, $1/mo credit, 1 vCPU / 0.5 GB, **1 replica**, 3-day logs); **Hobby ($5/mo inc. $5 usage credit, up to 48 vCPU / 48 GB, 6 replicas, 7-day logs)**; **Pro ($20/mo per workspace inc. $20 usage credit, unlimited seats, up to 42 replicas, 30-day logs)**; Enterprise (custom, adds contractual SLA and SSO).
- Usage rates: Memory $0.00000386/GB/s (≈$10/GB/mo); CPU $0.00000772/vCPU/s (≈$20/vCPU/mo); Volumes ≈$0.15/GB/mo; Egress $0.05/GB.
- **Recommendation: Hobby ($5/mo) for development and pilot; Pro ($20/mo) at production launch.** Pro is required at launch for the 30-day log retention (incident forensics on a system that now holds user data) and replica headroom.
- **Estimated all-in cost** (API container ~0.5 vCPU / 1 GB sustained, Postgres ~0.5 vCPU / 1 GB + 5–10 GB volume, modest egress):
  - Pilot: **~$15–30/month**
  - Early production (§1.6 scale): **~$35–60/month**
  - This is **higher than the prior architecture's ~$10–25/month**, which is the direct and expected cost of the pivot: we now run a real database, with backups, that must stay up.
- **The Free plan is not viable** — 1 replica and 3-day log retention are unacceptable for a production service holding user data.
- **Backups:** confirm Railway Postgres point-in-time-recovery retention on the chosen plan and, if it does not meet the §1.6 RPO of 5 minutes / RTO of 4 hours, budget for an independent nightly logical backup to object storage (adds ~$1–3/month at our data volume). **Treat an unrehearsed restore as equivalent to no backup** — a restore drill belongs in M1.

**CF-2 — Auth0. ✅ APPROVED.** *Verified at auth0.com/pricing, 2026-10-11.*
- **Free: $0/month, up to 25,000 MAU**, unlimited social connections, 1 custom domain, passwordless, passkeys, basic attack protection, community support. **This covers MVP and well beyond** (§1.6 targets 500 registered users).
- Next tier if exceeded: B2C **Essentials from $35/month at 500 MAU**, scaling by MAU tier (e.g. $700/mo at 10,000 MAU). Note the Free tier's 25,000 MAU allowance is *more* generous than the entry paid tiers' listed starting bands — the reason to upgrade is features (role management, log streaming, separate prod/dev environments, standard support), not volume.
- **Recommendation: start on Free.** Budget for Essentials (**+$35/mo minimum**) at production launch if separate production/development tenants or SLA-backed support are judged necessary. This is inside the approved envelope.
- ⚠️ Note for the PO: the Free tier has **1-day log retention and community support only**. For a product holding customer data, that is thin for incident response. I recommend budgeting for Essentials at production launch and treating Free as a pilot-only posture.

**CF-3 — Supporting services. ✅ APPROVED.**
- *GitHub Actions:* **Repository visibility is confirmed PRIVATE (D-OPEN-2 resolved).** Private repos on GitHub Free receive **2,000 Actions minutes/month**; GitHub Team receives 3,000. 
  - **Is 2,000 minutes sufficient?** For this project, **yes, with headroom — but it is a trigger to watch.** Estimating from the §7.2 pipeline (lint → typecheck → unit tests → integration tests with Testcontainers Postgres → build → deploy): a full run is realistically **8–12 minutes**, dominated by the Testcontainers integration suite and the dual .NET + Vite builds. At a small team's cadence of ~10–15 pipeline runs per working day (~250/month), that is **~2,000–3,000 minutes/month** — i.e. **at or slightly over the free allowance once the team is at full pace.**
  - **Mitigations, in order of preference, applied before upgrading:** (1) aggressive caching of NuGet and npm/pnpm stores; (2) run the full integration suite only on `main` and on PRs labelled for it, with a fast lint+unit gate on every push; (3) concurrency groups that cancel superseded runs on the same PR branch — this alone typically reclaims 20–30%; (4) do **not** enable PR preview environments by default (§7.2 already marks these optional).
  - **COST TRIGGER TO WATCH (new — TD-13):** if billed Actions minutes exceed **~1,600/month** (80% of allowance) for two consecutive months, escalate. Remedies are GitHub Team ($4/user/month) or a self-hosted runner on an existing Railway container (effectively free but adds maintenance). Overage on GitHub Free is **$0.008/minute for Linux** — an extra 1,000 minutes is ~$8/month, which is small but should not be allowed to drift unmonitored. **Monthly Actions-minutes consumption is an explicit operational metric from M1** (§7.3).
- *Sentry:* free Developer tier is adequate at MVP; paid Team tier starts around **$26/month**. **Starting on free, approved.** *Pricing not independently re-verified in this pass — confirm at purchase.*

**CF-4 — Redis backplane for SignalR. ⏸️ NOT REQUIRED FOR MVP — DEFERRED.** With the availability target confirmed at **99.5%** (D-OPEN-3), the tension that made this a launch-time decision is gone. A single API replica is an acceptable posture inside a ≈3.6 hour/month error budget: deploys are brief and schedulable, and data safety is guaranteed independently by PITR rather than by replica count. **Decision: ship MVP on a single API replica with no Redis.** Railway Redis would add roughly **$5–15/month** when it arrives.
- **Future trigger (TD-1) — revisit immediately if any of these occur:** the availability target is formally raised above 99.5% or a contractual SLA is offered; concurrent SignalR connections exceed ~400; a second API replica is needed for any reason (load, zero-downtime deploys, or background work); or observed deploy-window disruption generates user complaints. **This cost is pre-approved in principle inside the production envelope — it needs a go-ahead, not a fresh budget conversation.**

**CF-5 — Resend. ⏸️ NOT REQUIRED FOR MVP.** External notification channels are out of MVP scope (PRD §3.4), so **no email vendor is needed now**. For planning: *verified at resend.com/pricing, 2026-10-11* — Free $0/mo for 3,000 emails/month (100/day limit, 30-day retention); **Pro $20/mo for 50,000 emails/month**, $0.90 per additional 1,000. When external notifications enter scope (TD-7), the free tier will likely cover early volume. No approval needed; recorded so the PO is not surprised later.

**Approved MVP cost envelope: ~$15–30/month during pilot, rising to ~$35–75/month at production launch.** The production upper bound assumes Railway Pro + Auth0 Essentials + Sentry Team. With CF-4 deferred, the realistic production figure sits in the **lower half** of that range (~$35–50/month). Everything else in the stack is free or already owned.

### 5.4 Explicitly rejected

Firebase / Supabase (second datastore, fragmented authorization, fragmented stack); Ably / Pusher (paid, for something SignalR does natively and free); Next.js (no SSR benefit behind auth); Kafka / RabbitMQ / Azure Service Bus (an in-process outbox + SignalR covers every MVP need; message infrastructure for a single service is pure overhead); GraphQL (API surface is ~25 endpoints, REST is clearer and the team knows it); Elasticsearch (Postgres full-text search is more than adequate at this scale); Kubernetes (absolutely not); CRDT/OT libraries (§3.2); `drive.readonly` or any restricted Google scope (triggers CASA assessment cost); MongoDB/DynamoDB (§5.1).

---

## 6. Google Sheets Integration — Scope (resolves OQ-1 / D-OPEN-1 — ✅ **CONFIRMED**)

> **Decision: Google Sheets *export* only is in MVP (M3). Sheets *import* and *ongoing two-way sync* are explicitly OUT of MVP** and are recorded as TD-12 with a demand-led trigger. **This is confirmed by the product owner — it is no longer a recommendation.**

Reasoning:

- **Export is cheap and delivers the whole portability promise.** It is a read of authoritative data, a mapping, and one Sheets API write. It satisfies FR-16's MVP requirement and the §3.2 portability metric. Roughly 1 week including the OAuth connector flow. Ships alongside CSV/XLSX, which share the same mapping layer.
- **Import is moderately expensive and has unbounded edge cases.** Arbitrary user spreadsheets mean validation, column mapping UI, type coercion, partial-failure reporting, and duplicate detection. Realistically 2–3 weeks to do *properly*, and done improperly it corrupts the authoritative store — which is now a far more serious failure mode than it was when Sheets *was* the store.
- **Two-way sync is genuinely expensive and architecturally hazardous.** It reintroduces, in the worst possible form, the exact problem the pivot just eliminated: two writable sources with no shared conflict model. Google Sheets has no row-level change feed and no per-cell versioning; reconciling "someone edited row 14 by hand" against "someone dragged a card" requires a bespoke mapping-and-conflict subsystem, a conflict UI, and ongoing operational attention. **4–6 weeks, and it owns a permanent share of the maintenance budget thereafter.**
- **Critically: there is no evidence anyone wants it yet.** The PRD itself (OQ-1) is uncertain. The old architecture *forced* deep Sheets coupling; the new one does not. Do not pay for two-way sync before a single pilot user has asked for it.

**Consequence of the decision.** The MVP Sheets feature is now a **single, small, one-directional feature** — mapping authoritative data to a generated sheet layout and writing it via one Sheets API call, sharing the mapping layer with CSV/XLSX export. It is comfortably a **S/M feature (~1 week)** and **no longer needs splitting** (§8.1 amended accordingly). The `sheets-import-sync` feature is removed from the MVP feature list entirely and becomes TD-12.

**If two-way sync is ever revisited (TD-12):** it must be scoped as its own feature with its own spec, strictly bounded (one sheet per board, a fixed generated layout, app-wins-on-conflict with explicit user notification, no free-form user layouts), and budgeted at 4–6 weeks plus a permanent share of the maintenance budget. L-5 in §9.2 is the evidence gate.

---

## 7. Repo, Deployment, and Operations

### 7.1 Repo: single monorepo

```
project-tracker/
├── apps/
│   ├── web/                   # React SPA
│   └── api/                   # ASP.NET Core
│       ├── Modules/
│       │   ├── IdentityAccess/
│       │   ├── Board/
│       │   ├── Collaboration/
│       │   ├── Activity/
│       │   └── Integrations/
│       └── Migrations/
├── packages/
│   └── contracts/             # Shared DTO + SignalR event envelope types
│                              # (TS source of truth; C# mirrored with a contract test)
├── docs/specs/
└── .github/workflows/
```

**Rationale:** two deployables, one small team, and a shared API/event contract that changes with nearly every feature. Separate repos would mean a cross-repo PR for most changes — pure friction at this size. Monorepo gives atomic contract changes and one pipeline. **Revisit trigger:** team exceeds ~8 engineers, or frontend and backend release cadences genuinely diverge.

Note what is *gone*: the `board-core` package. It existed to isolate the CRDT. There is no CRDT. The remaining shared code is types.

### 7.2 Deployment on Railway

| Environment | Services | Notes |
|---|---|---|
| `production` | `web` (static SPA + CDN), `api` (container), `postgres` | Single region. **Single `api` replica — confirmed acceptable at the 99.5% availability target. Redis backplane (CF-4/TD-1) deferred.** |
| `staging` | Same three, smaller | Deployed from `main`. Separate Auth0 tenant/application and separate database. |
| PR previews | `api` + ephemeral Postgres | **Disabled by default.** Enable per-PR only when genuinely needed — they consume both Railway plan credit and GitHub Actions minutes (CF-3). |

- `web` is a static build: long-cache hashed assets, no-cache `index.html`.
- **Database migrations run as an explicit, gated pipeline step before the API deploys**, never automatically on application startup. Startup migrations plus multiple replicas is a well-known way to corrupt a schema. **All migrations must be backward-compatible with the previously deployed API version** — expand/contract, always.
- Secrets (Auth0 client secret, DB connection string, Google client secret, data-encryption key) live in Railway environment variables. **Never in the SPA bundle.** The SPA holds only the Auth0 domain and public client ID.
- Health endpoints: `/healthz/live` (process) and `/healthz/ready` (DB connectivity + migration state).
- CI: lint → typecheck → unit tests → integration tests (Postgres via Testcontainers) → build → deploy staging on `main` → promote to production on tag. **The repository is private (D-OPEN-2), so Actions minutes are metered against a 2,000 min/month allowance** — apply dependency caching and run-cancellation concurrency groups from day one (CF-3).

### 7.3 Operations — new obligations created by the pivot

The old architecture had almost no operational surface; a relay outage degraded the product but could not lose data. **That is no longer true, and this section is the main new cost of the pivot.**

| Obligation | Requirement |
|---|---|
| **Backups** | Railway Postgres PITR enabled and **verified**. RPO ≤ 5 min, RTO ≤ 4 h (§1.6). A **documented, rehearsed restore runbook is an M1 deliverable**, not a post-launch task. |
| **Monitoring** | Uptime check on `/healthz/ready`; alerting on error rate, p99 latency, DB connection saturation, SignalR connection count, and failed-login rate. **Track the 99.5% monthly availability budget explicitly** — if two consecutive months breach it, TD-1 is triggered. |
| **Cost & quota watch** | **Monthly tracking of (a) Railway spend against the approved $15–30 / $35–75 envelope and (b) GitHub Actions minutes against the 2,000/month private-repo allowance.** Escalate at 80% of either. This is a named owner's responsibility from M1, not an ad-hoc check. |
| **Logging** | Structured logs with correlation IDs. **Never log request bodies** — they contain user board content. 30-day retention (Railway Pro). |
| **Encryption** | TLS in transit (platform-provided). Encryption at rest for the database (platform-provided — **verify and document**). Application-level encryption for the Google OAuth refresh tokens specifically. |
| **Secrets** | Railway environment variables; documented rotation procedure for the Auth0 client secret, the Google client secret, and the data-encryption key. |
| **Data lifecycle** | Account deletion, board deletion, retention periods, and backup retention. **MVP default adopted (§9.1, OQ-7): data retained until the user or board is deleted; deletion request honoured within 30 days; backups rolled off at the Railway plan's PITR window; no compliance certifications claimed for the v1 pilot.** A soft-delete-only system with no purge path is a GDPR problem waiting to happen — TD-5 (hard-purge job) is a **launch** requirement if EU users are in scope. |
| **Audit** | `activity_events` is the application audit trail. Administrative access to production data must be separately logged and least-privilege (PRD §7.3). |

---

## 8. Instructions for the Business Analyst

### 8.1 Feature splitting — items exceeding the 5-week threshold

Three PRD features exceed or sit at the threshold and **must be split during feature specification** (`/split-feature`). This list is now **final** — the decisions in §0 have resolved the previously conditional entries.

**1. `realtime-collaboration` (PRD P0, marked XL)** — must split:

| Feature slug | Scope | Est. |
|---|---|---|
| `concurrency-control` | `row_version` OCC, field-scoped patch API, server-side field-level merge, 409 conflict contract, fractional indexing | M (~1.5 wks) |
| `realtime-transport` | SignalR hub, authorized group join/eviction, transactional outbox publication, `sequence` cursor, reconnect catch-up, transport degradation | M (~2 wks) |
| `offline-reconciliation` | IndexedDB cache, pending-mutation queue, reconnect replay, reconciliation review UX, full sync-state UI set | M (~2 wks) |

**2. `backend-foundation` (PRD P0, marked L)** — at the threshold; recommend splitting:

| Feature slug | Scope | Est. |
|---|---|---|
| `platform-foundation` | Railway project, Postgres provisioning, migration pipeline, CI/CD, health checks, logging, monitoring, backup + **rehearsed restore runbook** | M (~2 wks) |
| `auth-foundation` | Auth0 integration, token validation, user provisioning/linking, session handling, sign-in/sign-out UX | M (~1.5 wks) |

**3. `configurable-swimlane-board` (PRD P0, marked L)** — at the threshold; recommend splitting:

| Feature slug | Scope | Est. |
|---|---|---|
| `board-data-model` | Schema for boards/columns/sub-stages/initiatives/tasks, §2.3 integrity rules, fractional indexing, core CRUD API | M (~2 wks) |
| `board-rendering` | Responsive rendering of both column types, swimlanes, sub-stage grids, mobile interaction model | M (~2.5 wks) |

**`board-data-model` must be specified first.** Five other features depend on it.

**Now resolved and NOT requiring a split:**

| Feature | Status |
|---|---|
| `sheets-import-sync` | ❌ **Removed from MVP.** D-OPEN-1 resolved to export-only. Replaced in MVP by a single small **`sheets-export`** feature (S/M, ~1 week) that shares the CSV/XLSX mapping layer. **This is now the lowest-risk integration item in the plan** — one-directional, no conflict model, no user-supplied layouts. Import and two-way sync move entirely to TD-12. |
| `stage-management` (L) | ⚠️ **Re-estimate at specification time.** Previously flagged "also at risk". Nothing in the PO decisions changes its size, so treat the 5-week threshold as a live check when its spec is written; split if it breaches. |

### 8.2 Forward-looking tech debt specifications

Write these **now**, with triggers stated, so the team can act decisively rather than deferring indefinitely:

| Spec | Trigger |
|---|---|
| **TD-1: SignalR Redis backplane + multi-replica API** | **Availability target is formally raised above the confirmed 99.5%**, or a contractual SLA is offered, **or** concurrent connections exceed ~400, **or** the 99.5% budget is breached in two consecutive months. Cost: CF-4 (~$5–15/mo, pre-approved in principle). |
| **TD-2: Read replica / query optimisation** | Sustained DB CPU >70%, or API read p99 >600 ms |
| **TD-3: Notification preferences and muting** | Median notifications per user per day exceeds ~15 in pilot telemetry |
| **TD-4: Rate limiting and abuse protection** | Before public (non-invite) signup opens |
| **TD-5: Hard-delete / data purge job** | Before launch if EU users are in scope; otherwise when a deletion request is first received (**do not wait for this one**) |
| **TD-6: Board card virtualisation** | Pilot boards routinely exceed ~300 visible cards, or board render breaches §1.6 targets |
| **TD-7: Resend transactional email** | External notification channels enter scope (PRD P2 `external-notifications`). Cost: CF-5. |
| **TD-8: Module → service split** | Deploy cadence or scaling needs diverge across the §4.2 module boundaries |
| **TD-9: Yjs for collaborative rich text** | Collaborative rich-text editing of descriptions or comments is requested. Scope to that field only — never the document. |
| **TD-10: Multi-region / data residency** | A customer requires EU data residency, or a contractual SLA is offered |
| **TD-11: Development standards documentation** | **No `DEVELOPMENT_STANDARDS/` directory exists in this project.** Create one covering API conventions (naming, serialization, error contract), EF Core/migration conventions, TypeScript and C# style, and component patterns — **before the second developer joins.** The authorization pattern in §4.2 especially needs documenting as a hard requirement. |
| **TD-12: Google Sheets import / two-way sync** | Pilot users request it (L-5 evidence gate). **Confirmed out of MVP** — see §6 for why and what it costs. |
| **TD-13: CI minutes / cost containment** | Billed GitHub Actions minutes exceed ~1,600/month (80% of the private-repo free allowance) for two consecutive months, **or** Railway spend approaches the top of the approved envelope. Remedies in CF-3. |

### 8.3 PRD amendments to fold in

- Record the §1.6 performance, scale and availability targets as the formal NFR, replacing the PRD §7.4 placeholder. **The availability target is 99.5% (PO-confirmed) — do not record 99.9%.**
- Add `row_version` to the Task entity (and to any entity the feature specs make concurrently editable).
- Add `TaskAssignee` and `TaskTag` as explicit join entities (PRD §6.1 currently models these as arrays).
- Add `sequence` to the Activity event entity — it is load-bearing for both the feed and real-time catch-up.
- Record the View `scope` rule: personal views creatable by any member; shared views Board-Administrator-only (resolves the FR-7 ambiguity).
- Record the §2.5 comments decision (single-level threading, author edit, author/admin soft-delete, **no @mentions in MVP**) and **close OQ-4**.
- **Close OQ-1 as export-only**, replace the `sheets-import-sync` P0 feature with a `sheets-export` feature, and move import/two-way sync to the TD-12 backlog.
- **Close OQ-2, OQ-3, OQ-5 and OQ-6** against this document. **OQ-7 remains open as a pre-launch item only**, with the MVP default in §9.1 recorded as the working assumption.
- Record the staged M1/M2/M3 delivery plan (§1.2) as the agreed MVP roadmap.
- **Remove the prior HLD's "board roles are a UI guardrail, not a security boundary" constraint from all product copy and documentation.** It is no longer true and must not survive into user-facing material.

---

## 9. Open Questions and Risks

### 9.1 MVP blockers — ✅ ALL CLEARED

**There are no outstanding blockers to beginning implementation.** All items previously listed here have been resolved by the product owner.

| ID | Item | Resolution |
|---|---|---|
| **D-OPEN-1** | OQ-1: Sheets scope | ✅ **Export-only in MVP.** Import and two-way sync deferred to TD-12. (§6) |
| **D-OPEN-2** | Repository visibility | ✅ **Private.** 2,000 free Actions minutes/month; sufficient with caching and run-cancellation, watched via TD-13. (CF-3) |
| **D-OPEN-3** | §1.6 targets, especially availability | ✅ **99.5% confirmed for MVP/launch.** Single API replica is acceptable; **CF-4 Redis backplane deferred to TD-1.** (§1.6) |
| **D-OPEN-4** | SR-1: staged vs big-bang | ✅ **Staged M1 → M2 → M3** (~5 + 5 + 4 weeks). (§1.2) |
| **CF-1..CF-5** | Cost sign-off | ✅ **Approved. Envelope: ~$15–30/mo pilot → ~$35–75/mo production.** CF-4 and CF-5 deferred, not needed. (§5.3) |
| **OQ-4** | Comment rules | ✅ **Single-level threading; author edit with marker; author/admin soft-delete; no @mentions in MVP.** (§2.5) |
| **OQ-5** | What "my team" means | ✅ **Resolved in the PRD:** a user-selected list of board members saved on the View. No Google Groups. (§2.1 View entity) |

### 9.1a OQ-7 — retention, deletion and compliance (scoped, **pre-launch** not pre-build)

OQ-7 is the one item not closed outright. It does **not** block building, and it should not hold up `/design-system`. I am proposing a defensible MVP-appropriate default so the team has something concrete to build against:

> **Proposed MVP default (adopt unless product/legal says otherwise):**
> - **Retention:** user and board data retained for as long as the account and board exist. No automatic time-based expiry of board content.
> - **Deletion:** account deletion and board deletion are honoured as a **hard purge within 30 days** of request, covering the primary store. Backups age out naturally within the Railway PITR window and are not individually scrubbed — this is standard and defensible practice, and must be stated plainly in the privacy notice.
> - **Activity events:** retained indefinitely as the audit trail, but purged with the board on hard delete.
> - **Compliance posture:** **no certifications claimed for the v1 pilot** — no SOC 2, no ISO 27001, no HIPAA, no formal DPA programme. Market the pilot accordingly and do not accept customers whose procurement requires them.
> - **Geography:** single region. **If EU users are in scope, TD-5 (hard-purge job) becomes a hard launch requirement** and a GDPR-compliant privacy notice and DPA template are needed. Flag this the moment an EU pilot user appears.

**What still needs product/legal input before public launch** (not before build): confirmation of the target market's geography; whether any pilot customer's procurement demands certifications; and sign-off on the privacy notice and terms. **Owner: product owner + legal. Deadline: before the M3 pilot opens beyond invited users.** TD-5 is the engineering dependency.

**Remaining engineering setup tasks** (not decisions): Auth0 tenant setup, Google Cloud project + OAuth consent screen (connector only, `drive.file` scope), Railway project provisioning. These belong to `platform-foundation` / `auth-foundation` in M1.

### 9.2 Learning questions — answered through MVP validation

| ID | Question | How we learn |
|---|---|---|
| L-1 | Is the configurable mixed-column model comprehensible, especially on mobile? | M1 pilot. **This is now the number-one product risk** and is the primary input to the UX/design-system stage. |
| L-2 | How often do genuine concurrent edits occur? | M1/M2 telemetry. Low frequency validates OCC decisively; unexpectedly high frequency would prompt a look at per-field presence indicators (not a CRDT). |
| L-3 | What is the real notification volume per user per day? | M2 telemetry. Drives TD-3. |
| L-4 | Do real boards approach the §1.6 size limits? | M1 instrumentation of board size distribution. |
| L-5 | Do users actually want Sheets sync, or is export sufficient? | M3 pilot feedback. Directly determines whether TD-12 is ever built. |
| L-6 | Does offline mode get used at all? | M3 telemetry on offline session frequency. If near zero, it is a candidate for simplification rather than hardening. |

### 9.3 Technical risks

| Risk | Impact | Likelihood | Mitigation |
|---|---|---|---|
| **Server-side authorization has a gap** | **Critical** | Medium | §4.2 `BoardAccessContext` type-level enforcement; repository-level `board_id` global filter; explicit non-negotiable code-review gate; dedicated adversarial test suite (tester-owned). **This is now the highest-severity technical risk in the product.** |
| Database outage or data loss | **Critical** | Low | PITR verified, **rehearsed** restore runbook as an M1 deliverable, monitoring, documented RPO/RTO (§1.6). |
| Single API replica means deploys and crashes are visible outages | Medium | Medium | **Accepted for MVP.** The confirmed 99.5% target (≈3.6 h/month) absorbs this comfortably. Mitigate with scheduled, short deploys and health-gated rollout. Resolved by TD-1 if the target is ever raised. |
| Conflict UX is confusing and users distrust the board | Medium | Medium | Auto-merge disjoint fields so most conflicts are invisible; surface only genuine same-field collisions, non-destructively. Heavily UX-dependent — flag to the design stage. |
| Offline reconciliation produces surprising results after a long offline period | Medium | Medium | Bounded offline window; reconciliation review UI; replay through the identical online code path (§3.6). |
| Notification noise drives users to ignore the feed | Medium | Medium | Deliberately narrow MVP rules (§3.5); measure from day one; TD-3 ready. |
| Centrally stored user content creates undefined compliance obligations | High | **Medium** (default now adopted) | §9.1a MVP default adopted. OQ-7 final confirmation is a **pre-launch** gate owned by the product owner and legal; TD-5 is the engineering dependency. Still the most likely thing to delay *launch*, but no longer a build risk. |
| Scope creep into two-way Sheets sync reintroduces dual-authority complexity | Low | Low | **Largely retired.** D-OPEN-1 confirmed export-only; two-way sync is now a TD-12 backlog item behind an explicit demand gate (L-5). |
| GitHub Actions minutes exceed the private-repo free allowance | Low | Medium | Caching, run-cancellation concurrency groups, integration suite gated to `main`/labelled PRs, PR previews off by default. Monitored monthly; TD-13 trigger at 80%. Worst case ~$8/mo overage. |
| Mixed-column board model proves confusing on mobile | High | Medium | L-1; this is a UX risk to resolve at the design-system stage, not an architecture risk. |

---

## 10. Full-Scale Architecture (Future State)

A sanity check that the MVP does not paint us into a corner. Sketch only.

| Dimension | MVP | ~10x (5k users, 500 concurrent) | ~100x (50k+ users, teams of 50+) |
|---|---|---|---|
| API | One service, one replica, five modules | Same service, 3–5 replicas behind the platform LB | Modules split into 2–3 services along §4.2 seams (TD-8) |
| Real-time | In-memory SignalR groups, single replica (99.5%) | Redis backplane, multi-replica (TD-1) | Dedicated real-time service, sharded by board hash |
| Database | Single Postgres | Primary + read replica for feed/reporting queries (TD-2) | Partitioned by board or tenant; activity events to a time-series/columnar store |
| Concurrency | OCC + field-level merge | **Unchanged — this design scales fine** | Unchanged; possibly per-field presence to reduce collisions |
| Activity events | One table, retained indefinitely | Partitioned by month, archived beyond 12 months | Separate append store, feed served from a materialised projection |
| Notifications | In-app only, synchronous fan-out | Fan-out moved to a background worker off the outbox | Dedicated notification service, multi-channel (email/push/Slack) |
| Auth | Auth0 Free | Auth0 Essentials/Professional | Auth0 Enterprise with SSO + SCIM |
| Frontend | SPA + PWA | Same + virtualisation (TD-6) | Same; possibly a native shell |
| Region | Single | Single + CDN | Multi-region with data residency (TD-10) |

**The key structural insight:** every scaling step above is *additive*. The outbox pattern is already the seam where asynchronous processing slots in. The modules are already bounded with no cross-module data access. The concurrency model does not change at any scale. **Nothing in the MVP design needs to be undone to get to 100x** — which is exactly the property a good MVP architecture should have, and precisely what the previous Sheets-bound design could not offer (it had a hard ceiling at the spreadsheet).

The one commitment that is expensive to reverse is **Postgres as the authoritative store**. That is fine — it is the correct choice at every scale this product will plausibly reach.

---

## 11. Product Surface and Next Steps

### 11.1 Does the product have a UI?

**Yes.** This is a responsive web application (PRD §Platform, FR-17) with substantial, novel interface requirements:

- A configurable board with two distinct column types rendering side by side
- Initiative swimlanes spanning task sub-stage sub-columns
- Drag-and-drop **plus** an accessible non-drag alternative (FR-6, §7.1)
- Mobile, tablet and desktop layouts for every MVP workflow
- A full set of synchronisation-state indicators (§3.6) — a genuinely unusual component family that must be designed, not improvised
- Threaded comments and an activity/notification feed
- Non-destructive conflict-resolution prompts (§3.3)
- Role-differentiated affordances (administrator configuration vs. contributor editing)

### 11.2 Next step: `/design-system`

**Confirmed: the next stage is `/design-system`.** All technical decisions and cost approvals are now locked, so the design stage has a stable foundation to work against.

Important note on the "pivot, not first pass" question: **I checked, and `docs/` currently contains only `PRD.md` and `HLD.md`. No design system artefact exists.** So despite this being an architectural pivot, the design-system stage is a **genuine first pass**, not a re-check. Nothing needs to be invalidated there because nothing was produced.

Points to carry into the design stage:

1. **L-1 is the top product risk.** The mixed-column board model's comprehensibility — especially on mobile — is the thing most likely to kill this product. It deserves disproportionate design attention.
2. **Synchronisation and conflict states are first-class components**, not error toasts. The full state set is in §3.6 and the conflict interaction in §3.3.
3. **Accessibility is a hard requirement, not a polish item.** The non-drag alternative to drag-and-drop (FR-6) must be designed, not retrofitted.
4. **Roles are now real.** The UI should reflect genuine capability differences between Board Administrator and User — and unlike the prior architecture, those affordances are now backed by real server-side enforcement, so the design can state them confidently rather than hedging.
5. **Notification and activity feed design directly determines whether TD-3 is needed.** Good information hierarchy here reduces the need for preference controls later.
6. **Comment threading is settled: single level, no @mentions.** Design the reply affordance for exactly one level of nesting and do not design a mention autocomplete.

---

## 12. Summary of Decisions

| # | Decision | Rationale |
|---|---|---|
| D-1 | **PostgreSQL on Railway is the authoritative store** (resolves OQ-2) | The data and access patterns are relational; transactional guarantees are load-bearing for the outbox; boring and managed. |
| D-2 | **Optimistic concurrency + server-side field-level merge. No CRDT.** (resolves OQ-3) | With an authoritative serialization point, the distributed-consensus problem does not exist. Removes the prior design's largest risk and several thousand lines of bespoke code. |
| D-3 | **SignalR over a transactional outbox for real-time** | Native to the C# stack, free, and the outbox makes "never notify about an uncommitted change" structural rather than disciplinary. |
| D-4 | **Server-enforced authorization via `board_members`, with type-level enforcement** (FR-9) | Roles are now real access control. The prior HLD's "guardrail only" honesty constraint is withdrawn. Highest-severity risk, so make the correct path the only compilable path. |
| D-5 | **Auth0, with Google / Microsoft / email-password — reversing the prior decision** | Both old arguments against it collapsed with the pivot. Free to 25k MAU. Authentication from Auth0; authorization stays in our database, uncached. |
| D-6 | **Activity events are append-only with a board `sequence`; notifications are an idempotent per-recipient projection** | One mechanism serves the audit trail, the feed, and real-time catch-up. The unique index makes "no duplicates" structural. |
| D-7 | **Comments: single-level threading, no @mentions in MVP** ✅ *PO-confirmed* | Mobile-friendly, simpler read-state and fan-out, matches real conversation depth in a tool this size. |
| D-8 | **Sheets export only in MVP; import and two-way sync deferred to TD-12** ✅ *PO-confirmed (OQ-1 closed)* | Export delivers the whole portability promise cheaply. Two-way sync reintroduces dual-authority — the precise problem the pivot just removed — for unvalidated demand. |
| D-9 | **One ASP.NET Core modular monolith, no BFF, one React SPA** | Right-sized. Seams preserved for a later split without paying for it now. |
| D-10 | **Monorepo; `board-core` deleted, `contracts` retained** | The package existed to isolate the CRDT. No CRDT, no package. |
| D-11 | **Offline replays through the identical online API and merge rules** | One code path, one set of tests, one place for a bug to hide. The best property of the old design, preserved for better reasons. |
| D-12 | **Fractional indexing for `sort_order`** | Survives the pivot unchanged — concurrent reordering without renumbering is correct regardless of storage. |
| D-13 | **Availability target 99.5%; backup and rehearsed restore are M1 deliverables** ✅ *PO-confirmed (D-OPEN-3 closed)* | 99.5% (≈3.6 h/month) is the right MVP posture: it keeps a single API replica viable and defers the Redis backplane cost entirely. Data *safety* is handled independently of availability by PITR and a rehearsed restore — the restore drill is non-negotiable and must not be deferred. |
| D-14 | **Staged MVP delivery: M1 (~5 wks) → M2 (~5 wks) → M3 (~4 wks)** ✅ *PO-confirmed (D-OPEN-4 closed)* | Learn from a real pilot at week 5 rather than week 14. Each milestone is independently shippable and within the 5-week feature threshold. |
| D-15 | **Private repository; CI on GitHub Actions within the 2,000 min/month free allowance** ✅ *PO-confirmed (D-OPEN-2 closed)* | Sufficient with caching, run cancellation, and integration tests gated to `main`. Monitored monthly; TD-13 is the escalation path. |
| D-16 | **MVP data-retention default: retain until deletion, hard purge within 30 days, no compliance certifications claimed for v1** | Defensible, standard, and unblocks the build. Final product/legal confirmation is a pre-launch gate (§9.1a), not a build blocker. |

---

<!-- gate:hld status=COMPLETE at=2026-10-11T12:00:00Z -->
