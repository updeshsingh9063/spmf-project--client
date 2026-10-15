"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { CalcBadge, Icon, Spinner, StatusPill } from "@/components/ui";
import { config } from "@/lib/config";
import { fmtMoney, fmtPct, fmtRelative } from "@/lib/format";
import { listProjects } from "@/lib/projectStore";
import { calcState } from "@/lib/spfm";
import type { Project, ProjectStatus } from "@/lib/types";

const FILTERS: { key: ProjectStatus | "all"; label: string }[] = [
  { key: "all", label: "All" },
  { key: "quote", label: "Quotes" },
  { key: "active", label: "Active" },
  { key: "complete", label: "Complete" },
  { key: "archived", label: "Archived" },
];

/** Sort options (requirements s27: sorting, recent, alphabetical, number, customer). */
type SortKey = "updated" | "name" | "number" | "client" | "value";

const SORTS: { key: SortKey; label: string }[] = [
  { key: "updated", label: "Recently updated" },
  { key: "name", label: "Project name (A–Z)" },
  { key: "number", label: "Project number" },
  { key: "client", label: "Customer (A–Z)" },
  { key: "value", label: "Contract value (high–low)" },
];

function sortProjects(projects: Project[], key: SortKey): Project[] {
  const by = [...projects];
  switch (key) {
    case "name":
      return by.sort((a, b) => a.name.localeCompare(b.name));
    case "number":
      return by.sort((a, b) => a.number.localeCompare(b.number, undefined, { numeric: true }));
    case "client":
      return by.sort((a, b) => a.client.localeCompare(b.client) || a.name.localeCompare(b.name));
    case "value":
      return by.sort((a, b) => (b.results?.revenue ?? -1) - (a.results?.revenue ?? -1));
    default:
      return by.sort((a, b) => b.updatedAt.localeCompare(a.updatedAt));
  }
}

export default function ProjectsPage() {
  const [projects, setProjects] = useState<Project[] | null>(null);
  const [query, setQuery] = useState("");
  const [filter, setFilter] = useState<ProjectStatus | "all">("all");
  const [sort, setSort] = useState<SortKey>("updated");
  const [cursor, setCursor] = useState<string | null>(null);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async (opts: { query: string; filter: ProjectStatus | "all" }) => {
    setError(null);
    try {
      const page = await listProjects({
        query: opts.query || undefined,
        status: opts.filter === "all" ? undefined : opts.filter,
      });
      setProjects(page.projects);
      setCursor(page.nextCursor);
    } catch (e) {
      setError((e as Error).message);
    }
  }, []);

  // Search and filter run server-side in API mode, so the query is debounced:
  // one request per pause in typing, not one per keystroke.
  useEffect(() => {
    const t = setTimeout(() => void load({ query, filter }), 250);
    return () => clearTimeout(t);
  }, [query, filter, load]);

  const loadMore = async () => {
    if (!cursor) return;
    setLoadingMore(true);
    try {
      const page = await listProjects({
        query: query || undefined,
        status: filter === "all" ? undefined : filter,
        cursor,
      });
      setProjects((prev) => [...(prev ?? []), ...page.projects]);
      setCursor(page.nextCursor);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoadingMore(false);
    }
  };

  // In local mode the whole set is already here, so filtering stays in the browser.
  const shown = useMemo(() => {
    if (!projects) return [];
    if (config.dataMode === "api") return sortProjects(projects, sort);

    const q = query.trim().toLowerCase();
    return sortProjects(
      projects.filter(
        (p) =>
          (filter === "all" || p.status === filter) &&
          (!q || [p.number, p.name, p.client, p.siteAddress].some((s) => s.toLowerCase().includes(q))),
      ),
      sort,
    );
  }, [projects, query, filter, sort]);

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
            placeholder="Search by name, customer or number"
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

        <label className="ml-auto flex items-center gap-2 text-sm text-ink-2">
          <span>Sort by</span>
          <select className="input h-9 w-auto py-0" value={sort} onChange={(e) => setSort(e.target.value as SortKey)}>
            {SORTS.map((s) => (
              <option key={s.key} value={s.key}>{s.label}</option>
            ))}
          </select>
        </label>
      </div>

      <section className="panel mt-4 overflow-hidden">
        {error ? (
          <p role="alert" className="p-6 text-sm text-bad">Couldn&apos;t load projects. {error}</p>
        ) : !projects ? (
          <p className="flex items-center gap-2 p-6 text-sm text-muted"><Spinner /> Loading projects…</p>
        ) : shown.length === 0 ? (
          <div className="p-10 text-center">
            <p className="font-medium">
              No projects match “{query || FILTERS.find((f) => f.key === filter)?.label}”.
            </p>
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
        <div className="mt-3 flex items-center justify-between gap-4">
          <p className="text-xs text-muted">
            Showing {shown.length} {shown.length === 1 ? "project" : "projects"}
            {cursor ? " so far" : ""}
          </p>
          {cursor && (
            <button onClick={loadMore} disabled={loadingMore} className="btn btn-quiet h-8 px-3 text-xs">
              {loadingMore ? <><Spinner /> Loading…</> : "Load more"}
            </button>
          )}
        </div>
      )}
    </div>
  );
}
