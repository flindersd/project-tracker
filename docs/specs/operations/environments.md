# Platform Environments (Railway)

## Overview

This document describes the Railway platform configuration for the Project Tracker initiative, covering the topology, environment variables, backup strategy, and the manual checklist for provisioning and verification.

The platform provides a hosted infrastructure layer on Railway that supports the shared backend (EF Core + PostgreSQL), CI/CD pipelines, health checks, structured logging, monitoring, and data recovery.

> **Note:** This is purely a configuration/ops document. The feature that actually implements the logic (health checks, migrations, logging, etc.) lives in `src/api/`. This document records what needs to be configured in the Railway console.

---

## 1. Railway Project Topology

```
+------------------------------------------------------------+
|  PROJECT: project-tracker (Railway)                         |
|                                                            |
|  ┌──────────────────────────────────────────────────────┐  |
|  │  Environment: staging                                 │  |
|  │  ┌─────────┐  ┌─────────┐  ┌────────────────┐       │  |
|  │  │  api    │  │  web    │  │  postgres      │       │  |
|  │  │  (api)  │  │  (web)  │  │  (managed-db)  │       │  |
|  │  └─────────┘  └─────────┘  └────────────────┘       │  |
|  │                                                      │  |
|  │  Shared: Volume  (Postgres data volume, shared env)   │  |
|  └──────────────────────────────────────────────────────┘  |
|                                                            |
|  ┌──────────────────────────────────────────────────────┐  |
|  │  Environment: production                             │  |
|  │  ┌─────────┐  ┌─────────┐  ┌────────────────┐       │  |
|  │  │  api    │  │  web    │  │  postgres      │       │  |
|  │  │  (api)  │  │  (web)  │  │  (managed-db)  │       │  |
|  │  └─────────┘  └─────────┘  └────────────────┘       │  |
|  │                                                      │  |
|  │  Shared: Volume  (Postgres data volume, shared env)   │  |
|  └──────────────────────────────────────────────────────┘  |
|  └──────────────────────────────────────────────────────┘  |
+------------------------------------------------------------+
```

### Environment Separation

- **Staging** and **Production** are separate *environments* within a single Railway project.
- Each environment has its own **secrets** (environment variables). Staging never sees production secrets.
- Each environment has its own **volume**. Staging and production databases are completely isolated.
- Both environments share the same **volume name** for restore purposes — this is intentional and required for the restore runbook.

### Service Allocation per Environment

| Environment | Service | Type | Railway plan |
|-------------|---------|------|--------------|
| staging     | api     | Container build (`net8.0`) | Hobby |
| staging     | web     | Static directory | Hobby |
| staging     | postgres| Managed PostgreSQL | Hobby |
| production  | api     | Container build (`net8.0`) | Pro |
| production  | web     | Static directory | Pro |
| production  | postgres| Managed PostgreSQL | Pro |

> **Why one project?** We start with a single project and two environments to reduce operational overhead. Once a trigger event (HLD TD-1) occurs, we will promote to a second project (separate billing).

---

## 2. Plan Posture

| Stage | Plan(s) | Monthly Cost | Notes |
|-------|---------|--------------|-------|
| Pilot  | Hobby x3 | ~$15/mo (includes $15 credit) | Hobby includes a $15/month credit. First ~30 days are free across all services. |
| Prod   | Pro x3  | ~$60/mo | Pro plan lifts the **5 GB volume cap** to **1 TB** and **6 days** log retention to **30 days**. |

### Cost Notes

- Railway Hobby: $5/mo per service (includes $5 credit).
- Railway Pro: $20/mo per service.
- Railway Object Storage: $0.015/GB-month. (Egress is billed separately.)
- GitHub Actions: ~$0.08/min (minutes consumed only when `RUNNER_OS=ubuntu` tasks run).

---

## 3. Backup Strategy

Railway managed PostgreSQL backs up volumes as **daily snapshots**.

