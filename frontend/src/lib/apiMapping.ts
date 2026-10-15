// Translation between the API's wire shapes and the UI's model.
//
// TICKET 1. Reduced to project identity along with the rest of the reset. The
// line items, scalar cost inputs and results that used to be mapped here were part
// of the job-costing workflow.
//
// Ticket 8 adds the financial summary — contract value, invoiced, collected, A/R,
// deferred and recognised — once the calculation service exists to produce it.

import type { Project, ProjectStatus } from "./types";
import type { ProjectDraft } from "./projectStore";

export interface ApiProjectDetail {
  id: string;
  number: string;
  name: string;
  customerId: string;
  customerName: string;
  siteAddress: string;
  status: ProjectStatus;
  createdAt: string;
  updatedAt: string;
  etag: string;
}

export interface ApiProjectSummary {
  id: string;
  number: string;
  name: string;
  customerName: string;
  status: ProjectStatus;
  updatedAt: string;
}

export interface ApiPage<T> {
  items: T[];
  nextCursor: string | null;
}

export interface ApiFieldChange {
  field: string;
  from: string | null;
  to: string | null;
}

export interface ApiChangeSet {
  id: string;
  by: string;
  action: "created" | "updated" | "calculated";
  at: string;
  changes: ApiFieldChange[];
}

/* ---------- API -> UI ---------- */

export function toProject(dto: ApiProjectDetail): Project {
  return {
    id: dto.id,
    number: dto.number,
    name: dto.name,
    client: dto.customerName,
    customerId: dto.customerId,
    siteAddress: dto.siteAddress,
    status: dto.status,
    etag: dto.etag,
    history: [],
    createdAt: dto.createdAt,
    updatedAt: dto.updatedAt,
  };
}

export function summaryToProject(s: ApiProjectSummary): Project {
  return {
    id: s.id,
    number: s.number,
    name: s.name,
    client: s.customerName,
    customerId: null,
    siteAddress: "",
    status: s.status,
    etag: null,
    history: [],
    createdAt: s.updatedAt,
    updatedAt: s.updatedAt,
  };
}

/* ---------- UI -> API ---------- */

export function toSaveRequest(draft: ProjectDraft, number?: string | null) {
  return {
    number: number ?? undefined,
    name: draft.name,
    customerId: draft.customerId ?? undefined,
    customerName: draft.customerId ? undefined : draft.client,
    siteAddress: draft.siteAddress,
    status: draft.status,
  };
}

/** Maps API field keys back to form field names, so a server message lands on the right input. */
export function toFormField(apiField: string): string {
  const direct: Record<string, string> = {
    name: "name",
    customerName: "client",
    siteAddress: "siteAddress",
    status: "status",
  };
  return direct[apiField] ?? apiField;
}
