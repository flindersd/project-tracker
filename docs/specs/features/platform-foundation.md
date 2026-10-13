# Platform Foundation

**Product:** [../PRD.md](../PRD.md)

## 1. Business Spec

<!-- gate:ba status=COMPLETE ui_required=false at=2026-10-13T00:00:00Z -->

### Overview

This backend/operations feature establishes the shared hosted platform on which the backend-authoritative Initiative Tracker can safely run: Railway environments and managed PostgreSQL, database connectivity and EF Core migration execution conventions, CI/CD gates, health checks, structured logging and monitoring, backup verification, and a rehearsed restore runbook. It deliberately provides platform capabilities rather than Board-domain persistence, authentication/user provisioning, or product UI.

### Problem Being Solved

The product's authoritative backend cannot safely persist customer work or be deployed reliably while PostgreSQL, migration execution, operational visibility, backup recovery, and deployment controls are absent or improvised by each dependent feature.

### Product Context and Scope Boundary

The PRD originally listed `backend-foundation` as a large P0 feature combining backend database, stable IDs, authentication/session foundation, board persistence, operational baseline, and migration approach. HLD §8.1 intentionally splits that bundle into:

| Split feature | Defined responsibility | Excluded from this feature |
|---|---|---|
| `platform-foundation` | Railway project, managed PostgreSQL, migration pipeline, CI/CD, health checks, logging, monitoring, backups, and rehearsed restore runbook | Authentication/session behaviour and product-domain persistence |
| `auth-foundation` | Auth0 integration, token validation, user provisioning/linking, session handling, and sign-in/sign-out UX | Railway/Postgres/platform operations |
| `board-data-model` | Board schema, integrity rules, fractional ordering, and Board CRUD | Gated production migration/deployment platform |

`platform-foundation` is therefore the infrastructure and deployment slice of the former `backend-foundation`, not a renamed broad backend feature. It must provide the common persistence and delivery path that lets domain modules deploy their reviewed EF Core migrations without inventing their own runners. The separately specified `board-data-model` may retain its Board-module schema and migrations, but its R-1 confirms that the production migration gate, provisioned Postgres, and CI remain owned here.

### Users and Stakeholders

- **Development team:** needs repeatable local/CI database validation and a safe path to deploy backend changes.
- **Operations/release owner:** needs observable, health-gated deployments, cost/quota visibility, and a tested recovery procedure.
- **Individual knowledge workers, small-team contributors, and Board Administrators:** do not operate this feature directly, but depend on it for durable, available, trustworthy board data.

### User Stories

- [ ] **[US-01]** As a developer, I want Railway-backed staging and production services with managed PostgreSQL so that the application has an authoritative, environment-isolated data store.
- [ ] **[US-02]** As a developer, I want the API to use the approved EF Core/Npgsql PostgreSQL configuration and module-aware migration conventions so that domain features can persist data consistently without startup schema changes.
- [ ] **[US-03]** As a release owner, I want CI/CD to validate changes and run reviewed migrations as an explicit gate before the API deploys so that incompatible or failed schema changes do not silently damage a live environment.
- [ ] **[US-04]** As an operations owner, I want health checks, structured privacy-safe logs, monitoring, and cost/quota alerts so that I can detect availability, performance, database, and delivery problems before they materially affect users or exceed the approved budget.
- [ ] **[US-05]** As a product owner responsible for centrally stored content, I want verified backups and a documented, rehearsed restore runbook so that the team can meet the MVP recovery objectives after a database incident.

### Acceptance Criteria

- [ ] Railway staging and production environments contain separate API, web, and managed PostgreSQL services; staging and production use separate databases and do not share secrets.
- [ ] PostgreSQL is the authoritative managed store and its configured backup/PITR capability is verified against the approved MVP RPO of no more than 5 minutes; a documented restore process targets an RTO of no more than 4 hours.
- [ ] The API obtains database configuration only from server-side environment variables/secrets; connection strings, Auth0 secrets, Google client secrets, and encryption keys are never bundled into the SPA or committed to source control.
- [ ] The shared backend platform supports EF Core 8 with `Npgsql.EntityFrameworkCore.PostgreSQL` against PostgreSQL and accommodates one DbContext and migrations-history table per module in the shared `public` schema.
- [ ] The API does not execute `Database.Migrate()` or any automatic migration at application startup.
- [ ] Every production/staging schema migration is reviewed, expand/contract compatible with the previously deployed API version, and runs through an explicit gated pipeline step before the associated API deployment.
- [ ] CI validates the applicable codebase through linting, type checking, unit tests, PostgreSQL-backed integration tests using Testcontainers, and builds before staging deployment; dependency caching and cancellation of superseded runs are configured to contain Actions usage.
- [ ] Deployments expose `/healthz/live` for process liveness and `/healthz/ready` for database connectivity and migration state; a failed readiness check prevents promotion of an unhealthy API.
- [ ] Structured logs include correlation identifiers, exclude request bodies and secrets, and are retained according to the selected Railway plan; production operational access is limited and auditable.
- [ ] Monitoring and alerting cover readiness/uptime, error rate, p99 latency, database connection saturation, failed-login rate, and—when SignalR is introduced—connection count; the 99.5% monthly availability budget is tracked.
- [ ] Railway spend and GitHub Actions minutes are reviewed monthly and escalated at 80% of their approved envelope/allowance; Actions usage at approximately 1,600 minutes for two consecutive months triggers the HLD TD-13 response.
- [ ] A restore runbook identifies ownership, prerequisites, verification steps, communications, and rollback/escalation actions, and a restore drill is completed and recorded before M1 release.
- [ ] The platform starts with one API replica and no Redis backplane, consistent with the confirmed 99.5% MVP availability target; multi-replica/Redis work remains deferred until an HLD TD-1 trigger occurs.