| Schedule | Retention | RPO | How it's provided |
|----------|-----------|-----|-------------------|
| Daily snapshot | 6 days | Up to 24 hours | Railway sets this by default on a Postgres volume; no user action required. |

### RPO / RTO for MVP

- **RPO:** ≤24 hours (validated against Railway backup schedule)
- **RTO:** ≤4 hours (restoring a 5 GB volume takes minutes; restoring a 100 GB volume still takes <4h on a 4vCPU 16GB RAM runner)

### Important Limitations

- Railway **does not** offer point-in-time recovery (PITR) for Postgres.
- Railway **does not** offer continuous WAL archiving.
- If the requirement ever moves to ≤5-minute RPO in production, the team must implement a secondary backup mechanism (e.g., nightly `pg_dump` → Object Storage, or a self-managed WAL archiver).

---

## 4. Environment Variables — by Category

### 4.1. Core platform (required by this feature)

| Variable | Env (both envs) | Purpose |
|----------|----------------|---------|
| `ASPNETCORE_URLS` | Yes | Base URL(s) for ASP.NET Core to listen on. Must be `http://0.0.0.0:5000` to allow external binding in Railway. |
| `DATABASE_URL` | Yes | Connection string in **libpq URI** form. Railway injects a ready-made value; local dev must provide one. See **Connection String Strategy** below. |
| `MigrationModuleOrder` | Yes | Colon-separated list of DbContext types that own migrations, in topological order. The migration pipeline executes them in this order. |

### 4.2. Auth (soft dependency — `auth-foundation`)

When the `auth-foundation` feature is delivered, these variables are added. They are **not** required by the platform itself.

| Variable | Env (both envs) | Purpose |
|----------|----------------|---------|
| `Auth0__Domain` | Yes | Auth0 tenant domain (e.g. `dev.us.auth0.com`). |
| `Auth0__Audience` | Yes | Auth0 audience / API identifier used by the backend. |

### 4.3. Google OAuth (soft dependency — future feature)

| Variable | Env (both envs) | Purpose |
|----------|----------------|---------|
| `Google__ClientId` | Yes | Google OAuth Client ID (issued from Google Cloud Console). |
| `Google__ClientSecret` | Yes | Google OAuth Client Secret (issued from Google Cloud Console). |

### 4.4. Operational / shared (used by multiple features)

| Variable | Env (both envs) | Purpose |
|----------|----------------|---------|
| `Sentry_Dsn` | Yes | Sentry DSN (written to `SENTRY_DSN`). See **Monitoring** below. |
| `Logging__MinimumLevel` | Yes | Serilog minimum level for the hosting app. Must be `Warning` for Production to prevent leaking EF parameter values and request bodies into logs. |
| `Logging__AllowedProperties` | Yes | Comma-separated property names that are allowed to be logged. This is an allow-list; anything not listed is never logged. |

### 4.5. Module-scoped (soft dependency — future modules)

Each module may define its own scoped variables that are not shared across the board. These are defined in the module's `.env.example` and are **injected into the module's own environment** (if the module uses a dedicated API). For the shared API hosting feature in `src/api/`, no additional scoped variables are required.

---

## 5. Per-Environment .env Files

### 5.1. Development (local)

Create `src/api/.env.local` with:

```ini
ASPNETCORE_URLS=http://localhost:5000
ConnectionStrings__Default=
# Optionally set for debugging against a local database
# DATABASE_URL=postgresql://localhost:5432/projecttracker_dev
# Auth0__Domain=<dev.auth0.domain>
# Auth0__Audience=<dev.audience>
# Sentry_Dsn=https://...
Logging__MinimumLevel=Debug
Logging__AllowedProperties=Microsoft.AspNetCore,Microsoft.EntityFrameworkCore.Database.Command
```

> **Important:** `ConnectionStrings__Default` must be empty in local development so the application defaults to `localhost:5432/projecttracker_dev`. Do not set `DATABASE_URL` unless you are connecting to a Railway-managed instance (e.g., via a tunnel or when running locally against a remote DB).

