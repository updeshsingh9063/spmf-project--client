// SPFM data model (Alpha, lean).
//
// PLACEHOLDER FIELDS: these inputs are a working stand-in until Samuel's
// SPFM field list and calculation rules arrive. The shape is deliberately
// simple so it can be swapped for the real field list without touching
// the screens' structure.
//
// QUICKBOOKS-READY: projects and lines carry optional QuickBooks Online
// references (see docs/QUICKBOOKS-MAPPING.md). They stay empty in the Alpha
// and are filled by the Phase 2 import.

export type ProjectStatus = "quote" | "active" | "complete" | "archived";

/**
 * Whether saved results still match the project's current inputs.
 * In API mode the server decides this; the browser never guesses.
 */
export type CalculationState = "never" | "outdated" | "current";

export interface ProductLine {
  id: string;
  description: string;
  qty: number;
  unitCost: number; // what Shield pays per unit
  unitPrice: number; // what the client is charged per unit
  qbItemId?: string | null; // QuickBooks "Products and services" item
}

export interface ProjectInputs {
  products: ProductLine[];
  variations: number; // approved variations / extras billed to the client
  labourHours: number;
  labourRate: number; // cost per labour hour
  subcontractorCost: number;
  otherDirectCost: number; // freight, consumables, permits
  overheadPct: number; // overhead recovery, % of direct cost
}

export interface SpfmResults {
  productRevenue: number;
  variations: number;
  revenue: number;
  productCost: number;
  labourCost: number;
  subcontractorCost: number;
  otherDirectCost: number;
  directCost: number;
  grossProfit: number;
  grossMarginPct: number | null;
  overhead: number;
  netProfit: number;
  netMarginPct: number | null;
  markupPct: number | null;
}

/** QuickBooks Online references. A QBO "Project" is a sub-customer of a Customer. */
export interface QuickBooksRefs {
  customerId: string | null;
  projectId: string | null;
  lastSyncedAt: string | null;
}

export interface FieldChange {
  field: string; // readable label, e.g. "Labour hours"
  from: string;
  to: string;
}

export interface HistoryEntry {
  id: string;
  at: string;
  by: string; // display name of the signed-in user
  action: "created" | "updated";
  changes: FieldChange[];
}

export interface Project {
  id: string;
  number: string; // e.g. SSD-2026-014
  name: string;
  client: string;
  /** Set in API mode once the customer exists server-side. */
  customerId?: string | null;
  siteAddress: string;
  status: ProjectStatus;
  inputs: ProjectInputs;
  results: SpfmResults | null;
  /** Server-reported freshness. Takes precedence over the local fingerprint. */
  calculationState?: CalculationState;
  calculatedFor: string | null; // local-mode fingerprint of the inputs behind the results
  calculatedAt: string | null;
  /** Which formula set produced the stored results. */
  engineVersion?: string | null;
  /** Concurrency token, echoed back as If-Match when saving. */
  etag?: string | null;
  quickbooks: QuickBooksRefs;
  history: HistoryEntry[];
  createdAt: string;
  updatedAt: string;
}

export const STATUS_LABEL: Record<ProjectStatus, string> = {
  quote: "Quote",
  active: "Active",
  complete: "Complete",
  archived: "Archived",
};
