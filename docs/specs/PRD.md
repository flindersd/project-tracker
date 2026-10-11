# Product Requirements Document — User-Owned Initiative Tracker

> **gate:prd status=COMPLETE**

| Field | Value |
|---|---|
| Product | User-Owned Initiative Tracker |
| Platform | Responsive web application |
| Status | Revised for hybrid, backend-authoritative MVP; ready for UX and revised technical guidance subject to open-question decisions |
| Primary data source (MVP) | Application backend database (authoritative); Google Workspace/Sheets is an optional import, export, or sync integration |
| Document date | 2026-10-11 |

## 1. Problem Statement

Individual knowledge workers and small teams need a lightweight way to track work across initiatives in configurable swimlanes and personal/team views. Current alternatives force an undesirable trade-off: Trello lacks the required board flexibility, Jira is too complex and IT-project-oriented, and spreadsheets do not provide dependable role enforcement, live collaboration, or an integrated activity trail.

**Underlying problem:** Users cannot efficiently coordinate and safely update initiative-based work in a simple, configurable shared workspace that provides trustworthy access control and real-time visibility while retaining practical data portability.

The product will provide a hosted kanban-style board backed by the application's authoritative backend database. It will support server-enforced Board Administrator/User roles, real-time shared updates, safe concurrent editing, comments, and relevant notifications. Google Workspace—especially Google Sheets—remains a user-controlled portability and interoperability option through export, import, or an optional sync connector; it is not the authoritative store.

## 2. Target Users and Personas

### 2.1 Primary persona — Individual knowledge worker

An individual manages personal work alongside work assigned by a small team. They need a personal view and a combined view of their own and team-assigned tasks, organized by initiative and workflow stage. They value low setup effort, mobile access, dependable collaboration, and the ability to take or connect their data to Sheets.

### 2.2 Secondary persona — Small-team contributor

A contributor works on a shared board with colleagues. They need to see shared tasks, tasks assigned to them, collaborators' changes, relevant discussion, and activity without adopting a complex enterprise project-management tool.

### 2.3 Supporting persona — Board/workspace configurator

The Board Administrator sets the board's initiative lifecycle, task sub-stages, swimlanes, membership roles, and shared views. They need to shape the board while the application reliably prevents ordinary contributors from altering protected structure.

## 3. Goals, Success Metrics, and Scope

### 3.1 Goals

1. Make initiative-based work easy to visualize and update through a configurable swimlane board in which each administrator-defined column can show initiatives alone or tasks organized into administrator-defined sub-stages within initiative swimlanes.
2. Provide secure, server-enforced Board Administrator and User access control for every protected board operation.
3. Support individual and small-team workflows through multiple assignees, real-time shared updates, conflict-safe concurrent editing, comments, and activity/notifications.
4. Preserve data portability and reduce lock-in through export and, subject to scope confirmation, Google Sheets import and/or connector-based synchronization.
5. Keep the product useful with intermittent connectivity through cached access, clear synchronization state, and safe reconciliation.

### 3.2 Success metrics

| Metric | Measurement | Initial target / baseline needed |
|---|---|---|
| Time saved tracking initiatives | Self-reported time spent tracking work before and after regular use; optionally validated in usability sessions | Demonstrable reduction versus each participant's current spreadsheet/tool workflow; baseline and numeric launch target to be defined |
| Core-workflow completion | Users open a board, create/update and move a task, add a comment, and use a filtered view | At least 80% of pilot users complete without facilitator help |
| Collaboration integrity | Simultaneous-edit test cases converge without lost updates; unauthorized protected actions are rejected by the server | 100% of agreed MVP concurrent-edit cases resolve without silent data loss; 100% of authorization tests reject disallowed operations |
| Collaboration awareness | Relevant assignments, task-status changes, and comments create visible activity/notifications | At least 95% of qualifying events are represented once, without duplicate notifications |
| Portability | Successful board-data export and, if included, Sheets import/sync operations | At least 95% of attempted supported operations succeed when the connector is reachable and authorized |

### 3.3 MVP scope — in

