# On-call and alert routing

## Owner

- **Primary owner:** TBD — product owner to confirm
- **Backup owner:** TBD — product owner to confirm

> **Open item:** No real named operational owner was provided for this implementation slice. This document intentionally uses placeholders and must be updated by the product owner before the feature can be considered fully complete.

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
- Update the placeholder owner entries as soon as the product owner provides the named individual and contact details.
- Do not ship a production release with unresolved ownership for backup-failure alerts.