### Functional Requirements

- **FR-01:** The feature shall provision and document distinct Railway staging and production environments with API, static web, and managed PostgreSQL services.
- **FR-02:** The feature shall configure Railway-managed PostgreSQL as the authoritative backend data store and verify the plan's backup/PITR retention against RPO ≤5 minutes and RTO ≤4 hours.
- **FR-03:** The feature shall configure server-side secret/environment-variable handling for database connectivity and operational secrets, with no secrets exposed to the SPA bundle or source repository.
- **FR-04:** The feature shall establish the approved EF Core 8 and Npgsql PostgreSQL baseline and support per-module DbContexts with independent migration history tables in one shared database and `public` schema.
- **FR-05:** The feature shall define and enforce the migration rule that migrations are reviewed, expand/contract compatible, and never run automatically when the API starts.
- **FR-06:** The feature shall provide an explicit, gated CI/CD migration step that completes before deployment of the API version that requires the schema change.
- **FR-07:** The feature shall provide a CI pipeline that runs linting, type checking, unit tests, PostgreSQL/Testcontainers integration tests, and builds before staging deployment from `main`, with promotion to production by tag.
- **FR-08:** The feature shall configure dependency caching and same-branch run cancellation in GitHub Actions and keep PR preview environments disabled by default.
- **FR-09:** The feature shall expose live and readiness health endpoints; readiness shall verify database connectivity and migration state.
- **FR-10:** The feature shall provide structured, correlation-ID-enabled, privacy-safe application logging and operational monitoring/alerting for the HLD-defined service-health signals.
- **FR-11:** The feature shall establish monthly tracking and 80%-threshold escalation for Railway spend and GitHub Actions minutes within the product owner's approved cost envelope.
- **FR-12:** The feature shall produce and execute a documented restore runbook, recording evidence of a successful restore drill before M1 release.
- **FR-13:** The feature shall preserve the MVP single-API-replica, no-Redis topology and document HLD TD-1 triggers rather than prematurely provisioning a Redis backplane or multi-replica deployment.

### Non-Functional Requirements

- **Availability and recovery:** The platform is designed for the product-owner-confirmed 99.5% monthly MVP target, RPO ≤5 minutes, and RTO ≤4 hours. These are targets, not a public contractual SLA.
- **Security and privacy:** TLS and platform encryption at rest must be verified and documented; secrets use Railway environment variables; logs must not contain request bodies, user board content, or secrets.
- **Reliability:** A failed migration or readiness check must block deployment/promotion. Database changes must remain compatible with the immediately preceding API release.
- **Observability:** The release owner can identify availability, error, latency, database saturation, deployment, backup/restore, and cost/quota conditions from documented checks, alerts, and logs.
- **Cost:** Implementation must remain within the approved pilot envelope of approximately $15–30/month and production envelope of approximately $35–75/month. Redis is explicitly out of MVP scope.

### Dependencies

Mark each dependency as one of:
- **Hard dependency** — cannot proceed without this. Development is blocked until it exists or is confirmed in scope.
- **Soft dependency** — should ideally exist first, but work can begin with a workaround or assumption.

| Dependency | Type | Notes |
|------------|------|-------|
| Railway account/project access and approved managed PostgreSQL plan | Hard | Required to provision isolated environments, database, backups/PITR, and runtime configuration. CF-1 is product-owner approved. |
| GitHub repository administration and GitHub Actions access | Hard | Required for private-repository CI, explicit migration gate, deployment workflow, caching, and usage controls. CF-3 is approved. |
| API and web deployable baseline | Hard | Required to configure service builds, health endpoints, deployment, and CI validation. The scaffold gate is complete. |
| Sentry/approved monitoring configuration and operational-alert recipient | Soft | HLD selects Sentry and requires monitoring; initial health/logging work can proceed while the final project/alert routing is configured. |
| `auth-foundation` Auth0 tenant/application and application secrets | Soft | It needs the same secret-management platform but is independently deliverable; authentication implementation is out of scope. |
| `board-data-model` and later module migrations | Soft | Consumers of the migration convention. This feature must establish a module-aware gate; it must not own Board schema/content. |

### Out of Scope

