"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { useAuth } from "@/lib/auth";

function MicrosoftMark() {
  return (
    <svg width="18" height="18" viewBox="0 0 21 21" aria-hidden="true">
      <rect x="1" y="1" width="9" height="9" fill="#f25022" />
      <rect x="11" y="1" width="9" height="9" fill="#7fba00" />
      <rect x="1" y="11" width="9" height="9" fill="#00a4ef" />
      <rect x="11" y="11" width="9" height="9" fill="#ffb900" />
    </svg>
  );
}

export default function LoginPage() {
  const { user, ready, error, mode, signIn } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (ready && user) router.replace("/");
  }, [ready, user, router]);

  return (
    <div className="grid min-h-screen md:grid-cols-[1.1fr_1fr]">
      <section className="relative hidden overflow-hidden bg-steel text-white md:flex md:flex-col md:justify-between md:p-12">
        {/* door-panel motif: the rails and stiles of a security door */}
        <svg className="absolute inset-y-10 right-10 h-[calc(100%-5rem)] w-[40%]" viewBox="0 0 300 600" preserveAspectRatio="xMaxYMid meet" aria-hidden="true">
          <rect x="30" y="20" width="240" height="560" rx="6" fill="none" stroke="rgb(255 255 255 / 0.09)" strokeWidth="10" />
          {Array.from({ length: 13 }).map((_, i) => (
            <line key={i} x1={60 + i * 15} y1="50" x2={60 + i * 15} y2="550" stroke="rgb(255 255 255 / 0.09)" strokeWidth="3" />
          ))}
          <rect x="30" y="285" width="240" height="30" fill="rgb(255 255 255 / 0.09)" />
          <circle cx="240" cy="300" r="9" style={{ fill: "var(--color-brass)" }} />
        </svg>
        <div className="relative">
          <p className="display text-2xl">SPFM</p>
          <p className="mt-1 text-sm text-white/60">Shield Security Doors</p>
        </div>
        <div className="relative max-w-md">
          <h1 className="display text-4xl leading-[1.1]">Every job, costed and checked before it leaves the office.</h1>
          <p className="mt-4 text-base leading-relaxed text-white/70">
            Enter a project, run the SPFM calculations, and keep a clean record for your accountant.
          </p>
        </div>
      </section>

      <section className="flex flex-col justify-center px-6 py-12 sm:px-16">
        <div className="mx-auto w-full max-w-sm">
          <p className="display text-xl md:hidden">SPFM</p>
          <h2 className="display mt-6 text-2xl md:mt-0">Sign in</h2>
          <p className="mt-2 text-sm text-ink-2">Use your Shield Microsoft work account.</p>

          <button
            onClick={signIn}
            disabled={!ready || (mode === "entra" && !!error)}
            className="btn btn-quiet mt-8 h-11 w-full gap-3 text-[0.95rem]"
          >
            <MicrosoftMark />
            Sign in with Microsoft
          </button>

          {error && (
            <p role="alert" className="mt-6 rounded-md border border-bad/30 bg-bad-wash p-3 text-xs leading-relaxed text-bad">
              {error}
            </p>
          )}
          {mode === "preview" && (
            <p className="mt-6 rounded-md border border-line bg-panel p-3 text-xs leading-relaxed text-muted">
              Preview mode: sign-in is simulated. Set NEXT_PUBLIC_AUTH_MODE=entra to use Microsoft Entra.
            </p>
          )}
        </div>
      </section>
    </div>
  );
}
