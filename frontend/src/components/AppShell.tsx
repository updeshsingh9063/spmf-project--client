"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { getMe } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { config } from "@/lib/config";
import { resetSampleData } from "@/lib/projectStore";
import { Icon, Spinner } from "./ui";

// Ticket 1: primary navigation is Work Queue, Projects and Financial Review.
// Financial Review stays disabled until Ticket 19 builds it -- showing it greyed
// out tells the user the workspace is coming, rather than hiding it entirely.
const NAV = [
  { href: "/", label: "Work Queue", icon: "queue" as const },
  { href: "/projects", label: "Projects", icon: "folder" as const },
  { href: "/financial-review", label: "Financial Review", icon: "period" as const, disabled: true },
  { href: "/profile", label: "Your account", icon: "user" as const },
];

export function AppShell({ children }: { children: React.ReactNode }) {
  const { user, ready, signOut } = useAuth();
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    if (ready && !user) router.replace("/login");
  }, [ready, user, router]);

  // In API mode, confirm the token reaches the protected /api/users/me endpoint.
  const [api, setApi] = useState<{ ok: boolean; text: string } | null>(null);
  useEffect(() => {
    if (!user || config.dataMode !== "api") return;
    getMe()
      .then((me) => setApi({ ok: true, text: `API connected as ${me.name}` }))
      .catch((e: Error) => setApi({ ok: false, text: e.message }));
  }, [user]);

  if (!ready || !user) {
    return (
      <div className="grid min-h-screen place-items-center">
        <p className="flex items-center gap-2 text-sm text-muted">
          <Spinner /> {ready ? "Signing in…" : "Loading SPFM…"}
        </p>
      </div>
    );
  }

  const isActive = (href: string) => (href === "/" ? pathname === "/" : pathname.startsWith(href));

  return (
    <div className="min-h-screen md:grid md:grid-cols-[232px_1fr]">
      <aside className="flex flex-col bg-steel text-white md:sticky md:top-0 md:h-screen">
        <div className="flex items-center justify-between gap-3 px-5 py-4 md:block md:py-6">
          <Link href="/" className="block">
            <span className="display block text-xl leading-none">SPFM</span>
            <span className="mt-1.5 block text-xs text-white/60">Shield Security Doors</span>
          </Link>
          <button onClick={signOut} className="text-xs text-white/70 hover:text-white md:hidden">
            Sign out
          </button>
        </div>

        <nav className="flex gap-1 px-3 pb-3 md:flex-col md:pb-0" aria-label="Main">
          {NAV.map((item) =>
            item.disabled ? (
              <span
                key={item.href}
                aria-disabled="true"
                title="Available in a later ticket"
                className="flex cursor-not-allowed items-center gap-2.5 rounded-md px-3 py-2 text-sm text-white/35"
              >
                <Icon name={item.icon} />
                {item.label}
              </span>
            ) : (
              <Link
                key={item.href}
                href={item.href}
                aria-current={isActive(item.href) ? "page" : undefined}
                className={`flex items-center gap-2.5 rounded-md px-3 py-2 text-sm transition-colors ${
                  isActive(item.href)
                    ? "bg-white/10 font-medium text-white shadow-[inset_3px_0_0_var(--color-brass)]"
                    : "text-white/70 hover:bg-white/5 hover:text-white"
                }`}
              >
                <Icon name={item.icon} />
                {item.label}
              </Link>
            ),
          )}
        </nav>

        <div className="mt-auto hidden border-t border-white/10 px-5 py-5 md:block">
          <Link href="/profile" className="block rounded hover:text-white">
            <p className="text-sm font-medium">{user.name}</p>
            <p className="truncate text-xs text-white/55">{user.email}</p>
          </Link>
          <button
            onClick={signOut}
            className="mt-3 inline-flex items-center gap-2 text-xs text-white/70 hover:text-white"
          >
            <Icon name="out" size={14} /> Sign out
          </button>
          <div className="mt-5 rounded-md bg-white/5 p-3 text-xs leading-relaxed text-white/60">
            {config.dataMode === "api" ? (
              <span className={api?.ok === false ? "text-[#f0b4ab]" : ""} role="status">
                {api ? api.text : "Checking API connection…"}
              </span>
            ) : (
              <>
                {config.authMode === "preview" ? "Preview mode. " : ""}Projects are saved in this browser.
                <button
                  onClick={async () => {
                    await resetSampleData();
                    window.location.reload();
                  }}
                  className="mt-1.5 block font-medium text-white/80 underline-offset-2 hover:underline"
                >
                  Reset sample data
                </button>
              </>
            )}
          </div>
        </div>
      </aside>

      <main className="min-w-0 px-4 py-6 sm:px-8 md:py-8">{children}</main>
    </div>
  );
}
