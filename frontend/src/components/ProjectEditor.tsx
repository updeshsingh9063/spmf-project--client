"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { CostBar } from "@/components/CostBar";
import { CalcBadge, Icon, Toast } from "@/components/ui";
import { useAuth } from "@/lib/auth";
import { fmtDate, fmtDateTime, fmtMoney, fmtMoneyCents, fmtPct } from "@/lib/format";
import { createProject, emptyInputs, updateProject, type ProjectDraft } from "@/lib/projectStore";
import { calcState, calculate, fingerprint, validate, type FieldErrors } from "@/lib/spfm";
import type { HistoryEntry, Project, ProjectInputs, ProductLine, SpfmResults } from "@/lib/types";

const blankDraft = (): ProjectDraft => ({
  name: "",
  client: "",
  siteAddress: "",
  status: "quote",
  inputs: emptyInputs(),
  results: null,
  calculatedFor: null,
  calculatedAt: null,
});

const toDraft = (p: Project): ProjectDraft => ({
  name: p.name,
  client: p.client,
  siteAddress: p.siteAddress,
  status: p.status,
  inputs: p.inputs,
  results: p.results,
  calculatedFor: p.calculatedFor,
  calculatedAt: p.calculatedAt,
});

const numValue = (n: number) => (Number.isFinite(n) ? n : "");

function NumField({
  id, label, value, onChange, error, suffix, step = "any",
}: {
  id: string; label: string; value: number; onChange: (n: number) => void; error?: string; suffix?: string; step?: string;
}) {
  return (
    <div>
      <label htmlFor={id} className="field-label">{label}</label>
      <div className="relative">
        <input
          id={id}
          type="number"
          inputMode="decimal"
          step={step}
          min={0}
          className={`input input-num ${suffix ? "pr-8" : ""}`}
          value={numValue(value)}
          aria-invalid={!!error}
          aria-describedby={error ? `${id}-err` : undefined}
          onChange={(e) => onChange(e.target.value === "" ? NaN : e.target.valueAsNumber)}
        />
        {suffix && <span className="pointer-events-none absolute top-1/2 right-3 -translate-y-1/2 text-sm text-muted">{suffix}</span>}
      </div>
      {error && <p id={`${id}-err`} className="field-error">{error}</p>}
    </div>
  );
}

function LedgerRow({ label, value, sub, strong, minus }: { label: string; value: number; sub?: string; strong?: boolean; minus?: boolean }) {
  return (
    <div className={`flex items-baseline justify-between gap-4 py-1.5 ${strong ? "font-semibold" : "text-ink-2"}`}>
      <span className="text-sm">{label}{sub && <span className="ml-1.5 text-xs font-normal text-muted">{sub}</span>}</span>
      <span className={`text-sm ${value < 0 && strong ? "text-bad" : ""}`}>{minus ? `${value > 0 ? "−" : ""}${fmtMoneyCents(Math.abs(value))}` : fmtMoneyCents(value)}</span>
    </div>
  );
}

function Results({ r, overheadPct }: { r: SpfmResults; overheadPct: number }) {
  return (
    <div>
      <LedgerRow label="Contract value" value={r.revenue} strong />
      <div className="pl-3">
        <LedgerRow label="Doors & hardware" value={r.productRevenue} />
        {r.variations > 0 && <LedgerRow label="Variations" value={r.variations} />}
      </div>
      <div className="mt-2 border-t border-line-soft pt-2">
        <LedgerRow label="Doors & hardware cost" value={r.productCost} minus />
        <LedgerRow label="Labour" value={r.labourCost} minus />
        <LedgerRow label="Subcontractors" value={r.subcontractorCost} minus />
        <LedgerRow label="Other direct" value={r.otherDirectCost} minus />
      </div>
      <div className="mt-2 border-t border-line pt-2">
        <LedgerRow label="Gross profit" value={r.grossProfit} sub={fmtPct(r.grossMarginPct)} strong />
        <LedgerRow label="Overhead" value={r.overhead} sub={`${Number.isFinite(overheadPct) ? overheadPct : 0}% of direct cost`} minus />
      </div>
      <div className="mt-3 rounded-md bg-paper px-4 py-3">
        <div className="flex items-end justify-between gap-3">
          <div>
            <p className="text-xs text-muted">Net profit</p>
            <p className={`display text-[1.9rem] leading-none ${r.netProfit < 0 ? "text-bad" : "text-ink"}`}>{fmtMoneyCents(r.netProfit)}</p>
          </div>
          <dl className="text-right text-xs text-ink-2">
            <div><dt className="inline">Net margin </dt><dd className="inline font-semibold text-ink">{fmtPct(r.netMarginPct)}</dd></div>
            <div><dt className="inline">Markup on cost </dt><dd className="inline font-semibold text-ink">{fmtPct(r.markupPct)}</dd></div>
          </dl>
        </div>
      </div>
      <div className="mt-5">
        <CostBar results={r} />
      </div>
    </div>
  );
}

