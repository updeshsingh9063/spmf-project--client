# Deployment (Azure)

Recommended setup, since Shield already uses Microsoft Entra:

| Part | Azure service |
|---|---|
| Frontend (this app) | App Service, Linux, Node 20 LTS |
| Backend (ASP.NET Core API) | App Service, Linux, .NET |
| Database | Azure SQL Database (or the database the backend already uses) |

Samuel creates the Azure subscription and adds the developer as Contributor on
an `spfm` resource group. Hosting is billed to Shield by Microsoft.

## Preview hosting on Netlify

For showing the preview (sample data in the browser), Netlify works with no
extra setup beyond `netlify.toml` in the repository:

- Deploy from Git (Netlify → Add new site → Import from GitHub). Drag-and-drop
  deploys don't work for this app: it needs Netlify's build step.
- Build command `npm run build`, publish directory `.next` (both already set in
  `netlify.toml`, which overrides the Netlify UI).
- Add any `NEXT_PUBLIC_*` settings under Site configuration → Environment
  variables, then redeploy.
- For Entra sign-in on Netlify, add the Netlify address as an SPA redirect URI.

## Frontend on Azure

The `NEXT_PUBLIC_*` values are built into the app, so set them **before**
building (see `.env.example`), with the live addresses.

```bash
npm ci
npm test
npm run build
# package the standalone server
cp -r .next/static .next/standalone/.next/static
cp -r public .next/standalone/public
cd .next/standalone && zip -r ../../spfm-web.zip . && cd ../..
az webapp deploy --resource-group spfm --name spfm-web --src-path spfm-web.zip --type zip
```

App Service settings:

- Startup command: `node server.js`
- Application setting `PORT=8080` (App Service default for Node)
- Add the live address as an SPA redirect URI in Entra (docs/ENTRA-SETUP.md)
- Add the live address to the API's CORS origins

## Backend and database

- Run the database migrations against Azure SQL from a release step, not at
  app start-up.
- Keep connection strings in App Service configuration or Key Vault, never in
  the repository.

## Check after every deployment

Run the acceptance checklist (docs/ACCEPTANCE-CHECKLIST.md) on the live site:
sign in, create, calculate, save, reopen, edit, recalculate, save.

## Rollback plan

| Situation | Action |
|---|---|
| Frontend release is broken | Redeploy the previous `spfm-web.zip` (keep the last 3). With a Standard plan, deploy to a `staging` slot first and **swap**; swap back to roll back in seconds. |
| Backend release is broken | Same as above for the API package. |
| A migration damaged data | Restore Azure SQL to a point in time just before the release (Azure SQL keeps 7 days by default), then redeploy the previous API. |
| Entra sign-in stops working | Check the redirect URI and admin consent first; no code rollback is usually needed. |

Before each release: note the release time, keep the previous packages, and
confirm the database backup window covers it.