- Responsive board with an administrator-configurable ordered set of columns. For each column, the Board Administrator selects either **initiative-only** (initiatives shown as whole blocks, with no task-level sub-columns) or **initiative + task** (administrator-defined task sub-stage sub-columns, with initiative swimlanes running through them). The number, order, and names of columns and the sub-stages within each initiative + task column are configurable.
- A suggested, illustrative starting template is initiative-only **Backlog**, **Do Next**, and **Completed** columns plus an **In Progress** initiative + task column with To Do, In Progress, and Done sub-stages. This is not a required MVP layout or default column set.
- Authoritative application backend and database for boards, initiatives, tasks, configuration, membership, comments, notifications, and audit/activity events.
- Server-enforced board membership and roles: Board Administrator and User. Board Administrators manage protected configuration and roles; Users create and update tasks only within their board access. The API/backend must authorize every mutation; client UI restrictions are not the security boundary.
- Create and edit tasks with the defined task fields, including multiple assignees; drag-and-drop task sub-stage changes.
- Application sign-in, board discovery/listing for authorized members, and shared access managed through application membership.
- User-created custom board views, including assignee-based filtering such as “assigned to me” and “assigned to me + my team.”
- Real-time push synchronization and conflict-safe simultaneous editing for shared boards using the authoritative backend/database.
- Comments on tasks and initiatives, including threaded replies if supported by the selected UX; comment creation and visibility honor board authorization.
- In-app activity feed and notifications for relevant events at minimum: assignment to the user, task status/sub-stage change, and comments on a task relevant to the user. Notification delivery channels beyond in-app are out of scope unless separately specified.
- Offline access to cached/last-loaded data, visible synchronization state, and eventual reconciliation when connectivity returns.
- Manual CSV and XLSX export/download of board data.
- Google Sheets integration as a separately scoped portability feature: the product must support Sheets export in MVP; whether MVP also includes Sheets import and/or ongoing two-way sync is an open product-scope decision (OQ-1). Any connector is secondary to the backend authority.

### 3.4 Out of scope for MVP / future candidates

- Live cursors/presence indicators (fast-follow; real-time data updates and conflict-safe editing are MVP).
- Native mobile applications.
- Enterprise project-management capabilities such as sprints, issue types, reporting suites, workflow automation, or advanced notification rules.
- Email, push, Slack, or other external notification delivery channels.
- Local CSV/XLSX as a connected editable source; export/download is the MVP portability fallback.
- Treating Google Sheets, Drive, or any client cache as the authoritative board store.

## 4. Core User Journeys and User Stories

### 4.1 Core workflow

1. A user signs in and sees boards for which the application backend has authorized membership. They create a board or open an existing authorized board.
2. They see the Board Administrator's configured columns. Initiative-only columns show whole initiative blocks; initiative + task columns show configured task sub-stages with an initiative swimlane spanning those sub-columns.
3. A Board Administrator creates or edits initiatives, column names, column types, task sub-stages, membership roles, or shared views. A User creates or updates tasks. The backend authorizes and persists each permitted change.
4. A user drags a task between valid task sub-stages in its initiative + task column; its task status is updated, collaborators receive the change in real time, and relevant activity is recorded.
5. A collaborator comments on a task or initiative; relevant participants can view the comment and receive an in-app notification/activity entry.
6. They save a view such as “My work” or “My team and me” and switch to it when needed.
7. When connected, collaborators' changes synchronize in real time and concurrent changes resolve without silent data loss. While offline, users review and update cached board data; the product makes pending, failed, and reconciliation state clear on reconnection.
8. A user can export the board, and—if the optional connector scope is approved—import from or synchronize with a connected Google Sheet. Backend data remains authoritative in all cases.

### 4.2 User stories

