// Sample projects for preview mode.
//
// TICKET 1. Reduced to project identity. The costed sample jobs that used to live
// here implied a job-costing product; from Ticket 6 projects are created by
// selecting an accepted QuickBooks estimate, and this file goes away.

import type { Project } from "./types";

const daysAgo = (n: number) => new Date(Date.now() - n * 86_400_000).toISOString();

const project = (
  id: string,
  number: string,
  name: string,
  client: string,
  siteAddress: string,
  status: Project["status"],
  updatedDaysAgo: number,
): Project => ({
  id,
  number,
  name,
  client,
  customerId: null,
  siteAddress,
  status,
  etag: null,
  history: [
    {
      id: `${id}-created`,
      at: daysAgo(updatedDaysAgo + 20),
      by: "Samuel Fish",
      action: "created",
      changes: [],
    },
  ],
  createdAt: daysAgo(updatedDaysAgo + 20),
  updatedAt: daysAgo(updatedDaysAgo),
});

export function seedProjects(): Project[] {
  return [
    project("a1b2c3d4-0001-4000-8000-000000000001", "SSD-2026-014", "Warehouse fit-out, Unit 4", "Harbour Logistics", "4 Dockside Road", "active", 1),
    project("a1b2c3d4-0002-4000-8000-000000000002", "SSD-2026-013", "Clinic front entrance", "Northside Medical", "22 Pine Street", "active", 3),
    project("a1b2c3d4-0003-4000-8000-000000000003", "SSD-2026-012", "Apartment block, 12 entries", "Westgate Property Group", "9 Gateway Avenue", "quote", 6),
    project("a1b2c3d4-0004-4000-8000-000000000004", "SSD-2026-011", "Retail store shutters", "Coastline Outfitters", "140 Marine Parade", "complete", 14),
    project("a1b2c3d4-0005-4000-8000-000000000005", "SSD-2026-010", "Residential rear door", "J. Patel", "7 Willow Close", "complete", 21),
  ];
}
