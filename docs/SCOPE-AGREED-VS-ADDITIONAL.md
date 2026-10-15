# SPFM — Scope: what was agreed, and what is additional

Mapping the 24 tickets in *SPFM Developer Backlog — Source of Truth* (15 Oct 2026)
against the scope accepted on Fiverr in October 2026.

**In one line:** the agreed scope bought a project workspace with SPFM calculations and
a database *prepared* for QuickBooks. The backlog requires a QuickBooks-connected
revenue-recognition and receivables system. The foundation carries over; the product
built on it is substantially larger.

---

## 1. What the accepted offer included

Quoted verbatim from the agreed custom offer:

> - Repository review and confirming the starting state
> - Microsoft sign in on the frontend (MSAL), connected to your API
> - Lean project data model, database tables and project API
> - Home screen with search, plus the main project screen with validation
> - SPFM calculations in code, with automated tests
> - Create, save, reopen, edit and recalculate projects
> - Deployed to your internal hosting, with README and setup documents
> - Validation against 2 or 3 real Shield projects and their known results
> - Named test cases with expected results handed over
> - Change history showing who edited what, and when
> - **Data model shaped to match QuickBooks, ready for Phase 2**
> - Second full testing pass and a rollback plan
> - 2 months of priority support
>
> **Not included: user roles, exports, error monitoring and the QuickBooks import
> (quoted separately).**

Two clauses govern everything below:

- **"Data model shaped to match QuickBooks, ready for Phase 2"** — the database was to
  be *prepared* for QuickBooks. That was delivered.
- **"the QuickBooks import (quoted separately)"** — the QuickBooks connection itself
  was explicitly outside the price.

---

## 2. Ticket-by-ticket mapping

### Included in the agreed scope — no additional cost

| # | Ticket | Covered by |
|---|---|---|
| 2 | SPFM domain model and database migrations | "Lean project data model, database tables" |
| 3 | Financial calculation service with explicit formulas | "SPFM calculations in code, with automated tests" |
| 7 | Projects list rebuilt around financial attention states | "Home screen with search" |
| 8 | Project financial summary header | "Main project screen" |
| 10 | Persistence, reopen/edit, status lifecycle | "Create, save, reopen, edit and recalculate" |
| 23 | Real-project end-to-end validation | "Validation against 2 or 3 real Shield projects" |
| 24 | Deploy the usable internal Alpha | "Deployed to your internal hosting" |

**7 tickets.**

### Partly covered

| # | Ticket | Position |
|---|---|---|
| 1 | Freeze the current direction, preserve the shell | New work, but small. Absorbed at no charge as part of realignment |
| 18 | Explicit overrides and correction audit trail | "Change history" was included; a structured override workflow with reasons, prior values and traceable corrections is beyond it |

**2 tickets.**

### Additional — outside the agreed scope

| # | Ticket | Why it is additional |
|---|---|---|
| 4 | QuickBooks OAuth connection | "QuickBooks import — quoted separately" |
| 5 | Synchronise customers, estimates, invoices, payments | Same |
| 6 | Create and link projects from QuickBooks records | Depends entirely on 4–5 |
| 9 | Show linked QuickBooks activity on the project page | Requires synchronised QuickBooks data |
| 11 | Shipment entry and full-shipment workflow | Shipments were not part of the agreed data model or workflow |
| 12 | Deferred versus recognised revenue engine | A revenue-recognition engine was not in the agreed calculations |
| 13 | Partial-shipment allocation workflow | New workflow with its own validation and persistence |
| 14 | Recognition period / effective-date handling | Accounting-period logic was not in scope |
| 15 | Work Queue framework as the home screen | A new primary workspace, replacing the agreed home screen |
| 16 | Unmatched / ambiguous QuickBooks record resolution | Requires QuickBooks sync; new resolution workflow |
| 17 | Accounts receivable and collections workflow | A/R depends on QuickBooks invoices and payments |
| 19 | Accounting period / Financial Review workspace | A further new workspace |
| 20 | Accountant-ready period reporting / export | "Exports" explicitly excluded |
| 21 | Historical project migration and linking | Requires QuickBooks sync; new workflow |
| 22 | Project close and 7-day auto-archive | New lifecycle automation |

**15 tickets.**

---

## 3. Summary

| | Tickets |
|---|---|
| Included in the agreed scope | **7** |
| Partly covered / absorbed | **2** |
| Additional | **15** |

The additional 15 contain the technically heaviest work in the backlog: the QuickBooks
integration, the revenue-recognition engine and partial-shipment allocation.

---

## 4. Why QuickBooks is the dividing line

The design document states:

> *"'QuickBooks import comes in Phase 2' is not aligned with the product design.
> QuickBooks synchronization is foundational to SPFM and belongs at the beginning of
> the usable product path."*

This is correct as product direction, and the build should follow it. It is also
precisely the item the accepted offer placed outside the price.

It is not a single feature. **Every ticket from 6 onward depends on synchronised
QuickBooks data.** Without it there are no invoices, no payments, no receivables, no
deferred revenue, and nothing for the Work Queue to present. The item deferred as a
later phase is, under the new requirements, the floor of the product.

---

## 5. What is already built and carries over

Delivered under the agreed scope and reusable without change:

- Microsoft Entra sign-in, end to end
- PostgreSQL database with migrations, users, customers and projects
- Project create / list / search / open / edit / save, with reopen and persistence
- Append-only history with who, what, old value, new value and when
- Optimistic concurrency, so two people cannot silently overwrite each other
- REST API with default-deny authorisation and structured error responses
- 120 automated tests: unit, database integration, API over HTTP, and end-to-end
- CI pipeline, deployment configuration, rollback plan and handover documentation

The design document's own assessment: authentication and the account shell **KEEP**;
projects list, search and status shell **KEEP / REWORK**; project identity fields
**KEEP / REWORK**.

What is removed from the core, as the client directs: manual doors/hardware entry,
labour, subcontractor and overhead costs, and the gross-margin dashboard.

---

## 6. Proposed way forward

1. **Begin Tickets 1–3 immediately at no additional cost.** Realignment, the SPFM domain
   model and the shared calculation service. These sit inside the agreed scope and are
   prerequisites for everything else.
2. **Provide a separate estimate for Tickets 4–6 and 11–22**, the QuickBooks foundation
   and the financial workflows built on it.
3. **Agree the commercial position before Ticket 4**, which is where the work leaves the
   agreed scope.

This keeps delivery moving while the additional scope is settled, and avoids the
question surfacing late in the build.

---

*Prepared 16 October 2026. Reference: `docs/client-source-of-truth/` for both client
documents, and the superseded `SPFM_Requirements.pdf` that scoped the original offer.*