| ID | User story | Acceptance summary |
|---|---|---|
| US-1 | As a knowledge worker, I want to discover boards I am authorized to access so that I can open the right board without a URL or ID. | After sign-in, I see authorized boards and can open one with load/synchronization status. |
| US-2 | As a board user, I want to see the administrator-configured initiative columns and, where applicable, tasks in initiative swimlanes across task sub-stages so that I can understand both initiative and task progress at a glance. | Each configured column renders according to its type and active-view filters. |
| US-3 | As a Board Administrator, I want to configure columns, their types, task sub-stages, initiatives, membership roles, and shared views so that the board reflects my workflow. | Permitted configuration persists through the backend; a non-administrator request is rejected server-side. |
| US-4 | As a contributor, I want to create and edit tasks with multiple assignees so that shared responsibility is visible. | All specified fields can be set and multiple assignees persist after reload. |
| US-5 | As a contributor, I want to move a task to another valid task sub-stage so that I can update progress quickly. | A valid move persists, synchronizes to collaborators, and indicates actionable failure if it cannot be saved. |
| US-6 | As a user, I want saved custom views so that I can focus on relevant work. | I can create, name, select, modify, and remove permitted views with assignee filtering at minimum. |
| US-7 | As a shared-board collaborator, I want changes to synchronize in real time and simultaneous edits to resolve safely so that we can work from a common source of truth. | Authorized users receive updates without refresh, and agreed concurrent-edit cases converge without silent data loss. |
| US-8 | As a collaborator, I want to comment on a task or initiative so that context and decisions stay with the work. | Authorized users can create and read comments; relevant participants see the activity and notification. |
| US-9 | As a user, I want an in-app notification/activity feed so that I know about changes relevant to me. | Assignments, status changes, and relevant comments generate attributable activity entries; I can distinguish unread items. |
| US-10 | As a user with intermittent connectivity, I want to use my last loaded board and understand synchronization state so that I am not blocked by being offline. | Cached data remains available; offline, stale, pending, failed, and reconciled states are visible. |
| US-11 | As a user, I want to export my board data and optionally connect it to Google Sheets so that I retain portability and can use familiar Workspace workflows. | CSV/XLSX export is available; any approved Sheets import/sync clearly states its direction, status, and conflict behavior. |
| US-12 | As a Board Administrator, I want protected configuration permissions distinct from task-editing permissions so that contributors cannot alter the board structure. | The backend enforces administrator-only protected operations and returns an authorization failure for disallowed requests. |

## 5. Functional Requirements

| ID | Requirement |
|---|---|
| FR-1 | The application shall provide a board with an administrator-configurable number, order, and name of columns. Each column shall be configured as either initiative-only or initiative + task. Initiative-only columns shall display initiatives as whole blocks. Initiative + task columns shall display administrator-configured task sub-stage sub-columns with initiative swimlanes running through them. |
| FR-2 | Board Administrators shall be able to create, rename, remove, reorder, and set the type of columns; configure task sub-stages; and create, edit, reorder, or remove initiatives/swimlanes, memberships, roles, and shared views. The product shall prevent or explicitly resolve removal or type changes of a structure element containing work before completing the action. |
| FR-3 | The backend shall be the authoritative persistence and authorization layer for all board content and mutations. Google Sheets, local caches, and exports shall not independently override backend-authoritative state. |
| FR-4 | The board shall display initiatives in their configured columns and tasks at the intersection of their initiative swimlane and configured task sub-stage, subject to active-view filters. |
| FR-5 | Authorized Users and Board Administrators shall be able to create and update tasks with title, description, one or more assignees, due date, priority, task sub-stage/status, and tags/labels. |
| FR-6 | Users shall be able to move a task between valid task sub-stages within its initiative + task column by drag and drop and by an accessible non-drag alternative; the task's `taskSubStageId` shall be updated. |
| FR-7 | Users shall be able to create, name, edit, select, and delete permitted custom views. MVP views shall support assignee-based filtering. Personal versus shared-view authority must be defined in the detailed feature specification. |
| FR-8 | The application shall support authenticated sign-in and list boards for which the signed-in user has active application membership. |
| FR-9 | The backend shall enforce two MVP board roles on every protected request: Board Administrator may manage board structure, initiatives, membership/roles, and shared configuration; User may create and update tasks but shall not change protected board configuration or roles. A client-side UI guardrail alone shall not satisfy this requirement. |
| FR-10 | When online and authorized, board, task, comment, role, and configuration changes shall persist through the backend and show success or actionable failure feedback. |
| FR-11 | The application shall synchronize authorized shared-board changes in real time when connected and clearly communicate connection, pending, failed, and reconciliation states. Refresh/reload may be provided as recovery but is not the collaboration mechanism. |
| FR-12 | The application shall use a conflict-resolution strategy over authoritative backend data that preserves agreed concurrent changes where possible and never silently loses a conflicting edit. The detailed strategy is a Tech Lead decision. |
| FR-13 | The application shall cache the last successfully loaded board locally and allow users to view and work with it while offline, exposing connection, freshness, and pending/failed-write status. |
| FR-14 | Authorized users shall be able to create and read comments on tasks and initiatives. Comments shall include stable identity, author, creation time, content, and parent target; reply/thread behavior must be specified by the comments feature. |
| FR-15 | The application shall record and present an in-app activity feed/notification experience for relevant events, including assignment to the recipient, task status/sub-stage changes, and comments on a task relevant to the recipient. Notifications shall identify the event, actor where appropriate, target, timestamp, and read/unread state. |
| FR-16 | The application shall allow users to export available board data in CSV and XLSX formats. It shall support Google Sheets export in MVP. If Sheets import or ongoing synchronization is approved, it shall use a documented mapping, explicit synchronization direction and status, and conflict/error guidance; it shall not replace backend authority. |
| FR-17 | The application shall support mobile, tablet, and desktop layouts for all MVP workflows. |

