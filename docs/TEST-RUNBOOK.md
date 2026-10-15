# SPFM Alpha — Test Runbook

How to test the application yourself: what it is for, what the client asked for,
how to check each part, and an honest account of what is finished and what is not.

Tick the boxes as you go.

---

## What SPFM is for

An internal web application replacing the spreadsheets Shield Security Doors uses to
cost projects. A Shield user signs in with their Microsoft work account, enters the
figures for a job — doors supplied, labour, subcontractors, overhead — runs the SPFM
calculations, and saves. Later they reopen it, change a figure, recalculate, and the
system records who changed what.

**The acceptance condition**, in the client's own words (§46–47):

> A Shield user can securely log in with Microsoft, create or open a project, enter
> valid SPFM data, run accurate calculations, save the project, reopen it later, edit
> it, recalculate it, and see the relevant change history — on the deployed
> production environment.

---

## Before you start

Sequences 1–9 need only the frontend. Everything runs in **preview mode**: sign-in is
simulated and projects are stored in your browser, so no Microsoft account and no
database are involved.

```bash
cd D:\sam\spfm-built-by-us\frontend
npm run dev
```

Then open <http://localhost:3001> (or 3000 if it is free).

> **Reset between runs.** Preview data lives in your browser. To start clean, use
> *Reset sample data* in the sidebar, or open an incognito window. Do that before
> sequence 4 so the project numbers below line up.

---

## 1 · Signing in
*§5, §23, §45*

- [ ] **Open the app.** → Redirected to `/login`, not the dashboard. The right panel
      reads "Preview mode: sign-in is simulated."
      *Unauthenticated users must never reach project data.*
- [ ] **Click "Sign in with Microsoft".** → The button briefly shows "Signing in…",
      then the Overview loads.
- [ ] **Sign out from the sidebar, then type `/projects` into the address bar.**
      → Bounced back to `/login`.
      *§5 asks for unauthorized access to be handled, not just hidden.*

## 2 · The dashboard
*§6*

- [ ] **Read the five tiles.** → Total projects, contract value, gross profit, gross
      margin, net profit after overhead.
- [ ] **Check the "Needs calculating" panel.** → Lists any project whose figures are
      missing or older than its inputs.
- [ ] **Click a project name in "Recently updated".** → It opens that project.

## 3 · Finding a project
*§7, §27*

- [ ] **Go to Projects, type `shutter` in the search box.** → The list narrows as you
      type. Search covers name, customer and project number.
- [ ] **Clear it and try each status filter.** → All / Quotes / Active / Complete /
      Archived each filter correctly.
- [ ] **Work through all five "Sort by" orders.** → Recently updated, name A–Z,
      project number, customer A–Z, contract value high to low.
      *§27 asks specifically for sorting, alphabetical and project number.*

## 4 · Creating a project
*§7, §8, §9* — reset the sample data first

- [ ] **Click "New project".** → An empty form with one blank product line.
- [ ] **Enter a name and customer, then fill a line** (description, quantity, unit
      cost, unit price). → The line total updates as you type.
- [ ] **Click "Add line", fill a second item, then remove it with the bin icon.**
      → Lines add and remove freely.
- [ ] **Fill in labour hours, labour rate and overhead %.** → All accept decimals.

## 5 · Validation
*§10, §22* — the point is that bad data cannot be saved silently

- [ ] **Clear the project name and click Calculate.** → "Enter a project name."
      appears under the field, and a banner counts the fields needing attention.
      *§10 asks for understandable messages, not `ValidationException: NullReference…`.*
- [ ] **Set overhead % to 150.** → "Enter a percentage between 0 and 100."
- [ ] **Set a quantity to 0 and a unit cost to a negative number.** → Each error
      appears against its own field, not lumped together.
- [ ] **Delete every product line and calculate.** → "Add at least one door or
      product line."

## 6 · Calculating
*§11, §16*

- [ ] **Fix the errors, then click Calculate.** → Results fill in: contract value,
      costs, gross profit, overhead, net profit, margins.
- [ ] **Read the note under the results.** → An **"Example figures"** warning saying
      these come from placeholder formulas.
      *Nobody should mistake stand-in arithmetic for Shield's real numbers.*
- [ ] **Make a loss-making job:** unit price barely above unit cost, high overhead %.
      → Net profit goes negative and shows in red, not clamped to zero.
- [ ] **Set every unit price to 0 and recalculate.** → Margins show as "–", not 0%.
      *A project with no revenue has no margin, which is not the same as a zero margin.*

## 7 · The Alpha loop
*§8, §16, §46* — **the sequence the whole project is judged on. No shortcuts.**

