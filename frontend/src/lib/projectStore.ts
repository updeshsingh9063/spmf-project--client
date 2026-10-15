// Project repository.
//
// Two modes, chosen by NEXT_PUBLIC_DATA_MODE (see .env.example):
//
//  - "api" (production): the ASP.NET Core API, called with the signed-in user's
//    Entra access token. The server owns the calculations, the change history and
//    concurrency control.
//
//  - "local" (preview): projects kept in this browser with sample data, so the whole
//    create -> calculate -> save -> reopen loop can be demonstrated without a backend
//    or a Shield account. Calculations run in the browser and are PREVIEW ONLY.

import { apiGet, apiPost, apiPut } from "./api";
import {
  summaryToProject,
  toFormField,
  toProject,
  toResults,
  toSaveRequest,
  type ApiChangeSet,
  type ApiPage,
  type ApiProjectDetail,
  type ApiProjectSummary,
  type ApiResults,
} from "./apiMapping";
import { config } from "./config";
import { diffProjects } from "./history";
import { seedProjects } from "./seed";
import { calculate, fingerprint } from "./spfm";
import type {
  CalculationState,
  HistoryEntry,
  Project,
  ProjectInputs,
  ProjectStatus,
  SpfmResults,
} from "./types";

export interface ProjectDraft {
  name: string;
  client: string;
  customerId?: string | null;
  siteAddress: string;
  status: ProjectStatus;
  inputs: ProjectInputs;
  results: Project["results"];
  calculatedFor: string | null;
  calculatedAt: string | null;
}

const apiMode = () => config.dataMode === "api";

/* ---------- local (browser) storage: preview mode only ---------- */

const KEY = "spfm.projects.v1";

function normalize(p: Partial<Project> & Pick<Project, "id">): Project {
  return {
    ...(p as Project),
    quickbooks: p.quickbooks ?? { customerId: null, projectId: null, lastSyncedAt: null },
    history: p.history ?? [],
  };
}

function readAll(): Project[] {
  if (typeof window === "undefined") return [];
  try {
    const raw = window.localStorage.getItem(KEY);
    if (raw) return (JSON.parse(raw) as Project[]).map(normalize);
  } catch {
    /* storage unavailable or corrupt: fall back to sample data */
  }
  const seeded = seedProjects();
  writeAll(seeded);
  return seeded;
}

function writeAll(projects: Project[]) {
  try {
    window.localStorage.setItem(KEY, JSON.stringify(projects));
  } catch {
    /* ignore: preview only */
  }
}

function nextNumber(all: Project[]): string {
  const year = new Date().getFullYear();
  const prefix = `SSD-${year}-`;
  const max = all
    .filter((p) => p.number.startsWith(prefix))
    .reduce((m, p) => Math.max(m, Number(p.number.slice(prefix.length)) || 0), 0);
  return `${prefix}${String(max + 1).padStart(3, "0")}`;
}

const entry = (by: string, action: HistoryEntry["action"], changes: HistoryEntry["changes"]): HistoryEntry => ({
  id: crypto.randomUUID(),
  at: new Date().toISOString(),
  by,
  action,
  changes,
});

/* ---------- public API (same signatures in both modes) ---------- */

export interface ProjectPage {
  projects: Project[];
  nextCursor: string | null;
}

export async function listProjects(
  options: { query?: string; status?: ProjectStatus; cursor?: string; limit?: number } = {},
): Promise<ProjectPage> {
  if (apiMode()) {
    const params = new URLSearchParams();
    if (options.query?.trim()) params.set("q", options.query.trim());
    if (options.status) params.set("status", options.status);
    if (options.cursor) params.set("cursor", options.cursor);
    if (options.limit) params.set("limit", String(options.limit));

    const qs = params.toString();
    const { data } = await apiGet<ApiPage<ApiProjectSummary>>(`/api/v1/projects${qs ? `?${qs}` : ""}`);
    return { projects: data.items.map(summaryToProject), nextCursor: data.nextCursor };
  }

  // Local mode filters in the browser: the dataset is sample data, so paging it
  // would add complexity for no benefit.
  const q = options.query?.trim().toLowerCase();
  const projects = readAll()
    .filter(
      (p) =>
        (!options.status || p.status === options.status) &&
        (!q || [p.number, p.name, p.client, p.siteAddress].some((s) => s.toLowerCase().includes(q))),
    )
    .sort((a, b) => b.updatedAt.localeCompare(a.updatedAt));

  return { projects, nextCursor: null };
}

