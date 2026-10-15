# SPFM API contract

What the frontend expects from the ASP.NET Core backend when
`NEXT_PUBLIC_DATA_MODE=api`. `mock-api/server.mjs` implements this contract
for frontend development (`npm run mock-api`).

## Rules for every endpoint

- Every request carries `Authorization: Bearer <access token>` for the scope in
  `NEXT_PUBLIC_ENTRA_API_SCOPE`. Reject missing or invalid tokens with **401**.
- Only signed-in Shield users may use the API. Return **403** for anyone else.
- JSON in and out, camelCase property names (the ASP.NET Core default).
- Errors return a JSON body with a `title` (ProblemDetails is fine). The
  frontend shows `title` to the user, so write it as a plain sentence.
- CORS: allow the frontend origin(s), the `Authorization` and `Content-Type`
  headers, and `GET, POST, PUT`.

## Endpoints

| Method | Path | Returns | Notes |
|---|---|---|---|
| GET | `/api/users/me` | `{ name, email }` | Existing endpoint. Used to confirm sign-in reaches the API. |
| GET | `/api/projects` | `Project[]` | All projects. Sorting is done on the frontend. |
| GET | `/api/projects/{id}` | `Project` | 404 if it doesn't exist. |
| POST | `/api/projects` | `Project` (201) | Body: `ProjectDraft`. Server assigns `id`, `number`, timestamps, first history entry. |
| PUT | `/api/projects/{id}` | `Project` | Body: `ProjectDraft`. Server records a history entry if anything changed. |

## Shapes

TypeScript definitions are in `src/lib/types.ts` (the source of truth) and
`ProjectDraft` in `src/lib/projectStore.ts`.

```jsonc
// ProjectDraft (request body for POST and PUT)
{
  "name": "Warehouse fit-out, Unit 4",
  "client": "Harbour Logistics",
  "siteAddress": "4 Dockside Road",
  "status": "active",                 // "quote" | "active" | "complete"
  "inputs": {
    "products": [
      { "id": "uuid", "description": "Steel security door, single", "qty": 4,
        "unitCost": 820, "unitPrice": 1350, "qbItemId": null }
    ],
    "variations": 500, "labourHours": 32, "labourRate": 55,
    "subcontractorCost": 900, "otherDirectCost": 240, "overheadPct": 12
  },
  "results": { /* SpfmResults, or null if not calculated */ },
  "calculatedFor": "fingerprint string or null",
  "calculatedAt": "2026-10-13T09:30:00Z"
}

// Project (response) = ProjectDraft plus:
{
  "id": "uuid",
  "number": "SSD-2026-014",           // server-generated, unique
  "quickbooks": { "customerId": null, "projectId": null, "lastSyncedAt": null },
  "history": [
    { "id": "uuid", "at": "ISO date", "by": "Samuel Fish", "action": "updated",
      "changes": [ { "field": "Labour hours", "from": "12", "to": "16" } ] }
  ],                                   // newest first
  "createdAt": "ISO date",
  "updatedAt": "ISO date"
}
```

## Server-side validation

Repeat the checks in `validate()` (`src/lib/spfm.ts`) and return **400** with a
readable `title` when they fail: name and client required, at least one
product line, whole-number quantity of 1 or more, money values 0 or more,
overhead between 0 and 100.

## Calculations

For the Alpha the frontend runs the calculations and sends `results` with the
draft. Recommended: the API also recomputes `results` with the same rules
(`src/lib/spfm.ts` and the cases in `src/lib/testCases.ts`) and stores its own
answer, so saved figures never depend on the browser.

## History

`by` comes from the token (the `name` claim), never from the request body.
Field labels can follow `src/lib/history.ts` so entries read the same in both modes.
