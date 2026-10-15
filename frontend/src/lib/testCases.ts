// Named SPFM test cases: inputs plus the results we expect.
//
// TO DO WITH SAMUEL: replace the placeholder cases with 2–3 real Shield
// projects and the results he already knows are correct. `npm test` checks
// every case listed here, and the same list is handed over for future use.

import type { ProjectInputs, SpfmResults } from "./types";

export interface NamedTestCase {
  id: string;
  name: string;
  source: "placeholder" | "shield-real-project";
  inputs: ProjectInputs;
  expected: Partial<SpfmResults>;
}

export const TEST_CASES: NamedTestCase[] = [
  {
    id: "TC-01",
    name: "Warehouse fit-out: doors, shutters and variation",
    source: "placeholder",
    inputs: {
      products: [
        { id: "a", description: "Steel security door, single", qty: 4, unitCost: 820, unitPrice: 1350 },
        { id: "b", description: "Roller shutter 3m", qty: 2, unitCost: 2100, unitPrice: 3200 },
      ],
      variations: 500,
      labourHours: 32,
      labourRate: 55,
      subcontractorCost: 900,
      otherDirectCost: 240,
      overheadPct: 12,
    },
    expected: {
      productRevenue: 11800,
      revenue: 12300,
      productCost: 7480,
      labourCost: 1760,
      directCost: 10380,
      grossProfit: 1920,
      grossMarginPct: 15.61,
      overhead: 1245.6,
      netProfit: 674.4,
      netMarginPct: 5.48,
      markupPct: 18.5,
    },
  },
  {
    id: "TC-02",
    name: "Clinic entrance: job that loses money after overhead",
    source: "placeholder",
    inputs: {
      products: [
        { id: "a", description: "Security screen door, double", qty: 1, unitCost: 1450, unitPrice: 2480 },
        { id: "b", description: "Electric strike + intercom kit", qty: 1, unitCost: 610, unitPrice: 980 },
      ],
      variations: 0,
      labourHours: 12,
      labourRate: 55,
      subcontractorCost: 350,
      otherDirectCost: 90,
      overheadPct: 12,
    },
    expected: {
      revenue: 3460,
      directCost: 3160,
      grossProfit: 300,
      grossMarginPct: 8.67,
      overhead: 379.2,
      netProfit: -79.2,
      netMarginPct: -2.29,
    },
  },
  {
    id: "TC-03",
    name: "Single residential door, no overhead",
    source: "placeholder",
    inputs: {
      products: [{ id: "a", description: "Steel security door, single", qty: 1, unitCost: 820, unitPrice: 1290 }],
      variations: 0,
      labourHours: 4,
      labourRate: 55,
      subcontractorCost: 0,
      otherDirectCost: 45,
      overheadPct: 0,
    },
    expected: {
      revenue: 1290,
      directCost: 1085,
      grossProfit: 205,
      overhead: 0,
      netProfit: 205,
      grossMarginPct: 15.89,
    },
  },
];