- Auth0 sign-in/sign-out UX, token validation, session handling, user provisioning/linking, or Board membership/role enforcement (`auth-foundation` / `server-rbac`).
- Board, task, user, membership, comment, activity, or any other product-domain schema and CRUD behaviour, including Board-module migration authorship (`board-data-model` and later features).
- User-facing platform administration, status dashboard, or other product UI.
- SignalR, a Redis backplane, multi-replica API hosting, multi-region deployment, or a contractual SLA; these are deferred under HLD TD-1/TD-10 triggers.
- Google Sheets connector configuration beyond securely hosting its future secrets; connector OAuth and export are separate features.
- Data-retention policy, account/board hard-purge implementation, or legal/privacy-notice approval, which remain pre-launch work under HLD OQ-7/TD-5.

### Assumptions

- The product owner-approved Railway, GitHub Actions, and Sentry cost posture remains available within the HLD envelope.
- Railway can provide and document backup/PITR characteristics sufficient for the stated recovery objectives; if it cannot, the Tech Lead will specify the HLD-approved independent nightly-backup fallback.
- The platform remains a single-region, single-API-replica MVP deployment until a documented TD-1 or TD-10 trigger is met.

### Open Questions

| # | Question | Owner | Resolution timing |
|---|----------|-------|-------------------|
| 1 | Which Railway plan/backup retention configuration demonstrably meets the 5-minute RPO and supports the 4-hour restore target, or is the independent nightly logical-backup fallback required? | Tech Lead + release owner | Must resolve during `/tl` before infrastructure provisioning |
| 2 | Which named operational owner receives availability, spend, Actions-minute, and restore-failure alerts, and where is the escalation/contact record maintained? | Product owner + release owner | Should resolve during `/tl` before staging deployment |
| 3 | What deployment implementation will run and verify multiple module-specific EF Core migration history tables while preserving the required pre-deploy gate? | Tech Lead | Must resolve during `/tl` before the first production migration |

### Size Assessment

This feature has five user stories, no screens, and 13 functional requirements. It is within the HLD's M (~2 week) split estimate and does not exceed the early-warning thresholds (>5 user stories, >8 screens, or >15 functional requirements). Its external platform prerequisites are hard dependencies, but it has no multiple hard dependencies on unbuilt product features. No split is proposed; Technical Approach review must validate the delivery sequencing and migration-gate design.

## 2. UX Review

_Pending — run `/ux` if this feature has user-facing UI._

## 3. Technical Approach

<!-- gate:tl verdict=APPROVED at=2026-10-13T00:00:00Z -->

### Verdict

**APPROVED.** The feature is technically feasible, correctly scoped as the infrastructure slice of the former `backend-foundation`, and estimated at **~2 weeks** — comfortably inside the 5-week threshold. No split required.

One acceptance criterion was **factually unachievable as written** and has been renegotiated with the product owner (see D-1 below). Section 1 does not need to be reopened by the BA; the amendment is recorded here and listed in "Spec amendments to fold in".

NFRs confirmed: **yes** — HLD §1.6, restated in Section 1, with the RPO amended by D-1.
Cost flags: **none new.** All work sits inside the approved CF-1/CF-3 envelope.

---

### Key Technical Decisions

#### D-1 — RPO is amended to ≤24 hours for pilot; ≤5 minutes becomes a production-launch gate

**This supersedes the Section 1 AC and FR-02 wording of "RPO no more than 5 minutes", and resolves Section 1 OQ-1.**

The HLD (§1.6, §7.3, CF-1) assumed "Railway Postgres point-in-time-recovery". **That capability does not exist.** Verified at `docs.railway.com/reference/backups` on 2026-10-13: Railway backs up *volume snapshots*, on a Daily / Weekly / Monthly schedule only — Daily is the finest granularity and is retained for 6 days. There is no WAL archiving and no PITR. The achievable RPO on Railway managed Postgres is therefore **up to 24 hours**.

The spec's own stated fallback — "independent nightly logical backup" — does not close the gap either, since nightly is also a ~24h RPO. The assumption was self-defeating.

**Product owner decision (2026-10-13): accept a 24-hour RPO for M1/pilot.** Rationale: pilot data is low-value and recreatable; the 5-minute figure originated from an unverified platform claim rather than a business requirement; and building continuous WAL archiving (~1 week plus a permanent operational burden we would own end-to-end) before the product is validated is precisely the over-engineering this feature is meant to avoid.

**RTO ≤4 hours is retained and is achievable.** Railway restore is a staged volume mount plus redeploy — minutes, not hours. The runbook (D-8) must prove this, not assume it.

**Hard trigger — this is a launch gate, not a backlog item.** Before the pilot opens beyond invited users, or the moment any customer stores data they would not accept losing, RPO must be re-decided. Options priced today, for the record:

| Option | RPO | Cost | Build |
|---|---|---|---|
| Railway daily snapshot only (**chosen for pilot**) | ≤24 h | $0 | none |
| Scheduled `pg_dump` → Railway Object Storage ($0.015/GB-mo) | ~15 min | ~$1/mo | ~2 days |
| Self-managed WAL archiving (pgBackRest/wal-g) | ~5 min | ~$1–2/mo | ~1 wk + permanent ops burden |
| Neon Launch "instant restore", 7-day history | ~instant | **~$24–30/mo** — consumes the entire pilot envelope | ~3 days migration |

The last two are **new cost/scope flags** and must come back through `/tl` if triggered. Record this as **TD-14** in the HLD.

#### D-2 — Migrations run via EF Core migration bundles, executed by a gated CI job

