"use client";

import { useEffect } from "react";
import { STATUS_LABEL, type ProjectStatus } from "@/lib/types";

export function StatusPill({ status }: { status: ProjectStatus }) {
  const tone =
    status === "active"
      ? "bg-brass-wash text-brass-deep"
      : status === "complete"
        ? "bg-line-soft text-ink-2"
        : "bg-white text-ink-2 ring-1 ring-line ring-inset";
  return (
    <span className={`inline-flex items-center rounded px-2 py-0.5 text-xs font-medium ${tone}`}>
      {STATUS_LABEL[status]}
    </span>
  );
}

export function Toast({ message, onDone }: { message: string | null; onDone: () => void }) {
  useEffect(() => {
    if (!message) return;
    const t = setTimeout(onDone, 2600);
    return () => clearTimeout(t);
  }, [message, onDone]);
  if (!message) return null;
  return (
    <div
      role="status"
      className="toast fixed bottom-5 left-1/2 z-50 -translate-x-1/2 rounded-md bg-steel px-4 py-2.5 text-sm font-medium text-white shadow-lg"
    >
      {message}
    </div>
  );
}

/** Inline busy indicator for buttons and inline loading text (requirements s23). */
export function Spinner({ size = 14 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 16 16" className="spin" aria-hidden="true">
      <circle cx="8" cy="8" r="6" fill="none" stroke="currentColor" strokeWidth="2" opacity="0.25" />
      <path d="M8 2a6 6 0 0 1 6 6" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
  );
}

export function Icon({ name, size = 16 }: { name: "grid" | "folder" | "plus" | "search" | "out" | "calc" | "save" | "trash" | "back" | "user" | "queue" | "period"; size?: number }) {
  const p = { fill: "none", stroke: "currentColor", strokeWidth: 1.6, strokeLinecap: "round" as const, strokeLinejoin: "round" as const };
  const paths: Record<string, React.ReactNode> = {
    grid: (<><rect x="2.5" y="2.5" width="4.5" height="4.5" rx="1" {...p} /><rect x="9" y="2.5" width="4.5" height="4.5" rx="1" {...p} /><rect x="2.5" y="9" width="4.5" height="4.5" rx="1" {...p} /><rect x="9" y="9" width="4.5" height="4.5" rx="1" {...p} /></>),
    folder: <path d="M2 4.5c0-.6.4-1 1-1h3l1.5 1.5H13c.6 0 1 .4 1 1V12c0 .6-.4 1-1 1H3c-.6 0-1-.4-1-1z" {...p} />,
    plus: <path d="M8 3v10M3 8h10" {...p} />,
    search: (<><circle cx="7" cy="7" r="4.25" {...p} /><path d="m10.2 10.2 3.3 3.3" {...p} /></>),
    out: <path d="M6 13H3.5c-.6 0-1-.4-1-1V4c0-.6.4-1 1-1H6M10.5 11 13.5 8l-3-3M13.5 8H6" {...p} />,
    calc: (<><rect x="3" y="2" width="10" height="12" rx="1.5" {...p} /><path d="M5.5 5h5M5.5 8.5h1M9.5 8.5h1M5.5 11h1M9.5 11h1" {...p} /></>),
    save: <path d="M3 3.5c0-.3.2-.5.5-.5h7.3L13 5.2v7.3c0 .3-.2.5-.5.5h-9c-.3 0-.5-.2-.5-.5zM5.5 3v3h4.5V3M5 13V9.5h6V13" {...p} />,
    trash: <path d="M3 4.5h10M6.5 4.5V3h3v1.5M4.5 4.5l.6 8.5h5.8l.6-8.5" {...p} />,
    back: <path d="M9.5 3.5 5 8l4.5 4.5" {...p} />,
    user: (<><circle cx="8" cy="5.5" r="2.75" {...p} /><path d="M2.75 13.5c0-2.4 2.35-4 5.25-4s5.25 1.6 5.25 4" {...p} /></>),
    queue: (<><path d="M2.5 4h7M2.5 8h7M2.5 12h4" {...p} /><path d="M11.5 10.5 13 12l2.5-3" {...p} /></>),
    period: (<><rect x="2.5" y="3.5" width="11" height="10" rx="1.5" {...p} /><path d="M2.5 6.5h11M5.5 2.5v2M10.5 2.5v2" {...p} /></>),
  };
  return (
    <svg width={size} height={size} viewBox="0 0 16 16" aria-hidden="true">
      {paths[name]}
    </svg>
  );
}
