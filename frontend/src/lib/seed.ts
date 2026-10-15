// Sample projects for the preview. Fictional figures, not Shield data.
import { calculate, fingerprint } from "./spfm";
import type { Project, ProjectInputs, ProjectStatus } from "./types";

interface SeedRow {
  number: string;
  name: string;
  client: string;
  siteAddress: string;
  status: ProjectStatus;
  daysAgo: number;
  calc: "current" | "outdated" | "never";
  inputs: ProjectInputs;
}

const SEED_USER = "Samuel Fish";
let n = 0;
const line = (description: string, qty: number, unitCost: number, unitPrice: number) => ({
  id: `seed-${++n}`,
  description,
  qty,
  unitCost,
  unitPrice,
});

const rows: SeedRow[] = [
  {
    number: "SSD-2026-014",
    name: "Warehouse fit-out, Unit 4",
    client: "Harbour Logistics",
    siteAddress: "4 Dockside Road",
    status: "active",
    daysAgo: 1,
    calc: "current",
    inputs: {
      products: [
        line("Steel security door, single", 4, 820, 1350),
        line("Roller shutter 3m", 2, 2100, 3200),
      ],
      variations: 500,
      labourHours: 32,
      labourRate: 55,
      subcontractorCost: 900,
      otherDirectCost: 240,
      overheadPct: 12,
    },
  },
  {
    number: "SSD-2026-013",
    name: "Clinic front entrance",
    client: "Northside Medical",
    siteAddress: "118 King Street",
    status: "active",
    daysAgo: 3,
    calc: "outdated",
    inputs: {
      products: [
        line("Security screen door, double", 1, 1450, 2480),
        line("Electric strike + intercom kit", 1, 610, 980),
      ],
      variations: 0,
      labourHours: 12,
      labourRate: 55,
      subcontractorCost: 350,
      otherDirectCost: 90,
      overheadPct: 12,
    },
  },
  {
    number: "SSD-2026-012",
    name: "Apartment block, 12 entries",
    client: "Westgate Property Group",
    siteAddress: "22 Elm Avenue",
    status: "quote",
    daysAgo: 6,
    calc: "never",
    inputs: {
      products: [line("Security screen door, single", 12, 540, 890)],
      variations: 0,
      labourHours: 40,
      labourRate: 55,
      subcontractorCost: 0,
      otherDirectCost: 300,
      overheadPct: 12,
    },
  },
  {
    number: "SSD-2026-011",
    name: "Retail store shutters",
    client: "Coastline Outfitters",
    siteAddress: "Shop 7, Bay Mall",
    status: "complete",
    daysAgo: 14,
    calc: "current",
    inputs: {
      products: [
        line("Roller shutter 4m, powder coated", 2, 2650, 4100),
        line("Motor + remote upgrade", 2, 380, 690),
      ],
      variations: 750,
      labourHours: 18,
      labourRate: 55,
      subcontractorCost: 1200,
      otherDirectCost: 160,
      overheadPct: 12,
    },
  },
  {
    number: "SSD-2026-010",
    name: "Residential rear door",
    client: "J. Patel",
    siteAddress: "9 Linden Court",
    status: "complete",
    daysAgo: 21,
    calc: "current",
    inputs: {
      products: [line("Steel security door, single", 1, 820, 1290)],
      variations: 0,
      labourHours: 4,
      labourRate: 55,
      subcontractorCost: 0,
      otherDirectCost: 45,
      overheadPct: 12,
    },
  },
];

export function seedProjects(): Project[] {
  const now = Date.now();
  return rows.map((r, i) => {
    const at = new Date(now - r.daysAgo * 86_400_000).toISOString();
    const created = new Date(now - (r.daysAgo + 2) * 86_400_000).toISOString();
    const calculatedInputs =
      r.calc === "outdated" ? { ...r.inputs, labourHours: r.inputs.labourHours - 4 } : r.inputs;
    return {
      id: `seed-project-${i + 1}`,
      number: r.number,
      name: r.name,
      client: r.client,
      siteAddress: r.siteAddress,
      status: r.status,
      inputs: r.inputs,
      results: r.calc === "never" ? null : calculate(calculatedInputs),
      calculatedFor: r.calc === "never" ? null : fingerprint(calculatedInputs),
      calculatedAt: r.calc === "never" ? null : at,
      quickbooks: { customerId: null, projectId: null, lastSyncedAt: null },
      history: [
        ...(r.calc === "outdated"
          ? [{
              id: `seed-h-${i}-2`,
              at,
              by: SEED_USER,
              action: "updated" as const,
              changes: [{ field: "Labour hours", from: String(calculatedInputs.labourHours), to: String(r.inputs.labourHours) }],
            }]
          : []),
        { id: `seed-h-${i}-1`, at: created, by: SEED_USER, action: "created" as const, changes: [] },
      ],
      createdAt: created,
      updatedAt: at,
    };
  });
}
