import { describe, expect, it } from "vitest";
import { calcState, calculate, fingerprint, validate } from "./spfm";
import { TEST_CASES } from "./testCases";
import type { ProjectInputs } from "./types";

describe("named test cases (src/lib/testCases.ts)", () => {
  it.each(TEST_CASES.map((c) => [`${c.id} ${c.name}`, c] as const))("%s", (_, c) => {
    expect(calculate(c.inputs)).toMatchObject(c.expected);
  });
});

const warehouseFitOut: ProjectInputs = {
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
};

describe("calculate", () => {
  it("warehouse fit-out: every result field", () => {
    const r = calculate(warehouseFitOut);
    expect(r.productRevenue).toBe(11800);
    expect(r.revenue).toBe(12300);
    expect(r.productCost).toBe(7480);
    expect(r.labourCost).toBe(1760);
    expect(r.directCost).toBe(10380);
    expect(r.grossProfit).toBe(1920);
    expect(r.grossMarginPct).toBe(15.61);
    expect(r.overhead).toBe(1245.6);
    expect(r.netProfit).toBe(674.4);
    expect(r.netMarginPct).toBe(5.48);
    expect(r.markupPct).toBe(18.5);
  });

  it("returns null margins when there is no revenue", () => {
    const r = calculate({ ...warehouseFitOut, products: [], variations: 0 });
    expect(r.revenue).toBe(0);
    expect(r.grossMarginPct).toBeNull();
    expect(r.netMarginPct).toBeNull();
  });

  it("rounds to cents", () => {
    const r = calculate({
      ...warehouseFitOut,
      products: [{ id: "x", description: "Hinge kit", qty: 3, unitCost: 0.1, unitPrice: 0.2 }],
      variations: 0, labourHours: 0, subcontractorCost: 0, otherDirectCost: 0, overheadPct: 0,
    });
    expect(r.productCost).toBe(0.3);
    expect(r.grossProfit).toBe(0.3);
  });
});

describe("calcState", () => {
  it("tracks whether results match the current inputs", () => {
    const results = calculate(warehouseFitOut);
    const calculatedFor = fingerprint(warehouseFitOut);
    expect(calcState({ inputs: warehouseFitOut, results: null, calculatedFor: null })).toBe("never");
    expect(calcState({ inputs: warehouseFitOut, results, calculatedFor })).toBe("current");
    const edited = { ...warehouseFitOut, labourHours: 40 };
    expect(calcState({ inputs: edited, results, calculatedFor })).toBe("outdated");
  });
});

describe("validate", () => {
  it("accepts a complete project", () => {
    expect(validate({ name: "Fit-out", client: "Acme" }, warehouseFitOut)).toEqual({});
  });

  it("flags missing details and bad lines", () => {
    const e = validate(
      { name: " ", client: "" },
      {
        ...warehouseFitOut,
        products: [{ id: "a", description: "", qty: 1.5, unitCost: -1, unitPrice: 10 }],
        overheadPct: 140,
      },
    );
    expect(Object.keys(e).sort()).toEqual(
      ["client", "name", "overheadPct", "products.0.description", "products.0.qty", "products.0.unitCost"].sort(),
    );
  });

  it("requires at least one product line", () => {
    expect(validate({ name: "A", client: "B" }, { ...warehouseFitOut, products: [] }).products).toBeDefined();
  });
});
