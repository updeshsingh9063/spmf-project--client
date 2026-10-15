"use client";

// Session handling. Two modes, chosen by NEXT_PUBLIC_AUTH_MODE:
//  - "preview" (default): simulated sign-in for reviewing the screens.
//  - "entra": Microsoft Entra sign-in with MSAL (redirect flow). The access
//    token for the SPFM API scope is attached to every API request.
// No secrets live in the frontend: a single-page app registration has none.

import {
  createStandardPublicClientApplication,
  InteractionRequiredAuthError,
  type AccountInfo,
  type IPublicClientApplication,
} from "@azure/msal-browser";
import { createContext, useCallback, useContext, useEffect, useRef, useState } from "react";
import { setTokenProvider } from "./api";
import { config, missingConfig } from "./config";

export interface User {
  name: string;
  email: string;
}

interface AuthState {
  user: User | null;
  ready: boolean;
  error: string | null;
  mode: "preview" | "entra";
  signIn: () => void;
  signOut: () => void;
}

const PREVIEW_KEY = "spfm.session.v1";
const PREVIEW_USER: User = { name: "Samuel Fish", email: "samuel@shieldsecuritydoors.example" };

const AuthContext = createContext<AuthState | null>(null);

const toUser = (a: AccountInfo): User => ({ name: a.name || a.username, email: a.username });

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [ready, setReady] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const msal = useRef<IPublicClientApplication | null>(null);
  const mode = config.authMode;

  useEffect(() => {
    if (mode === "preview") {
      try {
        const raw = window.localStorage.getItem(PREVIEW_KEY);
        if (raw) setUser(JSON.parse(raw) as User);
      } catch {
        /* no stored session */
      }
      setTokenProvider(async () => null);
      setReady(true);
      return;
    }

    const missing = missingConfig();
    if (missing.length) {
      setError(`Microsoft sign-in isn't set up yet. Add ${missing.join(", ")} to the environment settings.`);
      setReady(true);
      return;
    }

    let cancelled = false;
    (async () => {
      try {
        const app = await createStandardPublicClientApplication({
          auth: {
            clientId: config.entra.clientId,
            authority: `https://login.microsoftonline.com/${config.entra.tenantId}`,
            redirectUri: config.entra.redirectUri || window.location.origin,
            postLogoutRedirectUri: window.location.origin,
          },
          cache: { cacheLocation: "sessionStorage" },
        });
        const result = await app.handleRedirectPromise();
        const account = result?.account ?? app.getActiveAccount() ?? app.getAllAccounts()[0] ?? null;
        if (account) app.setActiveAccount(account);
        msal.current = app;

        setTokenProvider(async () => {
          const active = app.getActiveAccount();
          if (!active) return null;
          try {
            const t = await app.acquireTokenSilent({ scopes: [config.entra.apiScope], account: active });
            return t.accessToken;
          } catch (e) {
            if (e instanceof InteractionRequiredAuthError) {
              await app.acquireTokenRedirect({ scopes: [config.entra.apiScope], account: active });
            }
            throw e;
          }
        });

        if (!cancelled) {
          setUser(account ? toUser(account) : null);
          setReady(true);
        }
      } catch (e) {
        if (!cancelled) {
          setError(`Microsoft sign-in failed: ${e instanceof Error ? e.message : String(e)}`);
          setReady(true);
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [mode]);

  const signIn = useCallback(() => {
    if (mode === "preview") {
      setUser(PREVIEW_USER);
      try {
        window.localStorage.setItem(PREVIEW_KEY, JSON.stringify(PREVIEW_USER));
      } catch {}
      return;
    }
    msal.current?.loginRedirect({ scopes: [config.entra.apiScope], prompt: "select_account" }).catch((e) =>
      setError(`Microsoft sign-in failed: ${e instanceof Error ? e.message : String(e)}`),
    );
  }, [mode]);

  const signOut = useCallback(() => {
    setUser(null);
    if (mode === "preview") {
      try {
        window.localStorage.removeItem(PREVIEW_KEY);
      } catch {}
      return;
    }
    const app = msal.current;
    app?.logoutRedirect({ account: app.getActiveAccount() ?? undefined });
  }, [mode]);

  return (
    <AuthContext.Provider value={{ user, ready, error, mode, signIn, signOut }}>{children}</AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside AuthProvider");
  return ctx;
}