**Resolves Section 1 OQ-3.** The requirement is to run N module-specific migration history tables against one database, before the API deploys, with no bespoke runner (`EF_CORE_AND_MIGRATIONS.md` §6 forbids one).

**Decision:** `dotnet ef migrations bundle --context <ModuleDbContext>` produces a **self-contained executable per module context**. CI builds one bundle per registered context and executes them in a fixed, declared order against the target database as an explicit job that **must succeed before the deploy job starts**.

Why bundles rather than `dotnet ef database update`:
- No .NET SDK, no project source and no design-time service provider needed at execution time — the deploy environment stays minimal.
- The bundle is a build artefact, so **the exact migration set that was reviewed in the PR is the one that runs**. `database update` re-resolves from source and is therefore not reproducible.
- Each bundle respects its own `MigrationsHistoryTable`, which is already configured (`__EFMigrationsHistory_Board`, `Program.cs:54`). Module independence falls out for free with no custom orchestration.

Alternative rejected: generating idempotent SQL via `migrations script --idempotent` and applying with `psql`. It works, but hand-applied SQL loses the history-table bookkeeping guarantees and invites someone to "just run the script" out of band.

Module order is declared in one list in the workflow, not inferred. With cross-module shadow FKs permitted (`EF_CORE_AND_MIGRATIONS.md` §2.1), order matters and must be explicit. For M1 the list is `BoardDbContext` only.

**The gate is a job dependency, not a script step.** A failed bundle must leave the previously deployed API running untouched. Because migrations are expand/contract (§6), the old API survives the new schema, so a failed *deploy* after a successful *migrate* is also safe.

#### D-3 — Readiness must actually check; today it lies

`Program.cs:72` returns `{ status = "ok" }` unconditionally. This satisfies the letter of "expose `/healthz/ready`" and none of its purpose — a health-gated deploy against a stub health check is worse than no gate, because it manufactures false confidence.

- `/healthz/live` — process only. No dependencies. Stays trivial. Correct as-is.
- `/healthz/ready` — **must** verify (a) a real database round trip and (b) that no migration is pending for any registered context.

Use `Microsoft.Extensions.Diagnostics.HealthChecks` with `AddDbContextCheck<T>` (which covers both connectivity and `GetPendingMigrationsAsync`), mapped via `MapHealthChecks` with a readiness tag predicate. Do **not** hand-roll this.

Readiness must return 503 on failure with a response body naming the failing check — an opaque 503 at 3am is an hour of wasted incident time.

#### D-4 — Three pre-existing security defects must be fixed in this feature

These are in `Program.cs` today, were out of scope for `board-data-model`, and are squarely platform concerns. They are **not** optional cleanup.

1. **`Program.cs:20-26` — `AllowAnyOrigin()` on the default CORS policy.** Unacceptable once a real auth token exists. Replace with an explicit allow-list driven by configuration (`Cors:AllowedOrigins`), `AllowCredentials()`, per environment. Staging and production get their own web origin only.
2. **`Program.cs:48-50` — hardcoded connection-string fallback including a password.** Violates the AC "database configuration only from server-side environment variables". The fallback is convenient in Development and is a production footgun. **Fail fast on startup if the connection string is absent in any non-Development environment.** A missing connection string must crash the process, not silently connect to a non-existent localhost.
3. **`Program.cs:49` — `configuration["DATABASE_URL"]` is read as a raw .NET connection string.** Railway injects `DATABASE_URL` in **libpq URI form** (`postgresql://user:pass@host:port/db`). Npgsql will not parse this. This is a latent, guaranteed first-deploy failure. Add an explicit URI→`NpgsqlConnectionStringBuilder` translation, and set `SSL Mode=Require`.

Defect 3 in particular is why this feature exists: without it, every dependent feature discovers the same breakage independently.

#### D-5 — Structured logging via Serilog; correlation ID from W3C `traceparent`

`Serilog.AspNetCore` with `UseSerilogRequestLogging`, writing **compact JSON to stdout** (Railway captures stdout; do not write log files to a volume).

- **Correlation ID is `Activity.Current.TraceId`**, not a bespoke header. ASP.NET Core already populates this from the W3C `traceparent` header. Inventing an `X-Correlation-Id` would duplicate a mechanism we already get for free and that Sentry already understands.
- **Request bodies are never logged.** This is a `MUST` — bodies contain user board content (HLD §7.3). Enforce by never calling `EnableBuffering` for logging purposes and by setting the request-logging enricher to an explicit property allow-list (method, path, status, duration, trace id, user subject once auth lands). An allow-list, not a deny-list — a deny-list leaks the first time someone adds a field.
- Query strings are logged **path-only**; query values may carry filter content.
- Default level `Information`, `Warning` for `Microsoft.*`, and **`Microsoft.EntityFrameworkCore.Database.Command` pinned to `Warning`** — at `Information` EF logs parameter values, which is a content leak through the back door.

#### D-6 — Railway topology: two environments in one project, Hobby → Pro

| | Pilot (now) | Production launch |
|---|---|---|
| Plan | **Hobby $5/mo** (incl. $5 credit) | **Pro $20/mo** (incl. $20 credit) |
| Log retention | 7 days | 30 days |
| Max volume | 5 GB | 1 TB |
| Members/project | 3 | unlimited |

