# Requirements coverage

Section-by-section status against `SPFM_Requirements.pdf` (48 sections).

**Legend** — ✅ done · 🔶 partial · ⬜ not started · ⛔ blocked on the client · ➖ out of scope

Last reviewed: 15 October 2026

---

## Frontend

| § | Requirement | Status | Where |
|---|---|---|---|
| 5 | Microsoft sign-in, MSAL, token on every request | ✅ | `lib/auth.tsx`, `lib/api.ts` |
| 5 | Sign out, session persistence | ✅ | `AppShell`, `profile` |
| 5 | Authentication error handling | ✅ | `login/page.tsx` — named cause plus what to try |
| 5 | Unauthorized access handling | ✅ | `AppShell` redirects to `/login`; 401 → "sign-in has expired" |
| 5 | No secrets in frontend | ✅ | SPA registration has none; only public client/tenant ids |
| 6 | Dashboard: total projects | ✅ | Overview tile |
| 6 | Recently updated, search, open, create | ✅ | Overview + Projects |
| 7 | Create / view / search / open / edit / save / recalculate | ✅ | Projects, ProjectEditor |
| 7 | Search by name, number, customer | ✅ | Server-side in API mode, debounced |
| 8 | Full project creation workflow | ✅ | Covered by the E2E spec |
| 9 | Main project screen, inputs, calculated fields | ✅ | `ProjectEditor` |
| 9 | Actions: Save, Calculate, Recalculate | ✅ | — |
| 9 | Cancel, Back to Projects | ✅ | "Discard changes" / "Cancel"; back link |
| 10 | Validation with understandable messages | ✅ | Field-level, plain English, never a stack trace |
| 10 | Server validation surfaced on the right field | ✅ | `ValidationError` → `toFormField` |
| 17 | Change history displayed | ✅ | Paged, loaded separately from the project |
| 22 | Friendly frontend errors | ✅ | `describeError`; ProblemDetails `detail` preferred |
| 22 | Concurrent-edit conflict | ✅ | 409 → explains their changes are safe, offers reload |
| 23 | Loading states | ✅ | Saving… / Calculating… / Loading projects… / Loading history… / Signing in… |
| 24 | Unsaved-changes warning | ✅ | `useUnsavedChanges` — covers tab close **and** in-app navigation |
| 25 | Desktop first, laptop, basic tablet | ✅ | Grids collapse; tables scroll horizontally |
| 25 | E2E run at desktop and laptop widths | ✅ | Two Playwright projects (1440 and 1280) |
| 26 | Professional, minimal, data-focused | ✅ | One animation in the app (the busy indicator) |
| 27 | Search, sorting, recent, alphabetical, number, customer | ✅ | Sort control with five orders |
| 27 | Paging for large result sets | ✅ | Cursor-based "Load more" |
| 28 | Project status | 🔶 | Quote/Active/Complete/Archived — **placeholder**, §28 warns against inventing these |
| 45 | Microsoft login screen | ✅ | `/login` |
| 45 | Authentication callback / error | ✅ | Handled in `/login`: "Completing sign-in…" and a guided error |
| 45 | Dashboard, Projects, New, Details/Edit | ✅ | `/`, `/projects`, `/projects/new`, `/projects/[id]` |
| 45 | Calculation results | ✅ | Results panel in the editor |
| 45 | Change history | ✅ | History panel in the editor |
| 45 | User / profile | ✅ | `/profile` |

## Backend

| § | Requirement | Status | Where |
|---|---|---|---|
| 3 | Layered architecture | ✅ | Four projects, one-directional |
| 11 | Calculation engine separated from UI | ✅ | `SPFM.Domain` — zero dependencies |
| 15 | Users, Projects, SPFM data, results, history | ✅ | 7 entities |
| 15 | QuickBooks mapping ready | ✅ | Nullable QBO columns, `Customer` as its own table |
| 16 | Create, save, reopen, retrieve, edit, recalculate, update | 🔶 | Implemented; **never run against a live database** |
| 17 | Change history: user, field, old, new, timestamp | ✅ | `ChangeSet` + `FieldChange`, real diffs only |
| 20 | API groups: auth, users, projects, calculations, history | ✅ | 8 endpoints under `/api/v1` |
| 21 | All sensitive APIs authenticated | ✅ | Default-deny fallback policy — verified |
| 22 | Consistent responses, no sensitive leakage | ✅ | RFC 9457 ProblemDetails with `traceId` |
| 29 | Decimal money, deterministic calculations | ✅ | `decimal` / `numeric(18,4)`; browser engine is preview-only |
| 30 | Migrations, reproducible database | 🔶 | `InitialCreate` written; **never applied** |
| 31 | Environment configuration, no committed secrets | ✅ | Fail-fast startup validation |

## Blocked on the client

| § | Requirement | Status |
|---|---|---|
| 11–12 | **The real SPFM formulas** | ⛔ `EngineVersion = "placeholder-1.0.0"` |
| 12 | **The SPFM field list** | ⛔ Current fields are a working stand-in |
| 12 | **Rounding and precision rules** | ⛔ Commercial rounding assumed |
| 14, 33 | **2–3 real Shield projects with known results** | ⛔ Not supplied |
| 28 | Real project statuses | ⛔ Placeholders |
| 7 | Project numbering format | ⛔ `SSD-{year}-{nnn}` assumed |

## Not yet started

| § | Requirement | Status |
|---|---|---|
| 32 | Integration tests against a real database | ⬜ Needs Docker (blocked: C: drive full) |
| 34 | Second full testing pass | ⬜ After the above |
| 35 | Rollback plan | ⬜ Designed, not written up |
| 36–37 | Deployment and production verification | ⬜ Needs Azure access |
| 38–39 | Full handover documentation | 🔶 README and design done |
| 40 | 2 months priority support | ➖ Post-delivery |

## Explicitly out of scope (§42)

QuickBooks import · user roles · permissions · data exports · advanced error
monitoring · payments · customer-facing site · mobile app.

---

## The honest summary

Everything that can be built without the client's specification is built. What
remains splits in two:

1. **Six items only Samuel can supply** — above all the formulas and the real
   project examples. Without them the calculations are unverifiable, and §14 is
   explicit that correctness must not be judged by whether the UI looks right.
2. **Three verification gaps needing a working environment** — the database has
   never been created, API mode has never been exercised end to end, and the E2E
   suite cannot run without a browser download. All three are blocked on free disk
   space on the development machine.