function History({ entries }: { entries: HistoryEntry[] }) {
  const [all, setAll] = useState(false);
  const shown = all ? entries : entries.slice(0, 5);
  return (
    <section className="panel p-5" aria-labelledby="history-h">
      <h2 id="history-h" className="text-[0.95rem] font-semibold">Change history</h2>
      <p className="mt-0.5 text-xs text-muted">Who changed what, and when. Recorded each time the project is saved.</p>
      {entries.length === 0 ? (
        <p className="mt-4 text-sm text-ink-2">No changes yet. History starts when the project is first saved.</p>
      ) : (
        <ol className="mt-4 space-y-4 border-l border-line pl-4">
          {shown.map((h) => (
            <li key={h.id} className="relative">
              <span className="absolute top-1.5 -left-[21px] h-2 w-2 rounded-full bg-steel ring-2 ring-white" aria-hidden="true" />
              <p className="text-sm">
                <span className="font-medium">{h.by}</span>{" "}
                <span className="text-ink-2">{h.action === "created" ? "created the project" : `made ${h.changes.length} ${h.changes.length === 1 ? "change" : "changes"}`}</span>
              </p>
              <p className="text-xs text-muted">{fmtDateTime(h.at)}</p>
              {h.changes.length > 0 && (
                <ul className="mt-1.5 space-y-0.5 text-xs text-ink-2">
                  {h.changes.map((c, i) => (
                    <li key={i}>
                      <span className="text-ink">{c.field}:</span> {c.from} <span aria-label="changed to">→</span> {c.to}
                    </li>
                  ))}
                </ul>
              )}
            </li>
          ))}
        </ol>
      )}
      {entries.length > 5 && (
        <button onClick={() => setAll(!all)} className="mt-4 text-sm font-medium text-brass-deep hover:underline">
          {all ? "Show recent only" : `Show all ${entries.length} entries`}
        </button>
      )}
    </section>
  );
}