- [ ] **With a calculated project, click "Create project".** → Saves, the address
      becomes `/projects/<id>`, "Project created" confirms.
- [ ] **Go to Projects and find it with the search box.** → Appears with its number,
      contract value and margin.
- [ ] **Open it again.** → Every figure exactly as you left it. Badge reads
      "Calculated".
- [ ] **Change labour hours.** → Badge changes to "Needs recalculating" and the
      figures dim.
      *§16: stale results must never be presented as current.*
- [ ] **Click Recalculate, then Save project.** → New figures, badge back to
      "Calculated", save confirmed.
- [ ] **Reload the page (F5).** → Edited values and new results survive.
- [ ] **Sign out, sign back in, reopen the project.** → Still intact. **This is the
      complete loop the Alpha is accepted on.**

## 8 · Change history
*§17*

- [ ] **Scroll to "Change history" on the project.** → An entry for the creation, and
      one per save that changed something.
- [ ] **Read the entry for your edit.** → Who, when, and the field with old and new
      value — e.g. "Labour hours: 32 → 40".
- [ ] **Click Save without changing anything, then reload.** → No new entry.
      *An audit trail full of empty saves is one nobody reads.*

## 9 · Not losing work
*§23, §24, §25, §45*

- [ ] **Change a figure, then click "Projects" in the sidebar.** → A confirmation
      asks before leaving. Dismiss it — you stay put, edit intact.
      *§24. A stray sidebar click silently discarding an edited project is the
      failure this prevents.*
- [ ] **With unsaved changes, try to close the tab.** → The browser warns you.
- [ ] **Change a figure and click "Discard changes".** → Asks, then reverts to the
      last saved values.
- [ ] **Watch the buttons while saving and calculating.** → "Saving…" and
      "Calculating…" with a spinner.
      *§23 asks that the user is never left wondering.*
- [ ] **Open "Your account" in the sidebar.** → Who is signed in, the sign-in method,
      and where data is read from.
- [ ] **Narrow the window to laptop, then phone width.** → Columns stack, tables
      scroll sideways rather than the page. Desktop is the priority (§25), but
      nothing should break.

## 10 · The automated suites
*§13, §32, §33* — needs PostgreSQL running; run from the repository root

```powershell
# This machine keeps the toolchain on D:
$env:DOTNET_ROOT           = "D:\dotnet"
$env:PATH                  = "D:\dotnet;D:\dotnet-tools;$env:PATH"
$env:NUGET_PACKAGES        = "D:\nuget\packages"
$env:NUGET_HTTP_CACHE_PATH = "D:\nuget\http-cache"
$env:DOTNET_CLI_HOME       = "D:\dotnet-home"
$env:SPFM_TEST_CONNECTION  = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres"
$env:PLAYWRIGHT_BROWSERS_PATH = "D:\playwright-browsers"

# Start PostgreSQL if it is not already running
D:\pgsql-dl\extracted\pgsql\bin\pg_ctl.exe -D D:\pgdata -l D:\pgdata\server.log -o "-p 5432" start
```

- [ ] **Backend suite** — `dotnet test --filter "Category!=AcceptanceGate"`
      → **97 passing**: calculations, validation, EF model, repository against real
      PostgreSQL, and the API over HTTP.
- [ ] **Acceptance gate** — `dotnet test --filter "Category=AcceptanceGate"`
      → **It FAILS**, saying 0 real Shield projects have been supplied. **That is the
      correct result today.** Do not delete it to go green.
- [ ] **Frontend unit tests** — `cd frontend && npm test` → 13 passing.
- [ ] **End-to-end tests** — `cd frontend && npm run test:e2e` → 10 passing across
      desktop and laptop widths, driving the full Alpha loop in a real browser.

---

## Requirement coverage

All 48 sections. The honest answer to "is every requirement fulfilled" is
**no — and six of them cannot be, by us.**

