"use client";

// Project workspace.
//
// TICKET 1 — CONTROLLED RESET. The job-costing workflow that lived here has been
// removed: doors and hardware line items, labour hours and rate, subcontractors,
// other direct costs, overhead percentage, and the gross-margin / net-profit
// results panel. SPFM is a financial operations layer over QuickBooks, so those
// figures come from synchronised records and shipment decisions, not from a form.
//
// What remains is project identity — the part the client marked KEEP / REWORK —
// plus labelled placeholders for the sections the design document specifies:
// financial summary (Ticket 8), linked QuickBooks activity (Ticket 9), shipments
// (Ticket 11) and open decisions (Ticket 15).
//
// Deliberately not polished. Ticket 1's own note: "Do not spend time polishing
// replacement screens yet; this ticket is a controlled reset."

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Icon, Spinner, Toast } from "@/components/ui";
import { ApiError, ValidationError } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { fmtDateTime } from "@/lib/format";
import { createProject, getHistory, toFormField, updateProject, type ProjectDraft } from "@/lib/projectStore";
import { useUnsavedChanges } from "@/lib/useUnsavedChanges";
import { STATUS_LABEL, type HistoryEntry, type Project } from "@/lib/types";

type FieldErrors = Record<string, string>;

const blankDraft = (): ProjectDraft => ({
  name: "",
  client: "",
  customerId: null,
  siteAddress: "",
  status: "quote",
});

const toDraft = (p: Project): ProjectDraft => ({
  name: p.name,
  client: p.client,
  customerId: p.customerId ?? null,
  siteAddress: p.siteAddress,
  status: p.status,
});

/** Project identity only. Financial validation belongs to the calculation service. */
function validateIdentity(draft: ProjectDraft): FieldErrors {
  const e: FieldErrors = {};
  if (!draft.name.trim()) e.name = "Enter a project name.";
  if (!draft.client.trim()) e.client = "Enter the customer's name.";
  return e;
}

/** A section the design document specifies but a later ticket builds. */
function Planned({ title, ticket, children }: { title: string; ticket: number; children: React.ReactNode }) {
  return (
    <section className="panel p-5">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <h2 className="text-[0.95rem] font-semibold text-ink-2">{title}</h2>
        <span className="font-mono text-xs text-muted">Ticket {ticket}</span>
      </div>
      <p className="mt-1.5 max-w-[62ch] text-sm text-muted">{children}</p>
    </section>
  );
}

function History({ entries, loading }: { entries: HistoryEntry[]; loading: boolean }) {
  return (
    <section className="panel p-5" aria-labelledby="history-h">
      <h2 id="history-h" className="text-[0.95rem] font-semibold">Change history</h2>
      <p className="mt-0.5 text-xs text-muted">Who changed what, and when.</p>

      {loading && entries.length === 0 ? (
        <p className="mt-4 flex items-center gap-2 text-sm text-muted"><Spinner /> Loading history…</p>
      ) : entries.length === 0 ? (
        <p className="mt-4 text-sm text-ink-2">No changes yet.</p>
      ) : (
        <ol className="mt-4 space-y-4 border-l border-line pl-4">
          {entries.map((h) => (
            <li key={h.id} className="relative">
              <span className="absolute top-1.5 -left-[21px] h-2 w-2 rounded-full bg-steel ring-2 ring-white" aria-hidden="true" />
              <p className="text-sm">
                <span className="font-medium">{h.by}</span>{" "}
                <span className="text-ink-2">
                  {h.action === "created" ? "created the project" : `made ${h.changes.length} ${h.changes.length === 1 ? "change" : "changes"}`}
                </span>
              </p>
              <p className="text-xs text-muted">{fmtDateTime(h.at)}</p>
              {h.changes.length > 0 && (
                <ul className="mt-1.5 space-y-0.5 text-xs text-ink-2">
                  {h.changes.map((c, i) => (
                    <li key={i}>
                      <span className="text-ink">{c.field}:</span> {c.from || "blank"}{" "}
                      <span aria-label="changed to">→</span> {c.to || "blank"}
                    </li>
                  ))}
                </ul>
              )}
            </li>
          ))}
        </ol>
      )}
    </section>
  );
}

