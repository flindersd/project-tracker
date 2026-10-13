# Platform Foundation — Gherkin Scenarios (Phase 1)

Scope: automated, code-level verification of the acceptance criteria in
`docs/specs/features/platform-foundation.md` that are expressible as unit or
integration tests against the API process. Several Section 1 ACs (Railway
provisioning, CI/CD workflow wiring, monthly cost review, the restore drill) are
infrastructure/process deliverables with no code surface to assert against from
inside the test suite — these are listed as gaps in Section 4 of the spec, not
silently skipped.

Covers: D-3 (real readiness), D-4.1–D-4.3 (CORS allow-list, fail-fast connection
config, `DATABASE_URL` URI translation), D-5 (EF command log level), D-6
(`railway.toml` startCommand/healthcheckPath correctness).

---

## 1. Readiness health check (D-3, AC "a failed readiness check prevents promotion")

1.1
Given the configured PostgreSQL database is unreachable
When `GET /healthz/ready` is called
Then the response status is 503
And the response body names the failing check (e.g. contains `"postgres"` or `"database"`)

1.2
Given the configured PostgreSQL database has pending (unapplied) migrations for `BoardDbContext`
When `GET /healthz/ready` is called
Then the response status is 503

1.3
Given the configured PostgreSQL database is reachable and all `BoardDbContext` migrations are applied
When `GET /healthz/ready` is called
Then the response status is 200
And the response body reports a structured per-check breakdown (not just a bare `{"status":"ok"}` constant), including an entry for database connectivity

---

## 2. CORS allow-list (D-4.1, AC "connection strings... never bundled... "; NFR security)

2.1
Given `Cors:AllowedOrigins` is configured to `["https://staging.example.com"]`
When a cross-origin request is sent with `Origin: https://staging.example.com`
Then the response includes `Access-Control-Allow-Origin: https://staging.example.com` (the literal origin, not `*`)
And the response includes `Access-Control-Allow-Credentials: true`

2.2
Given `Cors:AllowedOrigins` is configured to `["https://staging.example.com"]`
When a cross-origin request is sent with `Origin: https://evil.example.com`
Then the response does not include an `Access-Control-Allow-Origin` header

---

## 3. Fail-fast connection configuration (D-4.2, AC "configuration only from server-side environment variables")

3.1
Given the environment is `Production`
And no `ConnectionStrings:Postgres` and no `DATABASE_URL` is configured
When the application host starts
Then startup fails (throws) rather than silently falling back to a hardcoded local connection string

---

## 4. `DATABASE_URL` libpq-URI translation (D-4.3)

4.1
Given `DATABASE_URL` is set to a libpq URI (`postgresql://user:pass@host:port/db`) pointing at a real reachable PostgreSQL instance
When a request that requires database access is made (e.g. `GET /api/boards`)
Then the request succeeds (does not fail with a connection-string parse error)

---

## 5. EF Core command log level (D-5, "request bodies are never logged" / no parameter-value leakage)

5.1
Given the application's logging configuration (`appsettings.json` and `appsettings.Development.json`)
When the `Microsoft.EntityFrameworkCore.Database.Command` category is inspected
Then its configured level is `Warning` or higher in every environment (never `Information` or more verbose, which would log parameter values)

---

## 6. `railway.toml` deploy configuration (D-6)

6.1
Given `src/api/railway.toml`
When its `[deploy]` section is inspected
Then `startCommand` references a fully-qualified path to the published DLL (not a bare filename that cannot resolve against the Nixpacks publish output)

6.2
Given `src/api/railway.toml`
When its `[deploy]` section is inspected
Then `healthcheckPath` is set to `/healthz/ready`

---

## Not covered by this test suite (see Section 4 gaps)

- Railway project/environment/service provisioning (Chunk 1) — manual console checklist, no code surface.
- GitHub Actions workflow behaviour beyond what unit tests can assert (fast/full gate timing, concurrency cancellation, required-reviewer environment protection) — verified by workflow run, not unit tests.
- Sentry wiring, uptime monitoring, DB pool saturation alerting, monthly cost review — operational configuration, verified manually.
- The restore drill and measured RTO (D-8) — must be an executed, recorded drill, not a test.
- Migration bundle CI job ordering (D-2) — a deploy-pipeline behaviour, not unit-testable in isolation without a live CI run.