Verified at `railway.com/pricing`, 2026-10-13. Usage rates: memory ≈$10/GB/mo, CPU ≈$20/vCPU/mo, volumes $0.15/GB/mo, egress $0.05/GB, object storage $0.015/GB-mo.

Use **one Railway project with `staging` and `production` environments**, not two projects. Environments give separate variable sets and separate volumes (which is what the AC requires) while keeping one canvas and one billing view. Two projects would double the admin surface for no isolation benefit at this size.

Three services per environment: `api` (container), `web` (static), `postgres`.

⚠️ **Hobby caps volumes at 5 GB and manual backups at 50% of volume size.** Fine at pilot scale (§1.6: 500 users). The Pro move is required at launch for log retention (D-7) regardless.

**`railway.toml` is wrong today** and must be fixed: `startCommand = "dotnet ProjectTracker.Api.dll"` has no path and will not resolve against the Nixpacks publish output. Pin the publish output directory and reference the DLL by full path. Also set `healthcheckPath = "/healthz/ready"` so Railway's own rollout gate uses the real check (D-3) — this is what delivers the AC "a failed readiness check prevents promotion of an unhealthy API", and it is a platform setting, not application code.

#### D-7 — Monitoring: Sentry for errors, Railway-native for uptime, GitHub for CI cost

Deliberately minimal — three tools, all already approved under CF-1/CF-3, no new vendor.

| Signal | Mechanism |
|---|---|
| Uptime / readiness | External uptime check on `/healthz/ready` + Railway deploy health gate |
| Error rate, p99 latency | Sentry (free Developer tier) — `Sentry.AspNetCore`, traces sampled, **`SendDefaultPii = false`** |
| DB connection saturation | Npgsql pool metrics → logged periodically; alert on pool exhaustion |
| Failed-login rate | Deferred to `auth-foundation` — there is no login here yet. Record as a documented hand-off, do not stub it. |
| SignalR connection count | **Not applicable at M1** (HLD TD-1). Do not build a placeholder. |
| Availability budget (99.5%) | Monthly manual review against uptime-check data. An SLO tool is over-engineering at this scale. |
| Railway spend / Actions minutes | Monthly manual review; escalate at 80% (TD-13) |

**Alert ownership (resolves Section 1 OQ-2).** Product owner decision: **a single named owner**, receiving all four alert classes by email, with the contact record maintained in-repo at `docs/specs/operations/on-call.md` and mirrored in `CODEOWNERS`. The specific person is a provisioning input for Chunk 5, not an open decision. **The backup-failure alert in particular must not ship unrouted** — an unmonitored backup is equivalent to no backup.

#### D-8 — The restore drill is a deliverable, not a document

A runbook nobody has executed is fiction. The M1 deliverable is a **completed drill with recorded evidence**: restore the staging volume from a snapshot, measure wall-clock time to a passing `/healthz/ready`, and record the measured RTO against the ≤4h target. If the measured time exceeds the target, that is a finding that comes back through `/tl` — not a number to round down.

Drill scope: staging only. Never rehearse against production.

#### D-9 — CI: two-tier pipeline to stay inside 2,000 Actions minutes

Per CF-3, a full run is ~8–12 min and the team's cadence puts us at or over the free allowance. Mitigations are applied **from day one**, not retrofitted:

- **Fast gate on every push/PR:** lint → typecheck → unit tests → build. Target <4 min.
- **Full gate on `main` and on PRs labelled `full-ci`:** adds Testcontainers Postgres integration tests.
- `concurrency: group: ${{ github.ref }}, cancel-in-progress: true` — reclaims 20–30% alone.
- `actions/cache` for the NuGet and npm stores; `actions/setup-dotnet` and `setup-node` cache keys off the lock files.
- **PR preview environments disabled.**

Promotion: push to `main` → migrate-staging → deploy-staging. Git tag `v*` → migrate-production → deploy-production. Production deploy requires a **GitHub Environment protection rule with required reviewer** — this is the human half of the migration gate and is a platform setting, not a script.

The root `package.json` scripts (`lint`, `test`, `build`) already fan out across both the npm workspaces and the .NET solution, so workflow steps call `npm run <script>` and stay thin. Do not duplicate the command composition into YAML.

#### D-10 — What this feature deliberately does NOT build

Restated because the temptation is real: no Redis, no second API replica, no SignalR scaffolding, no auth, no board schema, no rate limiting, no SLO tooling, no bespoke migration runner, no `X-Correlation-Id`. Every one of these has a documented trigger elsewhere; none has been met.

---

### Standards Compliance

This approach **references** the binding standards rather than re-deciding them:

- `EF_CORE_AND_MIGRATIONS.md` §2 (DbContext + history table per module), §6 (expand/contract, no `Database.Migrate()`, no bespoke runner) — D-2 implements the pipeline half of §6 and introduces no new convention.
- `AUTHORIZATION.md` — out of scope here; health endpoints are deliberately anonymous. When `auth-foundation` lands, `/healthz/*` must be explicitly `AllowAnonymous`, not accidentally so.
- `CSHARP_STYLE.md` — logging and DI registration follow it.
- `Program.cs` currently has **no `Database.Migrate()`** ✅ — the §6 `MUST NOT` is already satisfied and must stay that way.

