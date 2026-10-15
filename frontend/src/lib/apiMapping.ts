// Translation between the API's wire shapes and the UI's model.
//
// The two vocabularies differ deliberately: the API speaks of "lines", "customer"
// and "overheadPercent"; the screens speak of "products", "client" and "overheadPct".
// Rather than let both leak into every component, every conversion happens here, so
// there is exactly one file to change when Samuel's real SPFM field list arrives.

import type { CalculationState, ProductLine, Project, ProjectStatus, SpfmResults } from "./types";
import type { ProjectDraft } from "./projectStore";

/* ---------- wire shapes (exactly what the API sends) ---------- */

export interface ApiLine {
  description: string;
  quantity: number;
  unitCost: number;
  unitPrice: number;
}

export interface ApiResults {
  lineRevenue: number;
  variations: number;
  revenue: number;
  lineCost: number;
  labourCost: number;
  subcontractorCost: number;
  otherDirectCost: number;
  directCost: number;
  grossProfit: number;
  grossMarginPercent: number | null;
  overhead: number;
  netProfit: number;
  netMarginPercent: number | null;
  markupPercent: number | null;
}

export interface ApiProjectDetail {
  id: string;
  number: string;
  name: string;
  customerId: string;
  customerName: string;
  siteAddress: string;
  status: ProjectStatus;
  lines: ApiLine[];
  variations: number;
  labourHours: number;
  labourRate: number;
  subcontractorCost: number;
  otherDirectCost: number;
  overheadPercent: number;
  results: ApiResults | null;
  calculationState: CalculationState;
  calculatedAt: string | null;
  engineVersion: string | null;
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
  revenue: number | null;
  netProfit: number | null;
  calculationState: CalculationState;
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

export function toResults(r: ApiResults | null): SpfmResults | null {
  if (!r) return null;
  return {
    productRevenue: r.lineRevenue,
    variations: r.variations,
    revenue: r.revenue,
    productCost: r.lineCost,
    labourCost: r.labourCost,
    subcontractorCost: r.subcontractorCost,
    otherDirectCost: r.otherDirectCost,
    directCost: r.directCost,
    grossProfit: r.grossProfit,
    grossMarginPct: r.grossMarginPercent,
    overhead: r.overhead,
    netProfit: r.netProfit,
    netMarginPct: r.netMarginPercent,
    markupPct: r.markupPercent,
  };
}

export function toProject(dto: ApiProjectDetail): Project {
  return {
    id: dto.id,
    number: dto.number,
    name: dto.name,
    client: dto.customerName,
    customerId: dto.customerId,
    siteAddress: dto.siteAddress,
    status: dto.status,
    inputs: {
      // Line ids are generated here: they exist only so React can key the rows.
      // The API orders lines explicitly and does not need them.
      products: dto.lines.map((l) => ({
        id: crypto.randomUUID(),
        description: l.description,
        qty: l.quantity,
        unitCost: l.unitCost,
        unitPrice: l.unitPrice,
        qbItemId: null,
      })),
      variations: dto.variations,
      labourHours: dto.labourHours,
      labourRate: dto.labourRate,
      subcontractorCost: dto.subcontractorCost,
      otherDirectCost: dto.otherDirectCost,
      overheadPct: dto.overheadPercent,
    },
    results: toResults(dto.results),
    calculationState: dto.calculationState,
    calculatedFor: null, // the server decides freshness; the browser does not guess
    calculatedAt: dto.calculatedAt,
    engineVersion: dto.engineVersion,
    etag: dto.etag,
    quickbooks: { customerId: null, projectId: null, lastSyncedAt: null },
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
    inputs: {
      products: [],
      variations: 0,
      labourHours: 0,
      labourRate: 0,
      subcontractorCost: 0,
      otherDirectCost: 0,
      overheadPct: 0,
    },
    // The list only needs the headline figures; the rest arrives when a project is
    // opened. Sending full results for every row would be wasted bytes.
    results: s.revenue === null && s.netProfit === null
      ? null
      : ({ revenue: s.revenue ?? 0, netProfit: s.netProfit ?? 0 } as SpfmResults),
    calculationState: s.calculationState,
    calculatedFor: null,
    calculatedAt: null,
    engineVersion: null,
    etag: null,
    quickbooks: { customerId: null, projectId: null, lastSyncedAt: null },
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
    lines: draft.inputs.products.map((p: ProductLine) => ({
      description: p.description,
      quantity: p.qty,
      unitCost: p.unitCost,
      unitPrice: p.unitPrice,
    })),
    variations: draft.inputs.variations,
    labourHours: draft.inputs.labourHours,
    labourRate: draft.inputs.labourRate,
    subcontractorCost: draft.inputs.subcontractorCost,
    otherDirectCost: draft.inputs.otherDirectCost,
    overheadPercent: draft.inputs.overheadPct,
  };
}

/**
 * Maps the API's field keys back to the form's names, so a server validation
 * message lands on the right input.
 */
export function toFormField(apiField: string): string {
  const direct: Record<string, string> = {
    name: "name",
    customerName: "client",
    overheadPercent: "overheadPct",
    variations: "variations",
    labourHours: "labourHours",
    labourRate: "labourRate",
    subcontractorCost: "subcontractorCost",
    otherDirectCost: "otherDirectCost",
    lines: "products",
  };
  if (direct[apiField]) return direct[apiField];

  // lines.2.unitCost -> products.2.unitCost
  const line = apiField.match(/^lines\.(\d+)\.(\w+)$/);
  if (line) {
    const [, index, field] = line;
    const fieldMap: Record<string, string> = { quantity: "qty", description: "description", unitCost: "unitCost", unitPrice: "unitPrice" };
    return `products.${index}.${fieldMap[field] ?? field}`;
  }

  return apiField;
}
