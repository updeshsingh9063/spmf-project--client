# Microsoft Entra setup

The backend app registration already exists (the API's protected
`/api/users/me` works). The frontend needs its own registration as a
single-page app. No client secret is used or stored anywhere in the frontend.

## 1. Expose a scope on the API registration

Entra admin center → App registrations → *(SPFM API)* → **Expose an API**

- Application ID URI: keep the default `api://<api-client-id>`
- Add a scope: `access_as_user`, who can consent: Admins and users
- The full scope string is `api://<api-client-id>/access_as_user` → this is
  `NEXT_PUBLIC_ENTRA_API_SCOPE`

Skip this step if the API already exposes a scope; use that one.

## 2. Register the frontend

App registrations → **New registration**

- Name: `SPFM Web`
- Supported account types: **Accounts in this organizational directory only**
- Redirect URI: platform **Single-page application (SPA)**,
  `http://localhost:3000` for local development
- After deploying, add the live address too, e.g. `https://spfm.azurewebsites.net`

Then:

- **API permissions** → Add a permission → My APIs → *(SPFM API)* →
  `access_as_user` → **Grant admin consent for Shield**
- Copy **Application (client) ID** → `NEXT_PUBLIC_ENTRA_CLIENT_ID`
- Copy **Directory (tenant) ID** → `NEXT_PUBLIC_ENTRA_TENANT_ID`

## 3. Backend settings to check

- The API validates tokens for audience `api://<api-client-id>` (or the API's
  client ID) and the Shield tenant issuer.
- CORS allows the frontend origins listed in step 2.

## 4. Frontend environment

Create `.env.local` from `.env.example`:

```
NEXT_PUBLIC_AUTH_MODE=entra
NEXT_PUBLIC_ENTRA_CLIENT_ID=<SPFM Web client ID>
NEXT_PUBLIC_ENTRA_TENANT_ID=<Shield tenant ID>
NEXT_PUBLIC_ENTRA_API_SCOPE=api://<api-client-id>/access_as_user
NEXT_PUBLIC_DATA_MODE=api
NEXT_PUBLIC_API_BASE_URL=https://localhost:7001
```

Restart `npm run dev`. Sign in; the sidebar shows **API connected as …** when
the token reaches `/api/users/me`.

## Troubleshooting

| Message | Likely cause |
|---|---|
| "Microsoft sign-in isn't set up yet…" | One of the variables above is empty |
| AADSTS50011 redirect URI mismatch | The address in the browser isn't listed as an SPA redirect URI |
| AADSTS65001 consent required | Admin consent not granted in step 2 |
| "Your sign-in has expired" (401) | API audience or issuer doesn't match the token |
| "Can't reach the SPFM API" | API not running, wrong `NEXT_PUBLIC_API_BASE_URL`, or CORS blocked |
