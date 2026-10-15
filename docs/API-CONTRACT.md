# API contract

The SPFM API as built. Verified by the tests in `SPFM.Tests/Integration/ApiTests.cs`.

Base path `/api/v1`. Generated reference at `/swagger` when running in Development.

---

## Authentication

Every endpoint except `/health` and `/health/ready` requires a Microsoft Entra
access token:

```
Authorization: Bearer <access token>
```

The token must carry the `access_as_user` scope. It is enforced by the
authorization policy, not by an attribute, so a new endpoint is protected the
moment it exists — nothing has to be remembered.

| Situation | Response |
|---|---|
| No token | `401` with `WWW-Authenticate: Bearer` |
| Invalid or expired token | `401` with `Bearer error="invalid_token"` |
| Valid token, missing scope | `403` |
| Route that does not exist, unauthenticated | `401` — the API does not reveal which routes exist |

---

## Errors

Every error is an RFC 9457 problem document with content type
`application/problem+json`:

```json
{
  "title": "Project changed elsewhere",
  "detail": "This project was changed by someone else while you were editing it.",
  "status": 409,
  "instance": "/api/v1/projects/8f14e45f-ceea-467a-9575-1e0a5e39f5a1",
  "traceId": "0HN7G2M8K1QJ9:00000003"
}
```

`detail` is written for a person and can be shown as-is. `traceId` matches the
server log entry.

Validation failures add `errors`, keyed by field so the UI can attach each message
to its input:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "name": ["Enter a project name."],
    "lines.1.quantity": ["Enter a quantity of 1 or more."]
  }
}
```

---

## Endpoints

### `GET /health` · `GET /health/ready`

Anonymous. `/health` is liveness — the process is up. `/health/ready` is readiness
and includes the database, returning `503` when it is unreachable. Orchestrators
should restart on liveness and withhold traffic on readiness; a database blip must
not kill a healthy process.

### `GET /api/v1/users/me`

Returns the signed-in user, creating the record on first sign-in.

```json
{ "id": "…", "name": "Samuel Fish", "email": "samuel@shieldsecuritydoors.com" }
```

Name and email only. The full claim set is deliberately not exposed (§22).

### `GET /api/v1/projects`

| Query | Meaning |
|---|---|
| `q` | Substring match on project name, number or customer, case-insensitive |
| `status` | `quote` · `active` · `complete` · `archived` |
| `cursor` | Opaque; from the previous page's `nextCursor` |
| `limit` | 1–100, default 25 |

```json
{
  "items": [
    {
      "id": "…", "number": "SSD-2026-014", "name": "Warehouse fit-out",
      "customerName": "Bayview Logistics", "status": "quote",
      "revenue": 12300.0000, "netProfit": 674.4000,
      "calculationState": "current", "updatedAt": "2026-10-15T14:32:11.1234567+00:00"
    }
  ],
  "nextCursor": "MTcyOTAwMDAwMDA6OGYxNGU0NWY…"
}
```

`nextCursor` is `null` on the last page. Cursors are opaque — do not parse or
construct them. An unrecognised cursor restarts the list rather than failing, since
stale bookmarks are ordinary.

Paging is keyset, not offset, so page 20 costs what page 1 costs.

### `GET /api/v1/projects/{id}`

The full project. Responds with an `ETag` header, which the client must send back
as `If-Match` when saving.

```json
{
  "id": "…", "number": "SSD-2026-014", "name": "Warehouse fit-out",
  "customerId": "…", "customerName": "Bayview Logistics",
  "siteAddress": "12 Dock Road", "status": "quote",
  "lines": [
    { "description": "Steel security door", "quantity": 4.0000,
      "unitCost": 820.0000, "unitPrice": 1350.0000 }
  ],
  "variations": 0.0000, "labourHours": 32.0000, "labourRate": 55.0000,
  "subcontractorCost": 0.0000, "otherDirectCost": 0.0000, "overheadPercent": 12.0000,
  "results": { "revenue": 5400.00, "netProfit": 1428.80, "…": "…" },
  "calculationState": "current",
  "calculatedAt": "2026-10-15T14:32:11+00:00",
  "engineVersion": "placeholder-1.0.0",
  "createdAt": "…", "updatedAt": "…",
  "etag": "\"817\""
}
```

**`calculationState`** — `never`, `outdated` or `current`. The server decides it by
comparing the stored results' input fingerprint with the project's current inputs.
The client must not compute it: browser arithmetic is floating point and would
disagree. `outdated` means the figures shown no longer match the inputs (§16).

**`engineVersion`** — which formula set produced the stored results. While it starts
`placeholder`, the figures came from stand-in formulas, not Shield's SPFM rules.

**`results`** is `null` until the project has been calculated.

### `POST /api/v1/projects` → `201`

```json
{
  "number": null,
  "name": "Warehouse fit-out",
  "customerId": null,
  "customerName": "Bayview Logistics",
  "siteAddress": "12 Dock Road",
  "status": "quote",
  "lines": [{ "description": "Steel security door", "quantity": 4, "unitCost": 820, "unitPrice": 1350 }],
  "variations": 0, "labourHours": 32, "labourRate": 55,
  "subcontractorCost": 0, "otherDirectCost": 0, "overheadPercent": 12
}
```

`number` omitted is generated. `customerId` omitted creates or reuses a customer by
name, matched case-insensitively.

`409` if the project number is already taken.

### `PUT /api/v1/projects/{id}`

Same body. **Requires `If-Match`** with the ETag from the last read.

| | |
|---|---|
| `200` | Saved; the response carries a new ETag |
| `428` | No `If-Match`. The caller cannot prove which version it is editing |
| `409` | Someone else saved first. Nothing was written, and their change is intact |

Sending `If-Match: *` opts out of the check deliberately. The frontend never does.

### `POST /api/v1/projects/{id}/calculate`

Runs the SPFM calculations against the project's **saved** inputs and appends a
calculation run.

```json
{ "results": { "…": "…" }, "calculationState": "current", "engineVersion": "placeholder-1.0.0" }
```

Takes no body: the figures of record must correspond to a project someone can
reopen. Unsaved edits must be saved first.

Runs are append-only — calculating again adds a row rather than replacing one, so
what the figures said on a given date stays answerable.

`400` with field-keyed `errors` when the saved inputs are not valid.

### `GET /api/v1/projects/{id}/history`

Change history, newest first, paged with `cursor` and `limit`.

```json
{
  "items": [
    {
      "id": "…", "by": "Updesh", "action": "updated",
      "at": "2026-10-15T14:32:11+00:00",
      "changes": [{ "field": "Labour hours", "from": "32", "to": "40" }]
    }
  ],
  "nextCursor": null
}
```

`action` is `created`, `updated` or `calculated`. Field labels are the words on the
form, not property names. A save that changed nothing records no entry.

---

## Conventions

**Money** is `decimal` end to end — `numeric(18,4)` in PostgreSQL, `decimal` in C#.
It is never a floating-point type anywhere in the system (§29). All arithmetic
happens server-side; the browser formats and displays.

**Percentages** are `null`, not `0`, when the denominator is zero. A project with no
revenue has no margin, which is not the same as a zero margin.

**Enums** travel as words (`"quote"`), never numbers.

**Timestamps** are ISO 8601 with offset, always UTC.

---

## Reserved for Phase 2

`/api/v1/quickbooks/*` — connect, sync, import, mapping, status. Not implemented.
The data model carries nullable `qboCustomerId`, `qboProjectId` and `qboItemId` so
the import can populate them without a rebuild (§19).