## 6. Data Model and Source-of-Truth Rules

The application's backend database is the authoritative source of truth for all live board data, access control, collaboration state, and activity. Every persisted entity shall have a stable application-generated ID. Google Sheets is a secondary integration surface: exported data, imported data, or mapped connector data must retain a traceable association with application IDs where applicable, but a Sheet is not authoritative over live product data. A protected local cache may support offline operation only.

### 6.1 Entities

| Entity | Required fields | Notes |
|---|---|---|
| Board | `id`, `name`, `createdBy`, `createdAt`, `updatedAt` | Authoritative board container. |
| Initiative | `id`, `boardId`, `title`, `description` (optional), `columnId`, `sortOrder` | An initiative is placed in an administrator-configured column. |
| Board column | `id`, `boardId`, `name`, `sortOrder`, `columnType` | `columnType` is `initiative-only` or `initiative+task`. |
| Task sub-stage | `id`, `parentColumnId`, `name`, `sortOrder` | A task-level status sub-column within an initiative + task column. |
| Task | `id`, `boardId`, `initiativeId`, `title`, `description`, `assigneeIds[]`, `dueDate`, `priority`, `taskSubStageId`, `tagIds[]`, `sortOrder`, audit timestamps/version | Tasks belong to an initiative and valid task sub-stage. |
| Person/User | `id`, `displayName`, `email` (where available), authentication identity reference | Represents an authenticated product user and may be assigned to tasks. |
| Board member | `boardId`, `userId`, `role`, membership status, audit timestamps | `role` is `Board Administrator` or `User`; this record drives backend authorization. |
| Tag/Label | `id`, `boardId`, `name`, `color` (optional) | Tasks can hold zero or more tags. |
| View | `id`, `boardId`, `ownerUserId`, `scope`, `name`, `filters`, `sort/display preferences` | At minimum supports assignee filtering. `scope` and authority rules require feature-level definition. |
| Comment | `id`, `boardId`, `targetType`, `targetId`, `authorUserId`, `body`, `parentCommentId` (optional), `createdAt`, `updatedAt` | Attached to a task or initiative; parent reference supports replies if threaded comments are included. |
| Notification | `id`, `boardId`, `recipientUserId`, `eventType`, `actorUserId` (optional), `targetType`, `targetId`, `activityId`, `readAt` (optional), `createdAt` | In-app recipient-facing notification. |
| Activity event | `id`, `boardId`, `eventType`, `actorUserId`, `targetType`, `targetId`, event metadata, `createdAt` | Auditable feed event and source for relevant notifications. |
| Sheets connection/mapping | `id`, `boardId`, connection metadata, direction, mapping/version, last sync status/time | Secondary connector metadata; detailed credentials, mapping, and sync policy are technical/product decisions. |

