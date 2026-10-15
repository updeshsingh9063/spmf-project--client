// Runtime configuration from environment variables (see .env.example).
// NEXT_PUBLIC_* values are public by design: client IDs and scopes only,
// never secrets. Each variable must be referenced literally so Next.js can
// inline it at build time.

export const config = {
  /** "preview" = simulated sign-in, "entra" = Microsoft Entra via MSAL */
  authMode: process.env.NEXT_PUBLIC_AUTH_MODE === "entra" ? "entra" : "preview",
  /** "local" = projects saved in this browser, "api" = ASP.NET Core Project API */
  dataMode: process.env.NEXT_PUBLIC_DATA_MODE === "api" ? "api" : "local",
  apiBaseUrl: (process.env.NEXT_PUBLIC_API_BASE_URL ?? "").replace(/\/+$/, ""),
  entra: {
    clientId: process.env.NEXT_PUBLIC_ENTRA_CLIENT_ID ?? "",
    tenantId: process.env.NEXT_PUBLIC_ENTRA_TENANT_ID ?? "",
    apiScope: process.env.NEXT_PUBLIC_ENTRA_API_SCOPE ?? "",
    redirectUri: process.env.NEXT_PUBLIC_ENTRA_REDIRECT_URI ?? "",
  },
} as const;

/** Returns a readable list of missing settings for the chosen modes. */
export function missingConfig(): string[] {
  const m: string[] = [];
  if (config.authMode === "entra") {
    if (!config.entra.clientId) m.push("NEXT_PUBLIC_ENTRA_CLIENT_ID");
    if (!config.entra.tenantId) m.push("NEXT_PUBLIC_ENTRA_TENANT_ID");
    if (!config.entra.apiScope) m.push("NEXT_PUBLIC_ENTRA_API_SCOPE");
  }
  if (config.dataMode === "api" && !config.apiBaseUrl) m.push("NEXT_PUBLIC_API_BASE_URL");
  return m;
}
