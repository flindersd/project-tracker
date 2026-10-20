# On-call and alert routing

## Owner

- **Primary owner:** Flinders Doyle (flinders.doyle@gmail.com)
- **Backup owner:** TBD — product owner to confirm

> Primary owner confirmed by the product owner on 2026-10-20. The backup owner remains an open item and should be confirmed before launch.

## Alert classes routed here

- Sentry application errors and latency regressions
- Readiness / uptime failures on `/healthz/ready`
- Npgsql pool saturation warnings
- Monthly Railway spend / GitHub Actions allowance review findings

## Escalation path

1. Primary owner acknowledges the alert.
2. If the primary owner is unavailable, notify the backup owner.
3. If the issue affects production readiness or backup safety, escalate to the release owner and product owner.
4. Record the incident, root cause, and follow-up item in the team tracker.

## Contact record maintenance

- Keep this file in sync with `.github/CODEOWNERS`.
- The backup owner is still a placeholder — confirm with the product owner before launch.
- Do not ship a production release with unresolved ownership for backup-failure alerts.