export function ProjectEditor({ project }: { project: Project | null }) {
  const router = useRouter();
  const { user } = useAuth();
  const by = user?.name ?? "Unknown user";
  const [history, setHistory] = useState<HistoryEntry[]>(project?.history ?? []);
  const [draft, setDraft] = useState<ProjectDraft>(() => (project ? toDraft(project) : blankDraft()));
  const [saved, setSaved] = useState<string>(() => JSON.stringify(project ? toDraft(project) : blankDraft()));
  const [showErrors, setShowErrors] = useState(false);
  const [toast, setToast] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const clearToast = useCallback(() => setToast(null), []);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    if (params.get("created")) {
      setToast("Project created");
      window.history.replaceState(null, "", window.location.pathname);
    }
  }, []);

  const errors: FieldErrors = useMemo(() => (showErrors ? validate(draft, draft.inputs) : {}), [showErrors, draft]);
  const dirty = JSON.stringify(draft) !== saved;
  const state = calcState(draft);

  useEffect(() => {
    if (!dirty) return;
    const warn = (e: BeforeUnloadEvent) => e.preventDefault();
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [dirty]);

  const setInputs = (patch: Partial<ProjectInputs>) => setDraft((d) => ({ ...d, inputs: { ...d.inputs, ...patch } }));
  const setLine = (i: number, patch: Partial<ProductLine>) =>
    setInputs({ products: draft.inputs.products.map((l, j) => (j === i ? { ...l, ...patch } : l)) });
  const addLine = () =>
    setInputs({ products: [...draft.inputs.products, { id: crypto.randomUUID(), description: "", qty: 1, unitCost: 0, unitPrice: 0 }] });
  const removeLine = (i: number) => setInputs({ products: draft.inputs.products.filter((_, j) => j !== i) });

  const runCalculate = () => {
    const e = validate(draft, draft.inputs);
    setShowErrors(true);
    if (Object.keys(e).length) {
      setToast(`Fix ${Object.keys(e).length} ${Object.keys(e).length === 1 ? "field" : "fields"} before calculating`);
      return;
    }
    setDraft((d) => ({
      ...d,
      results: calculate(d.inputs),
      calculatedFor: fingerprint(d.inputs),
      calculatedAt: new Date().toISOString(),
    }));
    setToast("Calculated");
  };

  const save = async () => {
    const e = validate(draft, draft.inputs);
    setShowErrors(true);
    if (Object.keys(e).length) {
      setToast(`Fix ${Object.keys(e).length} ${Object.keys(e).length === 1 ? "field" : "fields"} before saving`);
      return;
    }
    setSaving(true);
    try {
      if (project) {
        const p = await updateProject(project.id, draft, by);
        setHistory(p.history);
        setSaved(JSON.stringify(toDraft(p)));
        setToast(state === "current" ? "Project saved" : "Project saved. Results still need calculating");
      } else {
        const p = await createProject(draft, by);
        setSaved(JSON.stringify(draft));
        router.replace(`/projects/${p.id}?created=1`);
      }
    } catch (err) {
      setToast(err instanceof Error ? err.message : "Couldn't save the project. Try again.");
    } finally {
      setSaving(false);
    }
  };

  const errCount = Object.keys(errors).length;
  const lines = draft.inputs.products;

  return (
    <div className="mx-auto max-w-6xl">
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
          <button onClick={save} disabled={saving} className="btn btn-brass">
            <Icon name="save" /> {saving ? "Saving…" : project ? "Save project" : "Create project"}
          </button>
        </div>
      </header>

      {errCount > 0 && (
        <div role="alert" className="mt-4 rounded-md border border-bad/30 bg-bad-wash px-4 py-3 text-sm text-bad">
          {errCount === 1 ? "1 field needs attention." : `${errCount} fields need attention.`} Check the fields marked in red.
        </div>
      )}

      <div className="mt-6 grid grid-cols-1 items-start gap-6 lg:grid-cols-[minmax(0,1fr)_380px]">
        <div className="space-y-6">
          <section className="panel p-5">
            <h2 className="text-[0.95rem] font-semibold">Project details</h2>
            <div className="mt-4 grid gap-4 sm:grid-cols-2">
              <div className="sm:col-span-2">
                <label htmlFor="name" className="field-label">Project name</label>
                <input id="name" className="input" value={draft.name} aria-invalid={!!errors.name}
                  onChange={(e) => setDraft({ ...draft, name: e.target.value })} placeholder="e.g. Warehouse fit-out, Unit 4" />
                {errors.name && <p className="field-error">{errors.name}</p>}
              </div>
              <div>
                <label htmlFor="client" className="field-label">Client</label>
                <input id="client" className="input" value={draft.client} aria-invalid={!!errors.client}
                  onChange={(e) => setDraft({ ...draft, client: e.target.value })} />
                {errors.client && <p className="field-error">{errors.client}</p>}
              </div>
              <div>
                <label htmlFor="status" className="field-label">Status</label>
                <select id="status" className="input" value={draft.status}
                  onChange={(e) => setDraft({ ...draft, status: e.target.value as ProjectDraft["status"] })}>
                  <option value="quote">Quote</option>
                  <option value="active">Active</option>
                  <option value="complete">Complete</option>
                </select>
              </div>
              <div className="sm:col-span-2">
                <label htmlFor="site" className="field-label">Site address</label>
                <input id="site" className="input" value={draft.siteAddress}
                  onChange={(e) => setDraft({ ...draft, siteAddress: e.target.value })} />
              </div>
            </div>
            <div className="mt-4 flex flex-wrap items-center justify-between gap-2 rounded-md bg-paper px-3 py-2.5 text-xs text-ink-2">
              <span>
                <span className="font-medium text-ink">QuickBooks:</span>{" "}
                {project?.quickbooks.projectId ? `linked to project ${project.quickbooks.projectId}` : "not linked"}
              </span>
              <span className="text-muted">Automatic import from QuickBooks comes in Phase 2.</span>
            </div>
          </section>

          <section className="panel">
            <div className="flex items-center justify-between px-5 pt-5">
              <div>
                <h2 className="text-[0.95rem] font-semibold">Doors & hardware</h2>
                <p className="mt-0.5 text-xs text-muted">One line per product supplied on this job.</p>
              </div>
              <button onClick={addLine} className="btn btn-quiet h-8 px-3 text-xs">
                <Icon name="plus" size={14} /> Add line
              </button>
            </div>
            {errors.products && <p className="field-error px-5 pt-2">{errors.products}</p>}
            <div className="relative mt-4 overflow-x-auto">
              <table className="w-full min-w-[640px]">
                <thead className="table-head">
                  <tr>
                    <th className="w-[40%]">Item</th>
                    <th className="w-20 !text-right">Qty</th>
                    <th className="!text-right">Unit cost</th>
                    <th className="!text-right">Unit price</th>
                    <th className="whitespace-nowrap !text-right">Line total</th>
                    <th className="w-10"><span className="sr-only">Remove</span></th>
                  </tr>
                </thead>
                <tbody>
                  {lines.map((l, i) => (
                    <tr key={l.id} className="align-top">
                      <td className="px-3 py-2">
                        <input aria-label={`Item ${i + 1} description`} className="input" value={l.description}
                          aria-invalid={!!errors[`products.${i}.description`]}
                          onChange={(e) => setLine(i, { description: e.target.value })} placeholder="e.g. Steel security door, single" />
                        {errors[`products.${i}.description`] && <p className="field-error">{errors[`products.${i}.description`]}</p>}
                      </td>
                      {(["qty", "unitCost", "unitPrice"] as const).map((k) => (
                        <td key={k} className="px-2 py-2">
                          <input aria-label={`Item ${i + 1} ${k === "qty" ? "quantity" : k === "unitCost" ? "unit cost" : "unit price"}`}
                            type="number" inputMode="decimal" min={0} step={k === "qty" ? 1 : "any"}
                            className="input input-num" value={numValue(l[k])}
                            aria-invalid={!!errors[`products.${i}.${k}`]}
                            onChange={(e) => setLine(i, { [k]: e.target.value === "" ? NaN : e.target.valueAsNumber })} />
                          {errors[`products.${i}.${k}`] && <p className="field-error text-right">{errors[`products.${i}.${k}`]}</p>}
                        </td>
                      ))}
                      <td className="px-3 py-2 text-right text-sm leading-9 text-ink-2">
                        {Number.isFinite(l.qty * l.unitPrice) ? fmtMoney(l.qty * l.unitPrice) : "–"}
                      </td>
                      <td className="py-2 pr-3">
                        <button onClick={() => removeLine(i)} className="grid h-9 w-8 place-items-center rounded text-muted hover:bg-bad-wash hover:text-bad"
                          aria-label={`Remove item ${i + 1}`}>
                          <Icon name="trash" />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {lines.length === 0 && (
              <p className="px-5 pb-5 text-sm text-ink-2">No lines yet. Add the doors and hardware supplied on this job.</p>
            )}
            <div className="h-3" />
          </section>

          <section className="panel p-5">
            <h2 className="text-[0.95rem] font-semibold">Labour & other costs</h2>
            <div className="mt-4 grid gap-4 sm:grid-cols-3">
              <NumField id="labourHours" label="Labour hours" value={draft.inputs.labourHours} error={errors.labourHours} onChange={(n) => setInputs({ labourHours: n })} />
              <NumField id="labourRate" label="Labour rate per hour" value={draft.inputs.labourRate} error={errors.labourRate} onChange={(n) => setInputs({ labourRate: n })} />
              <NumField id="subcontractorCost" label="Subcontractors" value={draft.inputs.subcontractorCost} error={errors.subcontractorCost} onChange={(n) => setInputs({ subcontractorCost: n })} />
              <NumField id="otherDirectCost" label="Other direct costs" value={draft.inputs.otherDirectCost} error={errors.otherDirectCost} onChange={(n) => setInputs({ otherDirectCost: n })} />
              <NumField id="variations" label="Variations billed" value={draft.inputs.variations} error={errors.variations} onChange={(n) => setInputs({ variations: n })} />
              <NumField id="overheadPct" label="Overhead" suffix="%" value={draft.inputs.overheadPct} error={errors.overheadPct} onChange={(n) => setInputs({ overheadPct: n })} />
            </div>
            <p className="mt-4 text-xs text-muted">
              Placeholder fields. These will be replaced with the confirmed SPFM field list.
            </p>
          </section>

          {project && <History entries={history} />}
        </div>

        <aside className="panel lg:sticky lg:top-6">
          <div className="flex items-center justify-between border-b border-line-soft px-5 py-4">
            <div>
              <h2 className="text-[0.95rem] font-semibold">SPFM results</h2>
              <CalcBadge state={state} />
            </div>
            <button onClick={runCalculate} className="btn btn-primary">
              <Icon name="calc" /> {state === "never" ? "Calculate" : "Recalculate"}
            </button>
          </div>
          <div className="px-5 py-4">
            {state === "outdated" && (
              <p className="mb-3 rounded-md bg-warn-wash px-3 py-2 text-xs text-warn">
                Inputs have changed since these figures were calculated. Recalculate to update them.
              </p>
            )}
            {draft.results ? (
              <div className={state === "outdated" ? "opacity-55" : ""}>
                <Results r={draft.results} overheadPct={draft.inputs.overheadPct} />
              </div>
            ) : (
              <div className="py-8 text-center">
                <p className="text-sm font-medium">No results yet</p>
                <p className="mx-auto mt-1 max-w-[16rem] text-sm text-ink-2">
                  Enter the job figures, then select Calculate to see costs and margins.
                </p>
              </div>
            )}
            {draft.calculatedAt && (
              <p className="mt-4 text-xs text-muted">Last calculated {fmtDate(draft.calculatedAt)}</p>
            )}
          </div>
        </aside>
      </div>
      <Toast message={toast} onDone={clearToast} />
    </div>
  );
}