export function ProjectEditor({ project }: { project: Project | null }) {
  const router = useRouter();
  const { user } = useAuth();
  const by = user?.name ?? "Unknown user";

  const [draft, setDraft] = useState<ProjectDraft>(() => (project ? toDraft(project) : blankDraft()));
  const [saved, setSaved] = useState<string>(() => JSON.stringify(project ? toDraft(project) : blankDraft()));
  const [etag, setEtag] = useState<string | null>(project?.etag ?? null);

  const [history, setHistory] = useState<HistoryEntry[]>([]);
  const [historyLoading, setHistoryLoading] = useState(false);

  const [showErrors, setShowErrors] = useState(false);
  const [serverErrors, setServerErrors] = useState<FieldErrors>({});
  const [toast, setToast] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [conflict, setConflict] = useState(false);

  const clearToast = useCallback(() => setToast(null), []);

  const loadHistory = useCallback(async () => {
    if (!project) return;
    setHistoryLoading(true);
    try {
      const page = await getHistory(project.id);
      setHistory(page.entries);
    } catch {
      // History is supporting detail; failing to load it must not block editing.
    } finally {
      setHistoryLoading(false);
    }
  }, [project]);

  useEffect(() => {
    void loadHistory();
  }, [loadHistory]);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    if (params.get("created")) {
      setToast("Project created");
      window.history.replaceState(null, "", window.location.pathname);
    }
  }, []);

  const localErrors = useMemo(() => (showErrors ? validateIdentity(draft) : {}), [showErrors, draft]);
  const errors: FieldErrors = useMemo(() => ({ ...localErrors, ...serverErrors }), [localErrors, serverErrors]);

  const dirty = JSON.stringify(draft) !== saved;
  useUnsavedChanges(dirty, "This project has unsaved changes. Leave without saving?");

  const patch = (p: Partial<ProjectDraft>) => {
    setDraft((d) => ({ ...d, ...p }));
    setServerErrors({});
  };

  const describeError = (err: unknown): string => {
    if (err instanceof ApiError) return err.message;
    return err instanceof Error ? err.message : "Something went wrong. Please try again.";
  };

  const save = async () => {
    const e = validateIdentity(draft);
    setShowErrors(true);
    if (Object.keys(e).length) {
      setToast(`Fix ${Object.keys(e).length} ${Object.keys(e).length === 1 ? "field" : "fields"} before saving`);
      return;
    }

    setSaving(true);
    setConflict(false);
    try {
      if (project) {
        const p = await updateProject(project.id, draft, by, etag);
        setEtag(p.etag ?? null);
        setSaved(JSON.stringify(toDraft(p)));
        setDraft(toDraft(p));
        setToast("Project saved");
        void loadHistory();
      } else {
        const p = await createProject(draft, by);
        setSaved(JSON.stringify(draft));
        router.replace(`/projects/${p.id}?created=1`);
      }
    } catch (err) {
      if (err instanceof ValidationError) {
        setServerErrors(
          Object.fromEntries(Object.entries(err.toFieldErrors()).map(([f, m]) => [toFormField(f), m])),
        );
        setShowErrors(true);
        setToast("Some details need fixing before this can be saved.");
      } else if (err instanceof ApiError && err.status === 409) {
        setConflict(true);
        setToast("Someone else changed this project.");
      } else {
        setToast(describeError(err));
      }
    } finally {
      setSaving(false);
    }
  };

  const cancel = () => {
    if (dirty && !window.confirm("Discard the changes you've made to this project?")) return;
    if (project) {
      setDraft(JSON.parse(saved) as ProjectDraft);
      setServerErrors({});
      setShowErrors(false);
      setToast("Changes discarded");
    } else {
      router.push("/projects");
    }
  };

  const errCount = Object.keys(errors).length;

  return (
    <div className="mx-auto max-w-4xl">
      <Link href="/projects" className="inline-flex items-center gap-1 text-sm text-ink-2 hover:text-ink">
        <Icon name="back" size={14} /> Projects
      </Link>

      <header className="mt-3 flex flex-wrap items-end justify-between gap-4">
        <div className="min-w-0">
          <p className="text-sm text-muted">{project ? project.number : "New project"}</p>
          <h1 className="display truncate text-[1.75rem] leading-tight">{draft.name.trim() || "Untitled project"}</h1>
        </div>
        <div className="flex items-center gap-3">
          {dirty && <span className="text-xs text-warn">Unsaved changes</span>}
          <button onClick={cancel} disabled={saving} className="btn btn-quiet">
            {project ? "Discard changes" : "Cancel"}
          </button>
          <button onClick={save} disabled={saving} className="btn btn-brass">
            {saving ? <Spinner /> : <Icon name="save" />}
            {saving ? "Saving…" : project ? "Save project" : "Create project"}
          </button>
        </div>
      </header>

      {conflict && (
        <div role="alert" className="mt-4 rounded-md border border-warn/40 bg-warn-wash px-4 py-3 text-sm text-warn">
          <p className="font-medium">Someone else changed this project while you were editing it.</p>
          <p className="mt-1">Your changes have not been saved, so theirs are intact. Reload to see the current version.</p>
          <button onClick={() => window.location.reload()} className="btn btn-quiet mt-3 h-8 px-3 text-xs">
            Reload project
          </button>
        </div>
      )}

      {errCount > 0 && (
        <div role="alert" className="mt-4 rounded-md border border-bad/30 bg-bad-wash px-4 py-3 text-sm text-bad">
          {errCount === 1 ? "1 field needs attention." : `${errCount} fields need attention.`} Check the fields marked in red.
        </div>
      )}

      <div className="mt-6 space-y-6">
        <section className="panel p-5">
          <h2 className="text-[0.95rem] font-semibold">Project details</h2>
          <div className="mt-4 grid gap-4 sm:grid-cols-2">
            <div className="sm:col-span-2">
              <label htmlFor="name" className="field-label">Project name</label>
              <input id="name" className="input" value={draft.name} aria-invalid={!!errors.name}
                onChange={(e) => patch({ name: e.target.value })} placeholder="e.g. Warehouse fit-out, Unit 4" />
              {errors.name && <p className="field-error">{errors.name}</p>}
            </div>
            <div>
              <label htmlFor="client" className="field-label">Customer</label>
              <input id="client" className="input" value={draft.client} aria-invalid={!!errors.client}
                onChange={(e) => patch({ client: e.target.value, customerId: null })} />
              {errors.client && <p className="field-error">{errors.client}</p>}
            </div>
            <div>
              <label htmlFor="status" className="field-label">Status</label>
              <select id="status" className="input" value={draft.status}
                onChange={(e) => patch({ status: e.target.value as ProjectDraft["status"] })}>
                {(Object.keys(STATUS_LABEL) as (keyof typeof STATUS_LABEL)[]).map((s) => (
                  <option key={s} value={s}>{STATUS_LABEL[s]}</option>
                ))}
              </select>
            </div>
            <div className="sm:col-span-2">
              <label htmlFor="site" className="field-label">Site address</label>
              <input id="site" className="input" value={draft.siteAddress}
                onChange={(e) => patch({ siteAddress: e.target.value })} />
            </div>
          </div>
          <p className="mt-4 rounded-md bg-paper px-3 py-2.5 text-xs leading-relaxed text-ink-2">
            From Ticket&nbsp;6 a project is created by selecting an accepted QuickBooks estimate, and the customer and
            contract value come with it. These fields stay editable only until then.
          </p>
        </section>

        <Planned title="Financial summary" ticket={8}>
          Contract value, invoiced, remaining to invoice, collected, accounts receivable, deferred revenue and
          recognised revenue — all derived by the shared calculation service from linked QuickBooks records and
          shipment decisions. Never typed in.
        </Planned>

        <Planned title="Linked QuickBooks activity" ticket={9}>
          The accepted estimate, invoices and payments behind the totals, so every figure can be traced back to a
          source document.
        </Planned>

        <Planned title="Shipments" ticket={11}>
          Shipment date and status — the operational fact QuickBooks does not hold, and the trigger for revenue
          recognition. Partial shipments open an allocation workflow in Ticket&nbsp;13.
        </Planned>

        <Planned title="Open decisions" ticket={15}>
          Unresolved items for this project, shown here and in the Work Queue. Only things needing an action.
        </Planned>

        {project && <History entries={history} loading={historyLoading} />}
      </div>

      <Toast message={toast} onDone={clearToast} />
    </div>
  );
}
