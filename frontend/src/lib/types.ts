// SPFM types — project identity only.
//
// TICKET 1. The job-costing model that lived here is gone: product lines, labour,
// subcontractors, overhead and the margin results. SPFM derives its financial
// figures from QuickBooks records and shipment decisions, so none of that belonged
// in a client-side type.
//
// Ticket 2 defines the real domain model server-side — Project, QuickBooksEntityLink,
// Shipment, ShipmentAllocation, RecognitionDecision, ManualOverride and
// AccountingPeriod. These types stay deliberately thin until then rather than
// guessing at shapes that document will settle.

export type ProjectStatus = "quote" | "active" | "complete" | "archived";

export const STATUS_LABEL: Record<ProjectStatus, string> = {
  quote: "Quote",
  active: "Active",
  complete: "Complete",
  archived: "Archived",
};

export interface FieldChange {
  /** Readable label, e.g. "Project name" — not the property name. */
  field: string;
  from: string;
  to: string;
}

export interface HistoryEntry {
  id: string;
  at: string;
  by: string;
  action: "created" | "updated";
  changes: FieldChange[];
}

export interface Project {
  id: string;
  /** Display number, e.g. SSD-2026-014. */
  number: string;
  name: string;
  /** Customer name. Becomes read-only once projects link to QuickBooks (Ticket 6). */
  client: string;
  customerId?: string | null;
  siteAddress: string;
  status: ProjectStatus;

  /** Concurrency token, echoed back as If-Match when saving. */
  etag?: string | null;

  history: HistoryEntry[];
  createdAt: string;
  updatedAt: string;
}
