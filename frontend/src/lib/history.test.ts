import { describe, expect, it } from "vitest";
import { diffProjects, type Comparable } from "./history";
import { calculate } from "./spfm";

const base: Comparable = {
  name: "Clinic front entrance",
  client: "Northside Medical",
  siteAddress: "118 King Street",
  status: "active",
  inputs: {
    products: [{ id: "a", description: "Screen door", qty: 1, unitCost: 1450, unitPrice: 2480 }],
    variations: 0,
    labourHours: 12,
    labourRate: 55,
    subcontractorCost: 350,
    otherDirectCost: 90,
    overheadPct: 12,
  },
  results: null,
};

describe("diffProjects", () => {
  it("returns nothing when nothing changed", () => {
    expect(diffProjects(base, structuredClone(base))).toEqual([]);
  });

  it("describes detail, input and line changes", () => {
    const after = structuredClone(base);
    after.status = "complete";
    after.inputs.labourHours = 16;
    after.inputs.products[0].qty = 2;
    after.inputs.products.push({ id: "b", description: "Intercom kit", qty: 1, unitCost: 610, unitPrice: 980 });
    const c = diffProjects(base, after);
    expect(c).toContainEqual({ field: "Status", from: "Active", to: "Complete" });
    expect(c).toContainEqual({ field: "Labour hours", from: "12", to: "16" });
    expect(c).toContainEqual({ field: "“Screen door” qty", from: "1", to: "2" });
    expect(c.find((x) => x.field === "Line added")?.to).toContain("Intercom kit");
  });

  it("records removed lines and recalculated net profit", () => {
    const after = structuredClone(base);
    after.inputs.products = [];
    after.results = calculate(after.inputs);
    const c = diffProjects(base, after);
    expect(c.find((x) => x.field === "Line removed")?.from).toContain("Screen door");
    expect(c.find((x) => x.field === "Net profit (calculated)")?.from).toBe("not calculated");
  });
});