| § | Requirement | Status | How it was checked |
|---|---|---|---|
| 5 | Microsoft sign-in, tokens, sign-out, session, auth errors | ✅ Done | Sequence 1 · automated |
| 6 | Dashboard: totals, recent, search, open, create | ✅ Done | Sequence 2 |
| 7 | Create, view, search, open, edit, save, recalculate | ✅ Done | Sequences 3–4, 7 |
| 8 | Project creation workflow | ✅ Done | Sequence 7 · E2E |
| 9 | Main project screen, inputs, actions, cancel | ✅ Done | Sequences 4, 7 |
| 10 | Validation with understandable messages | ✅ Done | Sequence 5 · API tests |
| 11 | Calculation engine separated from the UI | ✅ Done | Domain layer has zero dependencies |
| **12** | **The actual SPFM formulas and field list** | ⛔ **Blocked** | Placeholder arithmetic in use |
| 13 | Automated calculation tests | ✅ Done | 23 unit tests, mutation-checked |
| **14** | **Validation against real Shield projects** | ⛔ **Blocked** | Acceptance gate fails by design |
| 15 | Database: users, projects, results, history, QB-ready | ✅ Done | 7 tables, live PostgreSQL |
| 16 | Create, save, reopen, retrieve, edit, recalculate | ✅ Done | Sequence 7 · integration tests |
| 17 | Change history: who, what, old, new, when | ✅ Done | Sequence 8 · API tests |
| 18–19 | QuickBooks readiness (import is Phase 2) | ✅ Done | Nullable mapping columns, customer table |
| 20–21 | API groups, all sensitive endpoints authenticated | ✅ Done | Default-deny policy, verified |
| 22 | Error handling, consistent responses, no leakage | ✅ Done | RFC 9457 problem documents |
| 23 | Loading states | ✅ Done | Sequence 9 |
| 24 | Unsaved-changes warning | ✅ Done | Sequence 9 · E2E |
| 25 | Desktop first, laptop, basic tablet | ✅ Done | Sequence 9 · E2E at two widths |
| 26 | Professional, minimal, data-focused | ✅ Done | One animation in the app |
| 27 | Search, sorting, paging | ✅ Done | Sequence 3 |
| **28** | **Project statuses** | ⛔ **Blocked** | Placeholders — §28 warns against inventing them |
| 29 | Decimal money, deterministic calculations | ✅ Done | `numeric(18,4)`, verified in PostgreSQL |
| 30 | Migrations, reproducible database | ✅ Done | Applied to a real database |
| 31 | Environment config, no committed secrets | ✅ Done | Fail-fast startup validation |
| 32 | Unit, API, frontend and end-to-end testing | ✅ Done | Sequence 10 — 120 tests |
| **33** | **Named real-project test cases** | ⛔ **Blocked** | Harness built, data not supplied |
| 34 | Second full testing pass | 🔶 Partial | After the formulas arrive |
| 35 | Rollback plan | ✅ Done | [ROLLBACK-PLAN.md](ROLLBACK-PLAN.md) — written, never rehearsed |
| 36–37 | Deployment and production verification | 🔶 Partial | Dockerfiles and CI ready; not deployed |
| 38–39 | Documentation and handover | ✅ Done | Design, API, handover, rollback, coverage |
| 40 | 2 months priority support | ➖ Out | Post-delivery |
| 41–42 | Scope boundaries honoured | ✅ Done | No roles, exports, monitoring or QB import |
| 43–44 | Phase 2 and future expansion | ➖ Out | Quoted separately |
| 45 | Screen structure | ✅ Done | Login, dashboard, projects, new, detail, history, profile |
| 46–47 | User journey and priorities | ✅ Done | Sequence 7 |

---

## What is blocked, and on whom

> ### ⛔ The calculations are not Shield's
>
> `SpfmCalculator` contains ordinary job-costing arithmetic used as a stand-in, and
> reports its version as `placeholder-1.0.0`. Every figure the app shows carries an
> "Example figures" warning for exactly this reason. §12 is explicit that the formulas
> must come from the client's handoff document rather than be invented during
> development.

| Needed from Samuel | Blocks | § |
|---|---|---|
| **The SPFM formulas** — every rule, dependency, selection rule, intermediate value | The calculation engine | 11–12 |
| **The field list** — each input, type, required, ranges, dropdown options | Final schema and form | 12 |
| **Rounding rules** — where rounding applies, to how many places | Numeric correctness | 12, 29 |
| **2–3 real projects with known results** | Acceptance. §14 says correctness must not be judged by whether the UI looks right | 14, 33 |
| **Real project statuses** | Status field and filters | 28 |
| **Project numbering format** — and whether QuickBooks owns it | Number generation | 7 |

There is a test that **fails on purpose** until the real projects arrive:
`dotnet test --filter "Category=AcceptanceGate"`. Without it, a green suite would make
the Alpha look validated when the most important check has never run. Add the projects
to `backend/SPFM.Tests/Calculations/real-projects.json` and it passes.

> ### Microsoft sign-in cannot be tested on a deployed site yet
>
> The production URL has to be registered as a redirect URI in Shield's Entra app
> registration, which needs their tenant admin. Token *rejection* is tested against the
> genuine Microsoft handler; acceptance of a valid Shield token is the one path no
> automated test can reach.

---

Counts reflect the suite at the time of writing: 97 backend tests, 13 frontend unit
tests, 10 end-to-end tests. Re-run them rather than trusting this line.
