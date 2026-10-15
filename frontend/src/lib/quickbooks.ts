// QuickBooks Online mapping, ready for the Phase 2 import (not used in the Alpha).
// See docs/QUICKBOOKS-MAPPING.md for the full field mapping.

import type { SpfmResults } from "./types";

/** Which QuickBooks account type each SPFM cost line is expected to come from. */
export const COST_SOURCE: Record<
  keyof Pick<SpfmResults, "productCost" | "labourCost" | "subcontractorCost" | "otherDirectCost">,
  { qbAccountType: string; note: string }
> = {
  productCost: { qbAccountType: "Cost of Goods Sold", note: "Bills / purchases of doors and hardware, by item" },
  labourCost: { qbAccountType: "Cost of Goods Sold", note: "Time activities or payroll allocated to the project" },
  subcontractorCost: { qbAccountType: "Cost of Goods Sold", note: "Bills from subcontractor vendors tagged to the project" },
  otherDirectCost: { qbAccountType: "Cost of Goods Sold / Expense", note: "Freight, consumables and permits tagged to the project" },
};

/** Revenue side: invoice lines on the project (sub-customer). */
export const REVENUE_SOURCE = {
  productRevenue: "Invoice lines for Products and services items",
  variations: "Invoice lines using a Variations service item",
} as const;
