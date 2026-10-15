# Handover

What is delivered, what is not, and what a future developer needs to know.

Required by requirements §38–39.

---

## What this is

SPFM is an internal web application for Shield Security Doors. A Shield user signs
in with their Microsoft work account, creates or opens a project, enters the SPFM
figures, runs the calculations, saves, and can reopen, edit and recalculate later —
with a record of who changed what.

| | |
|---|---|
| Backend | ASP.NET Core 9, C# |
| Database | PostgreSQL 16, EF Core 9 |
| Frontend | Next.js 16, TypeScript |
| Sign-in | Microsoft Entra ID |

---

## The documents

| | |
|---|---|
| [SYSTEM-DESIGN.md](SYSTEM-DESIGN.md) | Architecture and the reasoning behind each decision |
| [TEST-RUNBOOK.md](TEST-RUNBOOK.md) | Step-by-step manual testing, with requirement coverage |
| [API-CONTRACT.md](API-CONTRACT.md) | Every endpoint, verified by tests |
| [REQUIREMENTS-COVERAGE.md](REQUIREMENTS-COVERAGE.md) | Section-by-section status |
| [ROLLBACK-PLAN.md](ROLLBACK-PLAN.md) | What to do when a deployment goes wrong |
| [ENTRA-SETUP.md](ENTRA-SETUP.md) | Microsoft sign-in configuration |
| [DEPLOYMENT.md](DEPLOYMENT.md) | Deploying to the hosting environment |
| [DEPLOY-SETTINGS.md](DEPLOY-SETTINGS.md) | Exact Render and Vercel settings, and free-tier caveats |
| [QUICKBOOKS-MAPPING.md](QUICKBOOKS-MAPPING.md) | How the schema is prepared for Phase 2 |
| [../README.md](../README.md) | Getting it running locally |

No document contains a secret.

---

## Running it

See the README. In short: start PostgreSQL, apply the migrations, run the API, run
the frontend. The API refuses to start on missing configuration and names what it
needs, rather than failing later on each request.

### Configuration

All environment-driven (§31). Nothing secret is committed.

| Variable | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL |
| `AzureAd__TenantId` / `__ClientId` / `__Audience` | API app registration |
| `Cors__AllowedOrigins__0` | Frontend origin |
| `NEXT_PUBLIC_API_BASE_URL` | Where the frontend finds the API |
| `NEXT_PUBLIC_ENTRA_*` | SPA registration — public ids only |

`NEXT_PUBLIC_*` values are baked in at build time, so changing one needs a rebuild.
They are public identifiers; a single-page app registration has no secret.

---

## Tests

| Suite | Count | Needs |
|---|---|---|
| Unit — calculations, validation, fingerprint | 23 | nothing |
| Schema — EF model invariants | 16 | nothing |
| Cursor and change tracking | 24 | nothing |
| Integration — repository against PostgreSQL | 12 | a database |
| API — over HTTP, against PostgreSQL | 22 | a database |
| End-to-end — the Alpha loop in a browser | 10 | a browser |

```bash
dotnet test --filter "Category!=AcceptanceGate"   # implementation
cd frontend && npm test && npm run test:e2e
```

### The acceptance gate

`dotnet test --filter "Category=AcceptanceGate"` **fails on purpose.**

It reports that the SPFM calculations have never been checked against real Shield
projects. Requirements §14 and §33 require 2–3 real projects with known results;
none have been supplied.

**Do not delete this test to make the build green.** Add the real projects to
`backend/SPFM.Tests/Calculations/real-projects.json` and it passes. Until then it
is the honest status of the most important part of the system.

---

## What a future developer must not break

**`SPFM.Domain` has no dependencies.** Not EF Core, not ASP.NET, not HTTP. That is
what lets the calculation engine be tested on its own (§11). If it ever stops being
testable without a database, the rule has been broken.

**Money is never a floating-point type.** `decimal` in C#, `numeric(18,4)` in
PostgreSQL. The browser formats and displays; it does not compute the figures of
record. There is a test asserting no `double` or `float` exists anywhere in the
model.

**Calculation runs are append-only.** Calculating again adds a row. Never update one
in place: the history of what the figures said, and which engine produced them, is
part of the audit trail.

**Migrations never run at application start.** Concurrent instances would race.
Run them as an explicit deployment step.

**The authorization fallback policy is default-deny.** A new endpoint is protected
the moment it exists. Opting out requires `[AllowAnonymous]`, which is visible in
review.

---

## Known limitations

Stated plainly rather than discovered later.

### Blocking acceptance

| | |
|---|---|
| **The SPFM formulas are placeholders** | `SpfmCalculator` implements standard job-costing arithmetic as a stand-in. `EngineVersion` is `placeholder-1.0.0`, so any stored result computed from invented formulas is identifiable. |
| **The field list is provisional** | The current inputs are a working shape. Extra fields land in `extra_inputs_json` until the real list is confirmed, then get promoted to typed columns. |
| **Rounding rules are assumed** | Commercial rounding, 2dp for currency, 4dp intermediate. Unconfirmed. |
| **No real-project validation** | See the acceptance gate above. |
| **Project statuses are invented** | §28 warns against this; they are placeholders. |
| **Project numbering is invented** | `SSD-{year}-{nnn}`. The real format, and whether QuickBooks owns it, is unknown. |

### Not yet exercised

| | |
|---|---|
| **A real Microsoft sign-in against the live API** | Needs a Shield work account. Token *rejection* is tested with the genuine Entra handler; acceptance of a valid token is not. |
| **Deployment** | Dockerfiles and CI exist; nothing has been deployed. |
| **The rollback plan** | Written, never rehearsed. |

### Deliberately out of scope (§42)

QuickBooks import · user roles · permissions · data exports · advanced error
monitoring · payments · customer-facing site · mobile app.

---

## Recommended next steps

1. **Get the formulas, field list, rounding rules and 2–3 real projects.** Everything
   else is secondary; until these arrive the calculations are unverified.
2. **Deploy and run the Alpha loop in production** (§37).
3. **Add a staging environment.** Migrations currently reach production untested
   against real data. This is the highest-value addition after the Alpha.
4. **Rehearse the rollback plan** once, before relying on it.
5. **Quote Phase 2 (QuickBooks)** separately, as §18 and §43 recommend.

---

## Support

Two months of priority support from delivery, covering bugs in delivered
functionality, small adjustments, deployment help, clarification, and Alpha
stabilisation. Substantial new functionality is new development.