**HLD amendment required:** HLD §7.1 shows `apps/web` / `apps/api`; the repository uses `src/`. `INDEX.md` already records that `src/` is correct. Fold this into the HLD.

---

### Module / File Breakdown

| Path | Change |
|---|---|
| `src/api/Program.cs` | CORS allow-list, connection-string resolution + fail-fast, `DATABASE_URL` URI translation, Serilog, Sentry, real health checks |
| `src/api/Infrastructure/DatabaseConnectionString.cs` | **new** — libpq URI → Npgsql translation (D-4.3) |
| `src/api/Infrastructure/HealthChecks/` | **new** — readiness registration + response writer |
| `src/api/Infrastructure/Logging/` | **new** — Serilog config, property allow-list enricher |
| `src/api/ProjectTracker.Api.csproj` | + Serilog.AspNetCore, Sentry.AspNetCore, Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore |
| `src/api/appsettings*.json` | Cors, Serilog, Sentry, health-check sections |
| `src/api/.env.example` | document every required variable |
| `src/api/railway.toml` | fix `startCommand`, add `healthcheckPath` (D-6) |
| `.github/workflows/ci.yml` | **new** — fast + full gates (D-9) |
| `.github/workflows/deploy.yml` | **new** — bundle → migrate → deploy, per environment (D-2) |
| `.github/CODEOWNERS` | **new** — alert/escalation ownership |
| `docs/specs/operations/runbook-restore.md` | **new** — runbook + recorded drill evidence (D-8) |
| `docs/specs/operations/on-call.md` | **new** — named owner, escalation path (D-7) |
| `docs/specs/operations/environments.md` | **new** — Railway topology, variables, plan posture |

---

### Risks and Mitigations

| Risk | Sev | Mitigation |
|---|---|---|
| 24h RPO is quietly carried into production | **High** | D-1 trigger recorded as HLD TD-14 and as a named launch gate, not a backlog item. The runbook states the RPO in plain language so nobody can claim they assumed otherwise. |
| `DATABASE_URL` format mismatch breaks first deploy | High | D-4.3 — explicit translation plus an integration test against a Railway-format URI. This is a known, guaranteed failure, not a hypothetical. |
| Readiness stub survives into the deploy gate | High | D-3; the drill (D-8) exercises the real check end-to-end, which is the only way to prove it. |
| Migration gate passes, deploy fails, schema is ahead of code | Medium | Expand/contract (`EF_CORE_AND_MIGRATIONS.md` §6) makes this safe by construction — the old API survives the new schema. |
| Actions minutes overrun | Medium | D-9 from day one; TD-13 trigger at 1,600 min. Worst case ~$8/mo. |
| Hobby 5 GB volume cap reached mid-pilot | Low | Monitored in the monthly cost review; Pro lifts it to 1 TB. |
| Single API replica — deploys are visible outages | Low | Accepted (HLD §9.3). 99.5% ≈ 3.6 h/month absorbs it. |

---

### Estimated Complexity

**~2 weeks**, matching HLD §8.1. Well under the 5-week threshold — **no split required.**

Roughly: provisioning 2d · connectivity/health/logging 3d · migration pipeline 2d · CI/CD 3d · monitoring, runbook and drill 2d.

The irreducible cost is the drill and the first real deploy; both are discovery-heavy and resist estimation. Chunk 1 is substantially **manual Railway console work** and cannot be fully automated by a developer agent — treat it as a guided checklist.

---

### Open Technical Questions

Both Section 1 open questions requiring a decision are now closed (OQ-1 → D-1, OQ-2 → D-7, OQ-3 → D-2). Remaining items are **provisioning inputs**, not decisions, and do not block the build:

1. The specific named individual and email for D-7 — needed before Chunk 5 completes.
2. Sentry organisation/project DSN — needed before Chunk 5; Chunks 1–4 proceed without it.

---

### Spec amendments to fold in

- **Section 1 AC and FR-02:** replace "RPO of no more than 5 minutes" with "RPO of no more than 24 hours for pilot, with a documented production-launch gate to re-decide (TD-14)". Remove "PITR" — Railway does not offer it.
- **Section 1 Assumptions:** the "independent nightly-backup fallback" assumption is void; nightly does not satisfy a 5-minute RPO.
- **HLD §1.6, §7.3, CF-1:** same RPO/PITR correction. Add TD-14. Correct `apps/` → `src/`.

---

### Implementation Chunks