export async function getProject(id: string): Promise<Project | null> {
  if (apiMode()) {
    try {
      const { data, etag } = await apiGet<ApiProjectDetail>(`/api/v1/projects/${encodeURIComponent(id)}`);
      return { ...toProject(data), etag: etag ?? data.etag };
    } catch (e) {
      if ((e as { status?: number }).status === 404) return null;
      throw e;
    }
  }
  return readAll().find((p) => p.id === id) ?? null;
}

export async function createProject(draft: ProjectDraft, by: string): Promise<Project> {
  if (apiMode()) {
    const { data, etag } = await apiPost<ApiProjectDetail>("/api/v1/projects", toSaveRequest(draft));
    return { ...toProject(data), etag: etag ?? data.etag };
  }

  const all = readAll();
  const now = new Date().toISOString();
  const project: Project = {
    ...draft,
    id: crypto.randomUUID(),
    number: nextNumber(all),
    quickbooks: { customerId: null, projectId: null, lastSyncedAt: null },
    history: [entry(by, "created", [])],
    createdAt: now,
    updatedAt: now,
  };
  writeAll([...all, project]);
  return project;
}

export async function updateProject(
  id: string,
  draft: ProjectDraft,
  by: string,
  etag?: string | null,
): Promise<Project> {
  if (apiMode()) {
    // If-Match carries the version this browser loaded. If someone else saved in the
    // meantime the API answers 409 rather than letting one edit erase the other.
    const { data, etag: newEtag } = await apiPut<ApiProjectDetail>(
      `/api/v1/projects/${encodeURIComponent(id)}`,
      toSaveRequest(draft),
      etag ?? null,
    );
    return { ...toProject(data), etag: newEtag ?? data.etag };
  }

  const all = readAll();
  const i = all.findIndex((p) => p.id === id);
  if (i < 0) throw new Error("This project no longer exists.");
  const changes = diffProjects(all[i], draft);
  const updated: Project = {
    ...all[i],
    ...draft,
    history: changes.length ? [entry(by, "updated", changes), ...all[i].history] : all[i].history,
    updatedAt: changes.length ? new Date().toISOString() : all[i].updatedAt,
  };
  all[i] = updated;
  writeAll(all);
  return updated;
}

/**
 * Runs the SPFM calculations.
 *
 * In API mode the server computes and stores the figures: money is decimal there,
 * and the result becomes a permanent, attributable calculation run. The browser
 * only displays what comes back.
 */
export async function calculateProject(
  id: string,
  draft: ProjectDraft,
): Promise<{ results: SpfmResults; state: CalculationState; engineVersion: string | null }> {
  if (apiMode()) {
    const { data } = await apiPost<{
      results: ApiResults;
      calculationState: CalculationState;
      engineVersion: string;
    }>(`/api/v1/projects/${encodeURIComponent(id)}/calculate`);

    return {
      results: toResults(data.results)!,
      state: data.calculationState,
      engineVersion: data.engineVersion,
    };
  }

  return { results: calculate(draft.inputs), state: "current", engineVersion: "preview" };
}

export async function getHistory(
  id: string,
  cursor?: string,
): Promise<{ entries: HistoryEntry[]; nextCursor: string | null }> {
  if (apiMode()) {
    const params = cursor ? `?cursor=${encodeURIComponent(cursor)}` : "";
    const { data } = await apiGet<ApiPage<ApiChangeSet>>(
      `/api/v1/projects/${encodeURIComponent(id)}/history${params}`,
    );

    return {
      entries: data.items.map((c) => ({
        id: c.id,
        at: c.at,
        by: c.by,
        action: c.action === "calculated" ? "updated" : c.action,
        changes: c.changes.map((f) => ({ field: f.field, from: f.from ?? "", to: f.to ?? "" })),
      })),
      nextCursor: data.nextCursor,
    };
  }

  const project = readAll().find((p) => p.id === id);
  return { entries: project?.history ?? [], nextCursor: null };
}

export async function resetSampleData(): Promise<void> {
  if (apiMode()) throw new Error("Sample data is only available in preview mode.");
  writeAll(seedProjects());
}

export function emptyInputs(): ProjectInputs {
  return {
    products: [{ id: crypto.randomUUID(), description: "", qty: 1, unitCost: 0, unitPrice: 0, qbItemId: null }],
    variations: 0,
    labourHours: 0,
    labourRate: 0,
    subcontractorCost: 0,
    otherDirectCost: 0,
    overheadPct: 0,
  };
}

/** Local-mode freshness. In API mode the server reports it instead. */
export function localCalculationState(p: Pick<Project, "inputs" | "results" | "calculatedFor">): CalculationState {
  if (!p.results || !p.calculatedFor) return "never";
  return p.calculatedFor === fingerprint(p.inputs) ? "current" : "outdated";
}

export { toFormField };
