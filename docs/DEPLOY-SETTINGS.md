# Deploy settings

Both hosts build from this repository. Nothing secret is committed; every value
below is either a public identifier or supplied by the host.

## Vercel — frontend

| Setting | Value |
|---|---|
| Root directory | `frontend` |
| Framework | Next.js (detected) |

### Preview deployment — the link to send the client

Works with no backend, no database and no Microsoft account.

```
NEXT_PUBLIC_AUTH_MODE=preview
NEXT_PUBLIC_DATA_MODE=local
```

### Live deployment — once Entra is configured

```
NEXT_PUBLIC_AUTH_MODE=entra
NEXT_PUBLIC_DATA_MODE=api
NEXT_PUBLIC_API_BASE_URL=https://spfm-api.onrender.com
NEXT_PUBLIC_ENTRA_CLIENT_ID=e0c6710b-273e-45eb-8706-1a0b3b2025d8
NEXT_PUBLIC_ENTRA_TENANT_ID=83dba18f-9e69-44c9-9af6-ea69f74eb71d
NEXT_PUBLIC_ENTRA_API_SCOPE=api://72556310-ab1a-4d95-8375-9f9fe3c0396d/access_as_user
NEXT_PUBLIC_ENTRA_REDIRECT_URI=https://<the-vercel-url>
```

`NEXT_PUBLIC_*` values are inlined at build time, so changing one needs a redeploy.
They are public: client ids, a tenant id and a scope. A single-page app
registration has no secret.

## Render — API and database

Use `render.yaml` as a Blueprint. One value must be set by hand after the
frontend exists:

| Variable | Value |
|---|---|
| `Cors__AllowedOrigins__0` | the Vercel URL, no trailing slash |

Then run the migrations once against the Render database:

```bash
SPFM_MIGRATION_CONNECTION="<external connection string from Render>" \
  dotnet ef database update \
    --project backend/SPFM.Infrastructure \
    --startup-project backend/SPFM.Infrastructure
```

Migrations are deliberately not run at startup: concurrent instances would race.

## Microsoft Entra — needed before sign-in works anywhere but preview

In the **SPA** app registration (`e0c6710b…`), add the Vercel URL as a redirect URI
under the Single-page application platform. Needs Shield tenant admin.

Until that is done, `NEXT_PUBLIC_AUTH_MODE=entra` will fail at the login screen
with a redirect-URI mismatch. Preview mode is unaffected.

## Free tier caveats

- **Render free web services sleep after 15 minutes.** The next request takes
  roughly 50 seconds. Poor first impression for a client; $7/month removes it.
- **Render free PostgreSQL is deleted after 30 days.** Move to a paid plan before
  any real Shield data goes in.
- **Vercel's Hobby plan is for non-commercial use.** This is paid client work, so
  check whether a Pro plan is required.
