# Alpha acceptance checklist

From the agreed Alpha scope. Status refers to this frontend; items that
depend on the backend, Entra or hosting are marked.

| # | Check | How to verify | Frontend status |
|---|---|---|---|
| 1 | A user can sign in successfully | Sign in with a Shield Microsoft account | Built (MSAL, `NEXT_PUBLIC_AUTH_MODE=entra`). Needs Shield's Entra IDs to test live. |
| 2 | The frontend can call the protected backend API | Sidebar shows "API connected as …" (calls `/api/users/me` with the token) | Built. Needs the API URL and CORS. |
| 3 | A new project can be created from the screen | Projects → New project → Create project | Done |
| 4 | Required inputs can be entered and validated | Leave fields empty, select Calculate: fields are marked with messages | Done (server must repeat the checks, docs/API-CONTRACT.md) |
| 5 | Core SPFM calculations give the expected results for the agreed test projects | `npm test` runs every case in `src/lib/testCases.ts` | Engine and tests done. **Needs Samuel's rules and 2–3 real projects.** |
| 6 | Projects can be saved, listed, searched and reopened with data intact | `npm run test:e2e` covers this | Done |
| 7 | Inputs can be changed, recalculated and saved again | Edit → "Needs recalculating" → Recalculate → Save | Done (e2e) |
| 8 | Everything works on the deployed internal environment without developer help | Run checks 1–7 on the live site | **Needs hosting** (docs/DEPLOYMENT.md) |

## Growth plan extras

| Item | Status |
|---|---|
| Change history showing who edited what, and when | Done (project screen, "Change history") |
| Data model shaped to match QuickBooks | Done (docs/QUICKBOOKS-MAPPING.md) |
| Named test cases with expected results handed over | Structure done (`src/lib/testCases.ts`); real cases pending |
| Second full testing pass | e2e suite runs at two screen sizes; repeat on the live site before handover |
| Rollback plan | docs/DEPLOYMENT.md |