### 6.2 Data integrity and synchronization behavior

- A write is complete when the authoritative backend has accepted and persisted it; the UI must distinguish local/pending, successful, rejected, and failed states.
- The backend shall authorize mutations before persistence and publish authorized state changes to connected collaborators.
- Connected shared changes must be delivered in real time. Concurrent edits must converge through a validated strategy without silent data loss; the detailed conflict and offline merge model is a must-resolve technical decision.
- Offline edits must reconcile on reconnection without silently overwriting unconfirmed local or remote edits.
- Application IDs, not display text, row numbers, or third-party identifiers, are the stable identity for core entities. A Sheets connector shall map these IDs where feasible and report unmappable/conflicting data explicitly.
- Comments, activity events, and notifications must respect the board membership and authorization model; removed users must not receive or access subsequent board content.

## 7. Non-Functional Requirements

### 7.1 Responsive and accessible experience

- All MVP workflows shall be usable on current mobile, tablet, and desktop browsers.
- Mobile layouts may use horizontally scrollable columns or an equivalent mobile board interaction, but must preserve task viewing, editing, comments, and activity access.
- Keyboard and assistive-technology users must have an alternative to drag-and-drop for stage changes.

### 7.2 Offline-first and reliability

- The last successfully loaded authorized board must remain viewable without network connectivity, subject to secure local-session controls.
- The interface must clearly show last refresh time, online/offline status, and pending or failed writes.
- When connected, the product must show real-time synchronization state and must not imply synchronization where it is unavailable. Refresh/reload is a recovery aid, not the normal collaboration model.
- Availability, recovery objectives, supported board size, and real-time latency targets require technical validation before implementation.

### 7.3 Privacy, data ownership, and security

- The application shall operate a central backend database that stores authoritative board content, collaboration data, memberships/roles, comments, notifications, and required operational/audit data. Product documentation and privacy disclosures must state this clearly.
- Data in transit shall use industry-standard transport encryption; stored application data, backups, and secrets shall be protected with encryption at rest and appropriate key/secret management.
- Server-side authorization shall use least privilege, enforce board membership and roles on every protected operation, and prevent a User from bypassing role restrictions through direct API calls.
- Authentication credentials, session tokens, connector credentials, and locally cached content shall use platform-appropriate secure storage. Secrets must not be embedded in client code.
- Access to production data shall be limited to authorized operational personnel under least-privilege controls, with logging/auditing appropriate to the service and applicable legal/compliance obligations.
- Users shall be able to export their board data and disconnect a Sheets integration. Data retention, account/board deletion, backup retention, and applicable privacy/compliance commitments require definition before launch.
- Google access, if used, shall use least-privilege OAuth scopes compatible with the approved Sheets integration.

### 7.4 Performance

- Board loading, edits, comments, notifications, and interactions should feel responsive for a small-team board. Concrete supported board-size, concurrency, real-time propagation, and latency targets require technical validation before implementation.

## 8. Dependencies, Risks, Assumptions, and Open Questions

### 8.1 Dependencies

