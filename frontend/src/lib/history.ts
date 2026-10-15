// Change history: works out what changed between two versions of a project,
// in words a person can read ("Project name: Unit 4 → Unit 7").
//
// TICKET 1. Only project identity is compared now. The financial fields that used
// to be diffed here are no longer entered by hand — they will be derived from
// QuickBooks records and shipment decisions, and their history belongs with the
// RecognitionDecision and ManualOverride records in Ticket 2.
//
// In API mode the backend records history; this covers preview mode.

import type { FieldChange, Project, ProjectStatus } from "./types";
import { STATUS_LABEL } from "./types";

export type Comparable = Pick<Project, "name" | "client" | "siteAddress" | "status">;

const text = (s: string) => (s.trim() ? s.trim() : "blank");

const FIELDS: { key: keyof Comparable; label: string; fmt: (v: string) => string }[] = [
  { key: "name", label: "Project name", fmt: text },
  { key: "client", label: "Customer", fmt: text },
  { key: "siteAddress", label: "Site address", fmt: text },
  { key: "status", label: "Status", fmt: (v) => STATUS_LABEL[v as ProjectStatus] ?? v },
];

export function diffProjects(before: Comparable, after: Comparable): FieldChange[] {
  const changes: FieldChange[] = [];

  for (const { key, label, fmt } of FIELDS) {
    const a = String(before[key] ?? "").trim();
    const b = String(after[key] ?? "").trim();
    if (a !== b) changes.push({ field: label, from: fmt(a), to: fmt(b) });
  }

  return changes;
}