| # | Name | Scope | Depends on | Size |
|---|------|-------|------------|------|
| 1 | Railway environments & configuration | Railway project with `staging`/`production` environments; `api`/`web`/`postgres` services; daily volume backup schedule; environment variables and secrets; fix `src/api/railway.toml` (`startCommand`, `healthcheckPath`); `.env.example`; `docs/specs/operations/environments.md`. **Largely manual console work — produce a verified checklist.** | — | small |
| 2 | Connectivity, health checks & logging | `DatabaseConnectionString` libpq-URI translation; fail-fast config resolution; CORS allow-list; real `/healthz/live` + `/healthz/ready` via `AddDbContextCheck`; Serilog JSON to stdout with property allow-list; `Program.cs` + `appsettings` + csproj packages. Fixes D-4.1–4.3. | Chunk 1 | medium |
| 3 | Migration bundle pipeline | `dotnet ef migrations bundle` per module context; declared context execution order; the gated migrate step as a discrete, failing-closed job. Verified against a real Railway staging database. | Chunk 2 | medium |
| 4 | CI/CD workflows | `.github/workflows/ci.yml` (fast + full gates, caching, concurrency cancellation); `.github/workflows/deploy.yml` (migrate → deploy, staging on `main`, production on tag with required-reviewer environment protection); PR previews off. | Chunk 3 | medium |
| 5 | Monitoring, cost watch & restore drill | Sentry wiring (`SendDefaultPii = false`); uptime check on `/healthz/ready`; DB pool saturation alert; alert routing to the named owner; `CODEOWNERS`; `on-call.md`; monthly Railway-spend and Actions-minutes review procedure; `runbook-restore.md` **plus an executed staging restore drill with recorded measured RTO**. | Chunk 4 | medium |

Each chunk leaves the codebase compilable and committable. Chunk 5's drill is a gate on the feature, not a documentation task.

## 4. Test Plan

<!-- gate:tests status=WRITTEN_AND_FAILING authored_by=tester at=2026-10-13T03:39:23Z -->

### Clarification Checklist

No clarifications were raised with the BA/Tech Lead before writing tests — Section 3 (D-1 through D-10) resolves all Section 1 open questions and is specific enough about expected behaviour (exact header names, status codes, file paths, log levels) to write deterministic tests directly against it.

### Test Plan Summary

| Layer | Framework | # Tests | Areas covered |
|---|---|---|---|
| Integration — C# backend (`WebApplicationFactory` + Testcontainers) | xUnit | 7 | D-3 readiness health checks (DB unreachable, pending migrations, healthy structured response); D-4.1 CORS allow-list (credentialed allowed origin, rejected disallowed origin); D-4.2 fail-fast connection-string resolution in Production; D-4.3 `DATABASE_URL` libpq-URI translation against a real Postgres container |
| Unit/config — C# backend (file-content assertions) | xUnit | 4 (2 theory cases + 2) | D-5 EF Core command log level pinned to `Warning` in every `appsettings*.json`; D-6 `railway.toml` `startCommand` full-path correctness and `healthcheckPath` pointing at `/healthz/ready` |

**Total new tests: 11**, all currently failing against the unmodified codebase (confirmed by running `dotnet test` — see below).

Pre-existing `board-data-model` test suite (29 tests) is unaffected and still green.

### Test files added

- `tests/api/ProjectTracker.Api.Tests/Platform/gherkin-scenarios.md` — plain-English Given/When/Then source for every test below
- `tests/api/ProjectTracker.Api.Tests/TestInfrastructure/PlatformWebApplicationFactory.cs` — test-only `WebApplicationFactory<Program>` supporting per-test configuration/environment overrides
- `tests/api/ProjectTracker.Api.Tests/Platform/HealthEndpointTests.cs`
- `tests/api/ProjectTracker.Api.Tests/Platform/CorsPolicyTests.cs`
- `tests/api/ProjectTracker.Api.Tests/Platform/ConnectionStringFailFastTests.cs`
- `tests/api/ProjectTracker.Api.Tests/Platform/DatabaseUrlTranslationTests.cs`
- `tests/api/ProjectTracker.Api.Tests/Platform/LoggingConfigurationTests.cs`
- `tests/api/ProjectTracker.Api.Tests/Platform/RailwayConfigurationTests.cs`
- `tests/api/ProjectTracker.Api.Tests/ProjectTracker.Api.Tests.csproj` — added `Microsoft.AspNetCore.Mvc.Testing` package reference (test infra only, no production dependency added)

### Acceptance criteria → test case mapping

| Section 1 AC / Section 3 decision | Test case(s) |
|---|---|
| "Deployments expose `/healthz/live`... and `/healthz/ready`... a failed readiness check prevents promotion" (AC); D-3 | `HealthEndpointTests.Ready_returns_503_when_database_is_unreachable`, `Ready_returns_503_when_migrations_are_pending`, `Ready_returns_200_with_structured_checks_when_database_is_healthy` |
| "The API obtains database configuration only from server-side environment variables/secrets" (AC); D-4.2 | `ConnectionStringFailFastTests.Startup_fails_in_production_when_no_connection_string_is_configured` |
| D-4.3 (`DATABASE_URL` libpq URI translation — "a known, guaranteed failure" per Risks table) | `DatabaseUrlTranslationTests.A_libpq_style_DATABASE_URL_is_usable_for_real_requests` |
| D-4.1 (CORS allow-list, replacing `AllowAnyOrigin()`) | `CorsPolicyTests.Allows_credentialed_requests_from_a_configured_allowed_origin`, `Does_not_allow_a_disallowed_origin` |
| D-5 ("Request bodies are never logged"; `Microsoft.EntityFrameworkCore.Database.Command` pinned to `Warning`) | `LoggingConfigurationTests.EfCoreCommandLogging_is_never_configured_more_verbose_than_warning` (theory over both `appsettings.json` and `appsettings.Development.json`) |
| D-6 (`railway.toml` `startCommand` must resolve against Nixpacks publish output; `healthcheckPath` must use the real readiness check) | `RailwayConfigurationTests.StartCommand_references_a_fully_qualified_dll_path`, `HealthcheckPath_points_at_the_real_readiness_endpoint` |