### 5.2. Staging

Create `src/api/.env.staging`:

```ini
ASPNETCORE_URLS=http://0.0.0.0:5000
DATABASE_URL=postgresql://staging_user:***@ep-xxxxxx.railway.app/dbname
Sentry_Dsn=https://sentry-prod-user.prio-example@...
Logging__MinimumLevel=Warning
Logging__AllowedProperties=Microsoft.AspNetCore,Microsoft.EntityFrameworkCore.Database.Command
```

> Railway injects `DATABASE_URL`, `Sentry_Dsn`, and all other environment variables into this file **only** when deployed to the staging environment. Do not include secret values here.

### 5.3. Production

Create `src/api/.env.production`:

```ini
ASPNETCORE_URLS=http://0.0.0.0:5000
DATABASE_URL=postgresql://prod_user:***@ep-xxxxxx.railway.app/dbname
Sentry_Dsn=https://sentry-prod-user.prio-example@...
Logging__MinimumLevel=Warning
Logging__AllowedProperties=Microsoft.AspNetCore,Microsoft.EntityFrameworkCore.Database.Command
```

> Railway injects the actual secret values at deploy time. The `.env.production` file in source control is purely a template.

### 5.4. Railway Environment UI

When the repository is connected to a Railway project, Railway will **not** auto-fill secrets. You must go to:

1. Railway → Project (`project-tracker`) → Settings → Environment variables
2. Add each variable above to **both** the `staging` and `production` environments.
3. For `DATABASE_URL`, Railway provides a template value on the PostgreSQL service page. Copy it, remove any obviously placeholder tokens (e.g. `***`), and paste it into the staging and production environments.

> ⚠️ **Do not commit `.env.*` files with real values.** The `.env.example` in the repository is a template that documents every possible variable and shows the correct format.

---

## 6. Connection String Strategy

### Railway injects a `DATABASE_URL`

When the PostgreSQL service is connected to an environment, Railway injects a `DATABASE_URL` environment variable into that environment. The format is **libpq URI**, e.g.:

```
postgresql://backend_api:SuperSecretP@ssw0rd@ep-east-12345.railway.app/dbname
```

### The application translates it

The application reads `DATABASE_URL` and translates it into an `NpgsqlConnectionStringBuilder` object, adding `SSL Mode=Require`. This translation is performed by `DatabaseConnectionString` (D-4.3).

### The "fail fast" guarantee

In the `production` environment, if `DATABASE_URL` is missing or cannot be translated, the application will throw a `ConfigurationException` **during startup** rather than attempting to use a fallback localhost connection. This ensures that production never starts against a non-existent database.

In `development`, if `DATABASE_URL` is missing, the application defaults to `localhost:5432/projecttracker_dev`. This is why the `.env.local` file uses `ConnectionStrings__Default`.

### If Railway does not inject it (local or misconfigured)

For local development, provide a `ConnectionStrings__Default`:

```
ConnectionStrings__Default="Server=localhost;Database=projecttracker_dev;Username=postgres;Password=postgres;"
```

### Railway "Copy connection string" button

On the PostgreSQL service page, Railway provides a "Copy connection string" button. The copy-pasted value is the libpq URI. Store that value as `DATABASE_URL` in your Railway environment variables (for both staging and production).

---

## 7. Manual Checklist

Perform the following steps **in order**. Each step is atomic and must be verified before proceeding to the next.

### Step 1 — Create the project

