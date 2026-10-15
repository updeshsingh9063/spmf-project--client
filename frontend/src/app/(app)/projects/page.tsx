"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { CalcBadge, Icon, StatusPill } from "@/components/ui";
import { fmtMoney, fmtPct, fmtRelative } from "@/lib/format";
import { listProjects } from "@/lib/projectStore";
import { calcState } from "@/lib/spfm";
import type { Project, ProjectStatus } from "@/lib/types";

const FILTERS: { key: ProjectStatus | "all"; label: string }[] = [
  { key: "all", label: "All" },
  { key: "quote", label: "Quotes" },
  { key: "active", label: "Active" },
  { key: "complete", label: "Complete" },
];

export default function ProjectsPage() {
  const [projects, setProjects] = useState<Project[] | null>(null);
  const [query, setQuery] = useState("");
  const [filter, setFilter] = useState<ProjectStatus | "all">("all");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    listProjects().then((page) => setProjects(page.projects), (e: Error) => setError(e.message));
  }, []);

  const shown = useMemo(() => {
    const q = query.trim().toLowerCase();
    return (projects ?? []).filter(
      (p) =>
        (filter === "all" || p.status === filter) &&
        (!q || [p.number, p.name, p.client, p.siteAddress].some((s) => s.toLowerCase().includes(q))),
    );
  }, [projects, query, filter]);

  return (
    <div className="mx-auto max-w-6xl">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="display text-[1.75rem] leading-tight">Projects</h1>
          <p className="mt-1 text-sm text-ink-2">Open a project to enter figures and run the calculations.</p>
        </div>
        <Link href="/projects/new" className="btn btn-brass">
          <Icon name="plus" /> New project
        </Link>
      </header>

      <div className="mt-6 flex flex-wrap items-center gap-3">
        <label className="relative w-full sm:w-80">
          <span className="sr-only">Search projects</span>
          <span className="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-muted">
            <Icon name="search" />
          </span>
          <input
            className="input pl-9"
            placeholder="Search by name, client or number"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
        </label>
        <div className="flex rounded-md border border-line bg-panel p-0.5" role="group" aria-label="Filter by status">
          {FILTERS.map((f) => (
            <button
              key={f.key}
              onClick={() => setFilter(f.key)}
              aria-pressed={filter === f.key}
              className={`rounded px-3 py-1.5 text-sm ${filter === f.key ? "bg-steel text-white" : "text-ink-2 hover:text-ink"}`}
            >
              {f.label}
            </button>
          ))}
        </div>
      </div>

      <section className="panel mt-4 overflow-hidden">
        {error ? (
          <p role="alert" className="p-6 text-sm text-bad">Couldn&apos;t load projects. {error}</p>
        ) : !projects ? (
          <p className="p-6 text-sm text-muted">Loading projects…</p>
        ) : shown.length === 0 ? (
          <div className="p-10 text-center">
            <p className="font-medium">No projects match “{query || FILTERS.find((f) => f.key === filter)?.label}”.</p>
            <p className="mt-1 text-sm text-ink-2">Try another search, or start a new project.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[820px]">
              <thead className="table-head">
                <tr>
                  <th>Number</th>
                  <th>Project</th>
                  <th>Status</th>
                  <th className="!text-right">Contract value</th>
                  <th className="!text-right">Gross margin</th>
                  <th>Calculation</th>
                  <th>Updated</th>
                </tr>
              </thead>
              <tbody>
                {shown.map((p) => (
                  <tr key={p.id} className="table-row hover:bg-paper/60">
                    <td className="text-ink-2">{p.number}</td>
                    <td>
                      <Link href={`/projects/${p.id}`} className="font-medium hover:underline">
                        {p.name}
                      </Link>
                      <span className="block text-xs text-muted">{p.client}</span>
                    </td>
                    <td><StatusPill status={p.status} /></td>
                    <td className="text-right">{p.results ? fmtMoney(p.results.revenue) : "–"}</td>
                    <td className="text-right">{fmtPct(p.results?.grossMarginPct ?? null)}</td>
                    <td><CalcBadge state={calcState(p)} /></td>
                    <td className="text-ink-2">{fmtRelative(p.updatedAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
      {projects && (
        <p className="mt-3 text-xs text-muted">
          Showing {shown.length} of {projects.length} projects
        </p>
      )}
    </div>
  );
}