| Dependency | Type | Why it matters | Status / owner |
|---|---|---|---|
| Backend architecture, database choice, and data model | Hard | Required for authoritative persistence, stable IDs, access control, collaboration, comments, and notifications. | Must be resolved in revised HLD; Tech Lead |
| Hosting, deployment, monitoring, backup, and recovery design | Hard | A hosted authoritative service needs operational reliability, incident response, and data recovery. | Must be resolved before build/launch; Tech Lead + product owner |
| Authentication and server-side authorization design | Hard | Required to identify users and enforce Board Administrator/User permissions at the API/backend boundary. | Must be resolved before build; Tech Lead |
| Real-time transport and conflict/offline reconciliation strategy | Hard | Required for live collaboration and conflict-safe concurrent editing. | Must be resolved before build; Tech Lead |
| Security, privacy, retention, and applicable compliance decisions | Hard | The product now centrally stores user content and must set safeguards and obligations accordingly. | Must be resolved before launch; product owner + Tech Lead |
| Comment/activity/notification event model | Hard | Required to produce correct, authorized, non-duplicative collaboration awareness. | Must be specified; product owner + Tech Lead |
| Google OAuth configuration and Google Sheets connector/export design | Soft for backend MVP; Hard for approved Sheets scope | Required for Sheets export and for any approved import/two-way synchronization. | Scope decision pending OQ-1; product owner + Tech Lead |
| Client-side cache and secure local-storage design | Soft | Enables offline continuity; online MVP development can begin while the detailed cache design is finalized. | Must be designed before offline release; Tech Lead |

### 8.2 Risks

| Risk | Impact | Mitigation / owner |
|---|---|---|
| Backend outage or database failure interrupts collaborative work | High | Design availability, backups, restore testing, monitoring, and graceful offline behavior; Tech Lead |
| Inadequate authorization exposes board data or permits configuration changes | High | Server-side authorization tests, least privilege, security review, and audit logging; Tech Lead |
| Conflict handling loses or obscures a concurrent edit | High | Validate a conflict-resolution strategy with representative concurrent and offline cases; Tech Lead |
| Centralized storage creates privacy, retention, or compliance obligations that are not defined | High | Define data classification, retention/deletion, access controls, and privacy disclosures before launch; product owner + Tech Lead |
| Notification volume becomes noisy or duplicates events | Medium | Start with narrowly defined relevant events, recipient rules, idempotency, and read state; product owner + UX + Tech Lead |
| Sheets connector mapping or two-way sync creates confusing conflicts | Medium | Keep backend authority explicit; scope connector direction and conflict UX before build; product owner + Tech Lead |

### 8.3 Assumptions

| Assumption | Impact if wrong | Validation owner |
|---|---|---|
| Small teams will accept a conventional hosted backend in exchange for secure roles and deeper collaboration. | Positioning, privacy disclosures, or architecture may need revision. | Product owner |
| Backend-authoritative data can meet expected board size, real-time, and offline expectations at an acceptable operating cost. | Scale limits or architecture may need adjustment. | Tech Lead |
| Administrator-configured combinations of initiative-only and initiative + task columns remain understandable on mobile. | Interaction model or configuration breadth may need adjustment. | UX + Tech Lead |
| In-app notifications are sufficient for MVP. | Email/push or integrations may be required sooner. | Product owner |

### 8.4 Open questions

| ID | Question / decision needed | Recommendation / current assumption | Priority | Owner |
|---|---|---|---|---|
| OQ-1 | What exact Google Sheets scope belongs in MVP beyond required export: one-time import, export only, or ongoing one-way/two-way synchronization? | Deliver CSV/XLSX and Sheets export in MVP. Treat import and ongoing sync as a separate connector feature unless the product owner explicitly prioritizes it; if selected, define direction and conflict behavior before build. | Should resolve before build; must resolve before connector build | Product owner |
| OQ-2 | What backend/database, hosting, availability, backup/recovery, and operational-monitoring architecture will support the authoritative product data? | Tech Lead to provide a revised HLD; choose conventional managed services appropriate to MVP scale and security requirements. | Must resolve before build | Tech Lead |
| OQ-3 | What real-time transport, concurrency-control, and offline reconciliation strategy will be used over the authoritative backend database? | It must preserve agreed concurrent changes where possible and surface irreconcilable conflicts rather than silently losing data. | Must resolve before build | Tech Lead |
| OQ-4 | What are the exact comment rules: task and initiative comments only, threading/replies, edit/delete policy, mentions, and participant notification rules? | Start with task/initiative comments and relevant participant notifications; do not assume @mentions or external delivery. | Should resolve before comments feature build | Product owner + UX |
| OQ-5 | What does “my team” mean for view filtering: selected assignee list, application-defined team, Google Group, or another model? | Start with a user-selected list or application-defined grouping unless identity integration is explicitly required. | Should resolve before build | Product owner |
| OQ-6 | What target board size, concurrent editor count, supported browsers, uptime, and latency define acceptable MVP performance? | Tech Lead should propose measurable targets for product-owner approval in the revised HLD. | Must resolve before launch | Product owner + Tech Lead |
| OQ-7 | What retention, deletion, backup-retention, geography, and compliance requirements apply to centrally hosted content? | Define before launch; scope compliance commitments to the intended market. | Must resolve before launch | Product owner + legal/security + Tech Lead |

