"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { CalcBadge, Icon, Spinner, StatusPill } from "@/components/ui";
import { fmtMoney, fmtPct, fmtRelative } from "@/lib/format";
import { listProjects } from "@/lib/projectStore";
import { calcState } from "@/lib/spfm";
import type { Project } from "@/lib/types";

export default function OverviewPage() {
  const [projects, setProjects] = useState<Project[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    listProjects().then((page) => setProjects(page.projects), (e: Error) => setError(e.message));
  }, []);

  if (error)
    return (
      <div role="alert" className="mx-auto max-w-6xl rounded-md border border-bad/30 bg-bad-wash px-4 py-3 text-sm text-bad">
        Couldn&apos;t load projects. {error}
      </div>
    );

  if (!projects)
    return <p className="flex items-center gap-2 text-sm text-muted"><Spinner /> Loading projects…</p>;

  // Totals use projects that are won (active or complete) and have results.
  const won = projects.filter((p) => p.status !== "quote" && p.results);
  const sum = (f: (p: Project) => number) => won.reduce((s, p) => s + f(p), 0);
  const revenue = sum((p) => p.results!.revenue);
  const gross = sum((p) => p.results!.grossProfit);
  const net = sum((p) => p.results!.netProfit);
  const grossMargin = revenue ? (gross / revenue) * 100 : null;

  const attention = projects.filter((p) => calcState(p) !== "current");
  const withMargin = projects.filter((p) => p.results && p.results.grossMarginPct !== null);
  const maxMargin = Math.max(1, ...withMargin.map((p) => p.results!.grossMarginPct!));

  return (
    <div className="mx-auto max-w-6xl">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="display text-[1.75rem] leading-tight">Overview</h1>
          <p className="mt-1 text-sm text-ink-2">
            {projects.length} {projects.length === 1 ? "project" : "projects"} in total. Financial figures cover the {won.length} active and completed; quotes are left out.
          </p>
        </div>
        <Link href="/projects/new" className="btn btn-brass">
          <Icon name="plus" /> New project
        </Link>
      </header>

      <section className="panel mt-6 grid grid-cols-2 lg:grid-cols-5" aria-label="Portfolio totals">
        {[
          { label: "Total projects", value: String(projects.length) },
          { label: "Contract value", value: fmtMoney(revenue) },
          { label: "Gross profit", value: fmtMoney(gross) },
          { label: "Gross margin", value: fmtPct(grossMargin === null ? null : Math.round(grossMargin * 10) / 10) },
          { label: "Net profit after overhead", value: fmtMoney(net), tone: net < 0 ? "text-bad" : "" },
        ].map((m, i) => (
          <div
            key={m.label}
            className={`px-5 py-4 ${i % 2 ? "border-l border-line-soft" : ""} ${i > 1 ? "border-t border-line-soft lg:border-t-0" : ""} lg:border-l lg:first:border-l-0`}
          >
            <p className="text-xs text-muted">{m.label}</p>
            <p className={`display mt-1 text-2xl ${m.tone ?? ""}`}>{m.value}</p>
          </div>
        ))}
      </section>

      <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-[1.4fr_1fr]">
        <section className="panel p-5">
          <h2 className="text-[0.95rem] font-semibold">Gross margin by project</h2>
          <p className="mt-0.5 text-xs text-muted">Calculated projects only, highest first.</p>
          <ul className="mt-5 space-y-3">
            {[...withMargin]
              .sort((a, b) => b.results!.grossMarginPct! - a.results!.grossMarginPct!)
              .map((p) => {
                const m = p.results!.grossMarginPct!;
                return (
                  <li key={p.id}>
                    <Link href={`/projects/${p.id}`} className="group grid grid-cols-[minmax(0,11rem)_1fr] items-center gap-3">
                      <span className="truncate text-sm text-ink-2 group-hover:text-ink group-hover:underline">{p.name}</span>
                      <span className="flex items-center gap-2" title={`${p.name}: ${fmtPct(m)} gross margin`}>
                        <span
                          className="h-3.5 rounded-r-[4px]"
                          style={{
                            width: `${(Math.max(m, 0) / maxMargin) * 80}%`,
                            minWidth: 2,
                            background: m < 0 ? "var(--color-bad)" : "var(--color-s1)",
                          }}
                        />
                        <span className={`text-xs font-medium ${m < 0 ? "text-bad" : "text-ink"}`}>{fmtPct(m)}</span>
                      </span>
                    </Link>
                  </li>
                );
              })}
          </ul>
        </section>

        <section className="panel p-5">
          <h2 className="text-[0.95rem] font-semibold">Needs calculating</h2>
          <p className="mt-0.5 text-xs text-muted">Results are missing or older than the latest inputs.</p>
          {attention.length === 0 ? (
            <p className="mt-6 text-sm text-ink-2">All projects are up to date.</p>
          ) : (
            <ul className="mt-4 divide-y divide-line-soft">
              {attention.map((p) => (
                <li key={p.id} className="flex items-center justify-between gap-3 py-3">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium">{p.name}</p>
                    <CalcBadge state={calcState(p)} />
                  </div>
                  <Link href={`/projects/${p.id}`} className="btn btn-quiet h-8 shrink-0 px-3 text-xs">
                    Open project
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>

      <section className="panel mt-6 overflow-hidden">
        <div className="flex items-center justify-between px-5 py-4">
          <h2 className="text-[0.95rem] font-semibold">Recently updated</h2>
          <Link href="/projects" className="text-sm font-medium text-brass-deep hover:underline">
            All projects
          </Link>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full min-w-[640px]">
            <thead className="table-head">
              <tr>
                <th>Project</th>
                <th>Status</th>
                <th className="!text-right">Contract value</th>
                <th className="!text-right">Gross margin</th>
                <th>Updated</th>
              </tr>
            </thead>
            <tbody>
              {projects.slice(0, 5).map((p) => (
                <tr key={p.id} className="table-row hover:bg-paper/60">
                  <td>
                    <Link href={`/projects/${p.id}`} className="font-medium hover:underline">
                      {p.name}
                    </Link>
                    <span className="block text-xs text-muted">{p.number} for {p.client}</span>
                  </td>
                  <td><StatusPill status={p.status} /></td>
                  <td className="text-right">{p.results ? fmtMoney(p.results.revenue) : "–"}</td>
                  <td className="text-right">{fmtPct(p.results?.grossMarginPct ?? null)}</td>
                  <td className="text-ink-2">{fmtRelative(p.updatedAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