1. Log in to [Railway](https://railway.app).
2. Click "New project".
3. Enter a project name: `project-tracker`.
4. Click "Create".

> **Goal:** A single Railway project named `project-tracker` with one `staging` and one `production` environment.

### Step 2 — Configure the PostgreSQL service

1. Click "Add service".
2. Choose "Postgres".
3. Choose "Create new database".
4. **Plan:** Hobby (for pilot).
5. **Name:** `db-default`.
6. Click "Add service".

Railway will create a managed PostgreSQL instance. Click the "⚙️" menu on the service and select "Environment variables". Railway will display a template `DATABASE_URL` for you.

> **Verify:** The service has status "Active". Click the "Get database credentials" button and note the `DATABASE_URL`.

### Step 3 — Configure the Web service

1. Click "Add service".
2. Choose "Static" → "Web static files".
3. **Name:** `web`.
4. Click "Add service".
5. Drag and drop the `src/web` folder from your repository into the static file picker (or set `src_dir` to `staticfiles`).
6. **Build command:** `npm run build`.
7. **Start command:** (leave empty, or `npx serve`).
8. Click "Add service".

> **Verify:** The service has status "Active".

### Step 4 — Configure the API service

1. Click "Add service".
2. Choose "GitHub Repo" → repository `Project Tracker`.
3. **Name:** `api`.
4. Open the service → **"Settings"** → **"Build"** and set **Root Directory** to `src/api`.
5. Confirm the **Builder** shows **Dockerfile**. Railway detects `src/api/Dockerfile` automatically; `src/api/railway.toml` also pins `builder = "DOCKERFILE"` and `dockerfilePath = "Dockerfile"`.
6. Leave **Build Command** and **Start Command** **empty**. They are owned by the Dockerfile and must not be set here — a value in either field overrides the Dockerfile and reintroduces the builder ambiguity this setup exists to remove.

> **Why a Dockerfile and not Nixpacks:** with Root Directory `src/api`, auto-detection (Nixpacks, and latterly Railpack) resolves the sibling `ProjectTracker.Api.sln`, which references the test project outside this build context, and fails with `MSB3202`. Railway also switched build drivers unannounced mid-setup, ignoring `railway.toml`'s build/start commands. An explicit Dockerfile removes auto-detection entirely. See `docs/specs/features/platform-foundation.md` Section 3, "D-6 Amendment".

> **Verify:** The build log shows Docker build stages (`sdk:8.0` → `aspnet:8.0`), and the service reaches status "Active".

### Step 5 — Configure the environment variables (staging)

1. Click the **"staging"** environment in the project dashboard.
2. Click **"Settings"** → **"Environment variables"**.
3. Click **"Add new"** and add:

| Key | Value |
|-----|-------|
| `DATABASE_URL` | `<copied-from-step-2>` |
| `MigrationModuleOrder` | `BoardDbContext:TaskDbContext:UserDbContext:MembershipDbContext` |
| `Board__AllowStubAuthorization` | `true` |

> **Note:** `ASPNETCORE_URLS` does not need to be set — `src/api/Dockerfile`'s `ENTRYPOINT` binds to Railway's injected `$PORT` dynamically at container start.
>
> **`Board__AllowStubAuthorization`** is a temporary bridge: `auth-foundation` has not shipped yet, so `StubBoardAuthorizationService` is the only `IBoardAuthorizationService` implementation available. It deliberately refuses to start in any non-Development environment unless explicitly opted in, to prevent accidentally shipping stub auth (every request treated as a `Member` on every board, no real identity check) to a real environment unnoticed. **Remove this variable once `auth-foundation` lands.** Note the double underscore (`__`), not a colon — ASP.NET Core's environment-variable configuration provider requires it.
>
> For now, leave `Sentry_Dsn` and `Logging__*` unset. They can be added when the feature is approved.

4. Click **"Save and redeploy"**.

> **Verify:** All three services (`api`, `web`, `postgres`) show "Deployed". The `api` service status transitions from `Running` → `Stopping` → `Starting` → `Running`.

### Step 6 — Configure the environment variables (production)

1. Click the **"production"** environment in the project dashboard.
2. Click **"Settings"** → **"Environment variables"**.
3. Click **"Add new"** and add:

| Key | Value |
|-----|-------|
| `DATABASE_URL` | `<copied-from-step-2>` |
| `Sentry_Dsn` | `<Sentry DSN or leave empty>` |
| `Logging__MinimumLevel` | `Warning` |
| `Logging__AllowedProperties` | `Microsoft.AspNetCore:Microsoft.EntityFrameworkCore.Database.Command` |
| `MigrationModuleOrder` | `BoardDbContext:TaskDbContext:UserDbContext:MembershipDbContext` |

> **Do NOT set `Board__AllowStubAuthorization` in production.** Production must fail fast and refuse to start with stub authorization — that is the whole point of the guard. Production access control is blocked on `auth-foundation` shipping first.
>
> `ASPNETCORE_URLS` does not need to be set here either, for the same reason as staging.

4. Click **"Save and redeploy"**.

> **Verify:** All three services in the production environment show "Deployed". The `api` service is healthy (see Step 7).

### Step 7 — Verify readiness (live health check)

1. Open the `api` service's "Health check" tab.
2. Verify that `/healthz/live` returns `200 OK` with `{ status: "ok" }`.
3. Verify that `/healthz/ready` returns `200 OK` with a structured response:
   ```json
   {
     "status": "ok",
     "checks": [
       { "name": "postgres", "status": "ok" },
       { "name": "pending-migrations", "status": "ok" }
     ]
   }
   ```

> **Verify:** `api` is **healthy and ready**.

### Step 8 — Configure backup retention

1. Click the PostgreSQL service → "⚙️" → "Settings".
2. Scroll to **"Volume backup"**.
3. Set **"Backup schedule"** to `daily`.
4. Set **"Backup retention"** to `6` days.
5. Click "Save changes".

> **Verify:** The backup status column shows "Daily backup completed" and the retention count is `6`. The next backup should appear within 24 hours.

> **Note:** These settings will be applied to **both** the staging and production environments once they are both configured, because they are volume-level settings.

### Step 9 — Confirm volume isolation

1. Click the PostgreSQL service → "Volumes" tab.
2. Confirm there are **two separate volumes**, one for staging and one for production, with distinct names or identifiers.
3. Note the storage usage for each.

> **Verify:** Staging and production databases are stored on separate volumes and are completely isolated.

### Step 10 — Note the volume name for restore runbook

1. Click the PostgreSQL service → "Volumes" tab.
2. Click on the staging volume.
3. Copy the **volume ID**. This ID is required by the restore runbook (`docs/specs/operations/runbook-restore.md`).

> **Record:** Write down the staging volume ID in a secure location. This is the value that will be referenced in the restore runbook.

---

## 8. Timing & Dependencies

| Step | Estimated time | Blocks |
|------|----------------|--------|
| 1. Create project | ~1 min | — |
| 2. Add PostgreSQL | ~3 min | Step 1 |
| 3. Add Web | ~2 min | Step 2 |
| 4. Add API | ~2 min | Step 3 |
| 5. Configure staging env vars | ~2 min | Step 4 |
| 6. Configure prod env vars | ~2 min | Step 5 |
| 7. Verify health checks | ~1 min | Step 6 |
| 8. Configure backups | ~1 min | Step 2 |
| 9. Confirm isolation | ~1 min | Step 8 |
| 10. Record volume ID | ~1 min | Step 9 |

**Total estimated time: ~15 minutes.**

---

## 9. Post-Verification Checks

Before moving on to the next feature chunk, confirm:

- ✅ Staging API responds to `/healthz/live` with `200`
- ✅ Staging API responds to `/healthz/ready` with `200` and both `postgres` and `pending-migrations` checks report `status: "ok"`
- ✅ Production API is running and healthy (same checks as above)
- ✅ Production logs show `INFO` level for application logs and `WARNING` level for EF Core command logging
- ✅ Sentry DSN is configured in the production environment (if Sentry feature is enabled)
- ✅ Backup schedule shows "Daily" with retention of 6 days
- ✅ Volume isolation: staging and production have separate volumes

If any of the above checks fail, **do not proceed**. Fix the configuration and re-verify.

---

## 10. Monitoring and Cost Review Checklists

### 10.1 External uptime monitor for `/healthz/ready`

Because the uptime pinger is external tooling, set it up as a third-party monitor rather than application code.

- [ ] Choose an uptime-monitoring provider approved by the team.
- [ ] Configure an HTTP check against the staging API's `/healthz/ready` endpoint.
- [ ] Configure a second HTTP check against the production API's `/healthz/ready` endpoint.
- [ ] Set the expected success code to `200 OK`.
- [ ] Alert on any non-200 response or timeout.
- [ ] Route alerts to the named on-call owner in `docs/specs/operations/on-call.md`.
- [ ] Verify the monitor follows redirects only if explicitly required by the provider.
- [ ] Record the monitor URL, alert policy, and owner handoff in the release notes or ops tracker.

### 10.2 Monthly Railway-spend and GitHub Actions-minutes review

Review the approved cost envelope once per month and escalate at the 80% threshold.

- [ ] Pull the current month-to-date Railway spend for staging and production.
- [ ] Pull the current month-to-date GitHub Actions usage.
- [ ] Compare spend to the approved monthly envelope.
- [ ] Compare Actions usage to the HLD TD-13 threshold of approximately **1,600 minutes for two consecutive months**.
- [ ] If either metric reaches 80% of the approved envelope, raise a review item to the release owner and product owner.
- [ ] If Actions usage hits ~1,600 minutes for two consecutive months, apply the TD-13 escalation response from the HLD.
- [ ] Record the review date, measured usage, decision, and any follow-up actions.
- [ ] Keep the review artifact with the platform operations docs so the next month can be compared against it.

---

## 11. Rollback Procedure

If a deploy breaks the API (e.g. a migration fails and the new API cannot start), the rollback steps are:

1. In the Railway project dashboard, switch to the **production** environment.
2. Click the broken `api` service.
3. Click **"Deploy"** → **"Deploy to this environment"**.
4. Select the **previous deployment** from the version history.
5. Click "Deploy".
6. Wait for the service to start and verify `/healthz/ready` returns `200`.

> **Note:** Rolling back the API effectively keeps the previously deployed migrations in place. If the database was migrated by the previous API version and the new API code expects a different schema, you will need to manually inspect the schema and determine whether to roll back the database as well. In most cases with expand/contract-compatible migrations (D-2), rolling back the code is sufficient.

---

## Appendix A — Variable Reference Table

| Variable | Source | Required | Scope |
|----------|--------|----------|-------|
| `ASPNETCORE_URLS` | `.env.*` files | ✅ Yes | Platform (all environments) |
| `DATABASE_URL` | Railway env var | ✅ Yes | Platform (all environments) |
| `MigrationModuleOrder` | `.env.*` files | ✅ Yes | Platform (all environments) |
| `Sentry_Dsn` | Railway env var | ✅ Yes (if Sentry is used) | Platform (all environments) |
| `Logging__MinimumLevel` | `.env.*` files | ✅ Yes | Platform (all environments) |
| `Logging__AllowedProperties` | `.env.*` files | ✅ Yes | Platform (all environments) |
| `Auth0__Domain` | Railway env var | ⏳ Pending auth-foundation | Auth (staging + production) |
| `Auth0__Audience` | Railway env var | ⏳ Pending auth-foundation | Auth (staging + production) |
| `Google__ClientId` | Railway env var | ⏳ Pending Google feature | Google (future) |
| `Google__ClientSecret` | Railway env var | ⏳ Pending Google feature | Google (future) |
| `ConnectionStrings__Default` | `.env.*` files | ✅ Yes (Development only) | Development only |

---

## Appendix B — Document History

| Date | Author | Change |
|------|--------|--------|
| 2026-10-13 | <your-name> | Initial creation — document Railway topology, environment variables, backup strategy, and manual checklist for the `platform-foundation` feature. |
