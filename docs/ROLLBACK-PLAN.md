# Rollback plan

What to do if a deployment introduces a serious problem.

Required by requirements §35 and included in the accepted Growth Plan.

---

## The principle

**Application code rolls back freely. Database migrations do not.**

A container image can be swapped back in minutes with no lasting consequence. A
migration that has dropped a column has destroyed data, and no amount of
redeploying brings it back. Every rule below follows from that asymmetry.

---

## Before every deployment

| Step | Why |
|---|---|
| 1. Take a database backup and **confirm it completed** | An untested backup is a hope, not a plan |
| 2. Note the currently deployed version (image tag / commit SHA) | You cannot roll back to a version you cannot name |
| 3. Review the migration SQL by hand | `dotnet ef migrations script --idempotent` — read it before it runs |
| 4. Confirm the migration is additive (see below) | Decides whether rollback is cheap or expensive |

Azure Database for PostgreSQL Flexible Server takes automatic daily backups with
point-in-time restore. For a deployment carrying a migration, take an **on-demand
backup immediately beforehand** regardless — automatic backups are a safety net,
not a deployment step.

---

## Migration rules

**Additive changes are safe.** Adding a table, adding a nullable column, adding an
index. The previous application version ignores them and keeps working, so code can
roll back without touching the database at all.

**Destructive changes need two deployments.** Dropping or renaming a column,
narrowing a type, making a column `NOT NULL`:

```
Deployment 1   add the new column, write to both, keep the old one
               (both versions of the app work against this schema)
Deployment 2   once deployment 1 is proven, stop writing the old column
Deployment 3   drop the old column, only after the data is known good
```

Tedious, and the reason a bad deploy stays a five-minute problem rather than a
restore-from-backup problem.

**Migrations never run automatically at application start.** Two instances starting
together would race, and a failed migration would leave the app crash-looping with
the schema half-changed. Run them as an explicit, reviewable deployment step:

```bash
dotnet ef database update \
  --project backend/SPFM.Infrastructure \
  --startup-project backend/SPFM.Infrastructure
```

---

## If something goes wrong

### Case 1 — the deployment carried no migration

The common case, and the easy one.

1. Redeploy the previous image tag
2. Confirm `/health` and `/health/ready` return 200
3. Sign in and open a project

**Time to recover: minutes. No data risk.**

### Case 2 — the deployment carried an additive migration

Also recoverable without touching data, because the old code ignores the new columns.

1. Redeploy the previous image tag
2. **Leave the migration in place** — do not reverse it
3. Verify as above

**Time to recover: minutes. No data risk.**

Reversing an additive migration here would be the mistake: the new columns are
harmless, and dropping them may destroy data already written to them.

### Case 3 — the deployment carried a destructive migration

The expensive case, and the reason for the two-deployment rule above.

1. **Stop writes immediately** — take the application offline rather than let it
   accumulate data against a schema you are about to roll back
2. Assess: has anything been written since the deploy?
   - **No** → restore the pre-deployment backup, redeploy the previous image
   - **Yes** → do not blind-restore. Restoring discards everything written since.
     Take a copy of the current database first, then decide what must be merged
     back by hand
3. Redeploy the previous image tag
4. Verify, then re-open the application

**Time to recover: an hour or more, and data loss is possible.**

### Case 4 — the application is fine but the data is wrong

Bad figures from a formula change rather than a failed deployment.

Calculation runs are **append-only**: every result keeps the inputs it came from and
the `EngineVersion` that produced it. So this does not need a restore.

1. Find the affected runs: `SELECT * FROM calculation_runs WHERE engine_version = '<bad version>'`
2. Correct the formula, bump `EngineVersion`, deploy
3. Recalculate the affected projects

The previous figures stay in the table as history. Nothing is lost, and the change
history shows exactly what happened.

---

## Verifying a rollback worked

Not "the site loads". Run the Alpha loop:

1. `/health` returns 200, `/health/ready` returns 200
2. Sign in with a Shield Microsoft account
3. Open an existing project — figures match what they were before
4. Edit, recalculate, save
5. Change history shows the edit, attributed correctly
6. Sign out and back in; reopen the project; data intact

If step 3 shows different figures from before the incident, **stop** — that is a
data problem, not a deployment problem, and case 4 applies.

---

## What must be recorded

Keep for every deployment:

- Image tag / commit SHA deployed, and the one it replaced
- Migration name applied, if any
- Backup identifier taken beforehand
- Who deployed, and when

Without the previous tag and the backup id, neither rollback path above can be
executed under pressure.

---

## Known limitations of this plan

Stated plainly, because a rollback plan that overstates its coverage is worse than
none:

- **It has never been rehearsed.** It is written from the system's design, not from
  a drill. The first real rollback will be the first test of it.
- **Restore time is unmeasured.** Azure point-in-time restore on a small instance is
  typically minutes, but this has not been timed on Shield's actual instance.
- **Single environment.** There is no staging environment in the current scope, so
  migrations are first applied for real in production. A staging slot would be the
  single highest-value addition after the Alpha.
- **One user today.** Case 3's "has anything been written since" is answerable by
  asking Samuel. With more users it needs a query, not a conversation.