## 9. Prioritized Feature Plan

| Priority | Feature slug | Feature name | Description | Dependencies | Est. effort |
|---|---|---|---|---|---|
| P0 | backend-foundation | Authoritative Backend Foundation | Backend database, stable IDs, authentication/session foundation, board persistence, operational baseline, and migration approach. | Backend/database/hosting decisions | L |
| P0 | server-rbac | Server-Enforced Board Roles | Board membership, Board Administrator/User roles, API authorization, role administration, and auditability. | backend-foundation; auth design | M |
| P0 | configurable-swimlane-board | Configurable Swimlane Board | Responsive rendering of administrator-configured initiative-only and initiative + task columns, per-column task sub-stage grids, and initiative swimlanes. | backend-foundation; server-rbac | L |
| P0 | task-management | Task Creation and Editing | Create/edit tasks with all MVP fields, multiple assignees, and authorized persistence. | backend-foundation; configurable-swimlane-board | M |
| P0 | realtime-collaboration | Real-Time Sync & Conflict-Safe Editing | Backend-backed push updates, concurrent-edit handling, offline reconciliation, and sync-state UX. | backend-foundation; realtime/conflict decision | XL — split during feature specification |
| P0 | stage-management | Stage Changes & Board Configuration | Accessible task moves plus administrator configuration of columns/types, per-column sub-stages, and initiatives. | configurable-swimlane-board; server-rbac | L |
| P0 | comments | Task and Initiative Comments | Authorized comments, target association, author/timestamps, and approved reply/edit rules. | backend-foundation; server-rbac; task-management | M |
| P0 | activity-notifications | Activity Feed & In-App Notifications | Event capture, relevant-recipient rules, feed, unread state, and notification handling for assignments, status changes, and comments. | backend-foundation; realtime-collaboration; comments | M |
| P1 | custom-board-views | Custom Board Views | Saved views with assignee filters for “me” and “me + team,” plus defined personal/shared authority. | backend-foundation; team-definition decision | M |
| P1 | offline-cache-recovery | Offline Cache & Recovery | Local cache, connectivity/freshness indicators, pending writes, reconciliation, and recovery controls. | realtime-collaboration; cache design | M |
| P1 | sheets-export | Google Sheets Export & Portability | Export to Sheets plus CSV/XLSX download; maps application data for user-controlled reuse. | backend-foundation; Google OAuth/export design | M |
| P1 | sheets-import-sync | Google Sheets Import / Optional Sync Connector | Approved one-time import and/or ongoing connector behavior, mapping, status, and conflict UX. | sheets-export; OQ-1 decision | L |
| P2 | live-cursors | Live Cursors & Presence | Optional collaborator cursor and presence indicators. | realtime-collaboration | M |
| P2 | external-notifications | External Notification Channels | Email, push, Slack, or other delivery channels and preferences. | activity-notifications | M |

## 10. Release Readiness

The PRD is ready for UX review and a **revised HLD**, but implementation is blocked until the Tech Lead resolves the backend architecture/database choice, hosting and recovery approach, server-side authorization model, real-time transport, and conflict/offline reconciliation strategy (OQ-2 and OQ-3). Before launch, the product owner and technical stakeholders must also approve performance targets and centralized-data security, retention/deletion, and compliance requirements (OQ-6 and OQ-7). The Sheets connector's exact MVP scope must be decided before that connector is built (OQ-1); it does not block the backend, roles, real-time collaboration, comments, or in-app activity/notifications MVP scope.

> **gate:prd status=COMPLETE**