### Test command(s)

```bash
# Full repo suite (as used by CI / npm run test)
npm run test

# This feature's backend tests only
dotnet test tests/api/ProjectTracker.Api.Tests/ProjectTracker.Api.Tests.csproj --filter "FullyQualifiedName~Platform"
```

Requires Docker running locally (Testcontainers spins up ephemeral `postgres:16-alpine` containers for the integration tests). CI already has Docker available on `ubuntu-latest` runners.

**Pre-existing repo issue found, not introduced by this test suite:** `src/api/ProjectTracker.Api.sln`'s `Global`/`GlobalSection` lines contain literal backslash-`t`/backslash-`n` escape sequences instead of real tab/newline characters, which breaks `dotnet restore/build/test` when invoked directly against the `.sln` file (MSBuild logs "Unable to find a project to restore!" and silently builds nothing). Tests were verified by building/running against the `.csproj` files directly, which parse correctly. The build agent should either fix the `.sln` line endings or continue invoking `dotnet test` against the `.csproj` paths / `npm run test` (which calls `dotnet test src/api/ProjectTracker.Api.sln` — **this will need the `.sln` fixed before the build agent can rely on the root `npm run test` script**).

### Confirmation tests currently fail (TDD gate)

Ran `dotnet test tests/api/ProjectTracker.Api.Tests/ProjectTracker.Api.Tests.csproj --filter "FullyQualifiedName~Platform"` against the unmodified codebase:

```
Total tests: 11
     Failed: 11
```

All 11 fail for the expected reason (missing/stubbed implementation — e.g. `/healthz/ready` still returns the hardcoded `{ status = "ok" }`, CORS still uses `AllowAnyOrigin()`, `railway.toml` still has the broken `startCommand`), not for compilation or environment errors. The pre-existing 29 `board-data-model` tests remain green and unaffected.

### Assumptions made

- **D-3 structured readiness body:** the spec doesn't dictate exact JSON shape for `/healthz/ready`. Assumed a `checks` array (consistent with `Microsoft.Extensions.Diagnostics.HealthChecks`' standard `UIResponseWriter`-style output) and a body that textually names "postgres"/"database" on failure. The build agent may shape the JSON differently as long as it is a genuine per-check breakdown, not a bare constant, and the 503 body names the failing check.
- **CORS header assertions:** assumed the conventional ASP.NET Core CORS middleware header names (`Access-Control-Allow-Origin`, `Access-Control-Allow-Credentials`) are used, since D-4.1 specifies `AllowCredentials()` via the standard `AddCors` API.
- **Fail-fast test isolates the connection-string path** from an unrelated existing guard (`StubBoardAuthorizationService` already throws in non-Development without explicit opt-in) by explicitly setting `Board:AllowStubAuthorization=true`, so the test fails/passes only on the connection-string behaviour it targets.
- **D-4.3 test uses the already-public `GET /api/boards` endpoint** as the DB-access probe, rather than a new dedicated endpoint, since no new public endpoint is specified for this feature.

### Not covered and why

- **Chunk 1 (Railway project/environment/service provisioning)** — manual console work with no code surface; the spec itself calls this "a guided checklist," not a build task. Verified by the operator, not a test.
- **CI/CD workflow behaviour** (fast/full gate timing, `concurrency: cancel-in-progress`, required-reviewer production environment protection, dependency caching) — these are GitHub Actions/environment settings; no meaningful unit test exists for them. Verified by an actual workflow run.
- **Sentry wiring, uptime monitoring, DB pool-saturation alerting, monthly Railway spend/Actions-minutes review (D-7, D-9 cost controls)** — operational configuration with no deterministic local test; verified manually per the runbook.
- **The restore drill and measured RTO (D-8)** — must be an executed, recorded drill against real staging infrastructure, not a test suite assertion.
- **Migration bundle CI job ordering (D-2)** — a deploy-pipeline behaviour that requires a live CI run against a real environment; not isolable as a unit/integration test without reimplementing the GitHub Actions runner.
- **Structured-log content assertions beyond the EF command log level** (e.g. "request bodies are never logged," correlation ID from `traceparent`) — no Serilog sink exists yet to intercept; writing a black-box test today would require guessing at a yet-unbuilt enricher's output shape in a way that risks dictating implementation rather than verifying behaviour. Recommend the build agent add this coverage as part of Chunk 2 once the Serilog enricher exists, or flag back to `/tl` if a testable seam (e.g. an injectable `ILoggerProvider`) should be specified.



## 5. Implementation Notes

_Pending — run `/build`._

## 6. Review Outcome

_Pending — run `/review`._

## 7. Release Notes

_Pending — run `/release`._
