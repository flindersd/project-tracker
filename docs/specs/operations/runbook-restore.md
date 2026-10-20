# Restore Runbook — Platform Foundation

## 1. Purpose

This runbook describes how to restore the staging PostgreSQL volume for Project Tracker, verify the API, communicate the outcome, and decide on escalation or rollback actions.

This is the operational procedure required by Section 3 D-8. It must be executed against real staging infrastructure and recorded with measured RTO evidence before M1 release.

## 2. Ownership

- **Primary owner:** Flinders Doyle (flinders.doyle@gmail.com)
- **Technical support:** release owner / backend engineer on duty
- **Approval for production-impacting communications:** product owner

## 3. Scope

- **Drill target:** staging only
- **Do not rehearse on production**
- **Restore target:** the staging PostgreSQL volume
- **Success criteria:** the API returns `200 OK` from `/healthz/ready` after the restore and the measured wall-clock time is recorded

## 4. Prerequisites

- [ ] Staging Railway environment exists and is healthy before the drill starts.
- [ ] The current staging volume ID is known and recorded.
- [ ] The on-call owner and backup owner are reachable.
- [ ] The latest successful backup/snapshot is available in Railway.
- [ ] The API deployment is pointing at the staging database that will be restored.
- [ ] A timer is ready to record the wall-clock elapsed time.
- [ ] A place to capture evidence exists: screenshots, timestamps, and the final readiness response.

## 5. Preconditions and Safety Checks

1. Confirm the drill is approved for staging.
2. Confirm the restore target is the staging volume only.
3. Confirm no live customer traffic depends on staging data.
4. Confirm the restore will not overwrite production resources.
5. Announce the drill window to the release owner and product owner.

## 6. Execution Procedure

1. Start the timer and record the baseline timestamp.
2. In Railway, open the staging PostgreSQL service.
3. Choose the most recent valid snapshot for restore.
4. Restore the staging volume according to Railway's console flow.
5. Wait for the restored volume to attach and the database service to return to an active state.
6. Redeploy or recycle the staging API if Railway does not automatically reconnect it.
7. Poll `/healthz/ready` until it returns `200 OK`.
8. Capture the final readiness body and the time at which the check first succeeded.
9. Stop the timer and compute the elapsed wall-clock duration.
10. Record the measured RTO, the snapshot identifier, the restored volume identifier, and any manual steps taken.

## 7. Verification Steps

- [ ] `/healthz/live` returns `200 OK`.
- [ ] `/healthz/ready` returns `200 OK`.
- [ ] The readiness body shows the database check passing.
- [ ] The API can serve a representative authenticated-less request path used by the platform baseline.
- [ ] Logs show the restored staging API is connected to the restored database.
- [ ] The measured time from restore start to readiness success is recorded.

## 8. Communications

### During the drill

- Notify the release owner when the drill starts.
- Notify the release owner if the restore is taking longer than expected.
- Escalate to the product owner if the restore appears to exceed the 4-hour target.

### After the drill

- Report the measured RTO.
- Report whether the recovery target was met.
- Report any data loss, manual intervention, or anomalies.
- Attach evidence to the release notes or ops tracker.

## 9. Rollback / Escalation Actions

If the restore fails or the API does not return healthy within the target window:

1. Stop the drill timer and record the failure point.
2. Keep the impacted staging environment isolated.
3. Re-attempt restore only if the failure mode is clearly understood and safe to retry.
4. Escalate to the release owner and product owner immediately.
5. If the failure indicates backup corruption or an unsafe restore path, open an incident and block release.
6. Document the root cause and the next corrective action before the drill is considered complete.

## 10. Drill Record Template

- **Date:** 
- **Owner:** Flinders Doyle (flinders.doyle@gmail.com)
- **Environment:** staging
- **Staging volume ID:** 
- **Snapshot ID:** 
- **Restore start time:** 
- **Readiness success time:** 
- **Measured RTO:** 
- **Target RTO:** ≤4 hours
- **Outcome:** pass / fail
- **Notes:** 

## 11. Executed Drill Evidence

**NOT YET PERFORMED — tracked as a follow-up item, owner: Flinders Doyle (flinders.doyle@gmail.com).**

This requires a human operator with Railway console access to execute the restore drill against the real staging environment and record actual measured results (wall-clock RTO, volume/snapshot IDs, timestamps). It cannot be performed by a build agent in a sandboxed environment and must be completed before M1 release.

This section must be completed with real evidence only. Do not fabricate timestamps, screenshots, or the measured RTO. Until this is filled in, FR-12 and the Section 1 restore-drill acceptance criterion remain open — this is accepted as a known, explicitly tracked gap for this build pass, not a silent omission.
