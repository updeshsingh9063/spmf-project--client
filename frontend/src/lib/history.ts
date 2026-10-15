// Change history: works out what changed between two versions of a project,
// in words a person can read ("Labour hours: 12 → 16").
// In API mode the backend records history; it can use the same field labels.

import type { FieldChange, Project, ProjectInputs, ProductLine, ProjectStatus } from "./types";
import { STATUS_LABEL } from "./types";

export type Comparable = Pick<Project, "name" | "client" | "siteAddress" | "status" | "inputs" | "results">;

const num = (n: number) => (Number.isFinite(n) ? String(n) : "blank");
const money = (n: number) =>
  Number.isFinite(n) ? n.toLocaleString("en-US", { style: "currency", currency: "USD", minimumFractionDigits: 2 }) : "blank";
const text = (s: string) => (s.trim() ? s.trim() : "blank");

const INPUT_FIELDS: { key: Exclude<keyof ProjectInputs, "products">; label: string; fmt: (n: number) => string }[] = [
  { key: "labourHours", label: "Labour hours", fmt: num },
  { key: "labourRate", label: "Labour rate per hour", fmt: money },
  { key: "subcontractorCost", label: "Subcontractors", fmt: money },
  { key: "otherDirectCost", label: "Other direct costs", fmt: money },
  { key: "variations", label: "Variations billed", fmt: money },
  { key: "overheadPct", label: "Overhead %", fmt: num },
];

const LINE_FIELDS: { key: keyof Pick<ProductLine, "qty" | "unitCost" | "unitPrice">; label: string; fmt: (n: number) => string }[] = [
  { key: "qty", label: "qty", fmt: num },
  { key: "unitCost", label: "unit cost", fmt: money },
  { key: "unitPrice", label: "unit price", fmt: money },
];

const lineName = (l: ProductLine) => (l.description.trim() ? `“${l.description.trim()}”` : "unnamed line");
const lineSummary = (l: ProductLine) => `${num(l.qty)} × ${money(l.unitPrice)} (cost ${money(l.unitCost)})`;

export function diffProjects(before: Comparable, after: Comparable): FieldChange[] {
  const c: FieldChange[] = [];
  const push = (field: string, from: string, to: string) => {
    if (from !== to) c.push({ field, from, to });
  };

  push("Project name", text(before.name), text(after.name));
  push("Client", text(before.client), text(after.client));
  push("Site address", text(before.siteAddress), text(after.siteAddress));
  push("Status", STATUS_LABEL[before.status as ProjectStatus], STATUS_LABEL[after.status as ProjectStatus]);

  for (const f of INPUT_FIELDS) push(f.label, f.fmt(before.inputs[f.key]), f.fmt(after.inputs[f.key]));

  const prev = new Map(before.inputs.products.map((l) => [l.id, l]));
  const next = new Map(after.inputs.products.map((l) => [l.id, l]));
  for (const l of after.inputs.products) {
    const old = prev.get(l.id);
    if (!old) {
      c.push({ field: "Line added", from: "–", to: `${lineName(l)} ${lineSummary(l)}` });
      continue;
    }
    push("Line description", text(old.description), text(l.description));
    for (const f of LINE_FIELDS) push(`${lineName(l)} ${f.label}`, f.fmt(old[f.key]), f.fmt(l[f.key]));
  }
  for (const l of before.inputs.products) {
    if (!next.has(l.id)) c.push({ field: "Line removed", from: `${lineName(l)} ${lineSummary(l)}`, to: "–" });
  }

  const net = (p: Comparable) => (p.results ? money(p.results.netProfit) : "not calculated");
  push("Net profit (calculated)", net(before), net(after));
  return c;
}
