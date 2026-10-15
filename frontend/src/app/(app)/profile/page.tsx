"use client";

// User / profile screen (requirements s45).
//
// Shows who is signed in and how this installation is configured. Deliberately
// read-only: profile details come from Microsoft Entra, and editing them here
// would create a second, divergent copy of the truth.

import { useEffect, useState } from "react";
import { Icon, Spinner } from "@/components/ui";
import { getMe, type Me } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { config } from "@/lib/config";

function Row({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <div className="grid gap-1 px-5 py-4 sm:grid-cols-[14rem_1fr] sm:gap-4">
      <dt className="text-sm text-muted">{label}</dt>
      <dd>
        <p className="text-sm">{value}</p>
        {hint && <p className="mt-0.5 text-xs text-muted">{hint}</p>}
      </dd>
    </div>
  );
}

export default function ProfilePage() {
  const { user, signOut, mode } = useAuth();
  const [me, setMe] = useState<Me | null>(null);
  const [checking, setChecking] = useState(config.dataMode === "api");
  const [apiError, setApiError] = useState<string | null>(null);

  // Calling the protected endpoint proves the whole chain works: a token was
  // acquired, attached, and accepted by the API.
  useEffect(() => {
    if (config.dataMode !== "api") return;
    let cancelled = false;

    getMe()
      .then((data) => !cancelled && setMe(data))
      .catch((e: Error) => !cancelled && setApiError(e.message))
      .finally(() => !cancelled && setChecking(false));

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div className="mx-auto max-w-3xl">
      <header>
        <h1 className="display text-[1.75rem] leading-tight">Your account</h1>
        <p className="mt-1 text-sm text-ink-2">
          Signed in with your Microsoft work account. Name and email come from Microsoft and are changed there.
        </p>
      </header>

      <section className="panel mt-6 divide-y divide-line-soft" aria-label="Account details">
        <Row label="Name" value={me?.name ?? user?.name ?? "Not signed in"} />
        <Row label="Email" value={me?.email ?? user?.email ?? "–"} />
        <Row
          label="Sign-in method"
          value={mode === "entra" ? "Microsoft Entra (work account)" : "Preview (simulated sign-in)"}
          hint={mode === "preview" ? "No Microsoft account is involved in preview mode." : undefined}
        />
      </section>

      <section className="panel mt-6 divide-y divide-line-soft" aria-label="Connection">
        <div className="px-5 py-4">
          <h2 className="text-[0.95rem] font-semibold">Connection</h2>
          <p className="mt-0.5 text-xs text-muted">Where this screen is reading and writing project data.</p>
        </div>
        <Row
          label="Data source"
          value={config.dataMode === "api" ? "SPFM API" : "This browser (preview data)"}
          hint={
            config.dataMode === "api"
              ? config.apiBaseUrl || undefined
              : "Projects are stored in this browser only and are not shared between devices."
          }
        />
        {config.dataMode === "api" && (
          <Row
            label="API check"
            value={
              checking ? "Checking…" : apiError ? "Could not reach the API" : me ? "Connected and authorised" : "–"
            }
            hint={apiError ?? (me ? "The API accepted this account's access token." : undefined)}
          />
        )}
      </section>

      <section className="panel mt-6" aria-label="Session">
        <div className="flex flex-wrap items-center justify-between gap-3 px-5 py-4">
          <div>
            <h2 className="text-[0.95rem] font-semibold">Sign out</h2>
            <p className="mt-0.5 text-xs text-muted">Ends this session on this device.</p>
          </div>
          <button onClick={signOut} className="btn btn-quiet">
            <Icon name="out" /> Sign out
          </button>
        </div>
      </section>

      {checking && (
        <p className="mt-4 flex items-center gap-2 text-xs text-muted">
          <Spinner /> Confirming the API accepts your sign-in…
        </p>
      )}
    </div>
  );
}
