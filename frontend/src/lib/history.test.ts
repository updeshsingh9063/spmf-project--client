import { describe, expect, it } from "vitest";
import { diffProjects, type Comparable } from "./history";

/**
 * Preview-mode change history. The API records its own in the real workflow, but the
 * labels have to match: history is read by Samuel, not by a developer.
 */
const base = (): Comparable => ({
  name: "Warehouse fit-out, Unit 4",
  client: "Harbour Logistics",
  siteAddress: "4 Dockside Road",
  status: "active",
});

describe("diffProjects", () => {
  it("records nothing when nothing changed", () => {
    expect(diffProjects(base(), base())).toEqual([]);
  });

  it("records the field with its old and new value", () => {
    const changes = diffProjects(base(), { ...base(), name: "Warehouse fit-out, Unit 7" });

    expect(changes).toHaveLength(1);
    expect(changes[0]).toEqual({
      field: "Project name",
      from: "Warehouse fit-out, Unit 4",
      to: "Warehouse fit-out, Unit 7",
    });
  });

  it("labels fields the way the form does, not the way the code does", () => {
    const changes = diffProjects(base(), { ...base(), client: "Northside Medical", siteAddress: "22 Pine Street" });

    expect(changes.map((c) => c.field)).toEqual(["Customer", "Site address"]);
  });

  it("shows status as a word, not its stored value", () => {
    const changes = diffProjects(base(), { ...base(), status: "complete" });

    expect(changes[0]).toEqual({ field: "Status", from: "Active", to: "Complete" });
  });

  it("treats surrounding whitespace as no change", () => {
    expect(diffProjects(base(), { ...base(), name: "  Warehouse fit-out, Unit 4  " })).toEqual([]);
  });

  it("says 'blank' rather than showing an empty gap", () => {
    const changes = diffProjects(base(), { ...base(), siteAddress: "" });

    expect(changes[0].to).toBe("blank");
  });

  it("records every field changed in one save", () => {
    const changes = diffProjects(base(), {
      name: "Renamed",
      client: "New customer",
      siteAddress: "Elsewhere",
      status: "complete",
    });

    expect(changes).toHaveLength(4);
  });
});
