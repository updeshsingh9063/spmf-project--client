# QuickBooks mapping (Phase 2 preparation)

The Alpha does not connect to QuickBooks. The data model already carries
the references the Phase 2 import needs, so no rebuild is required later.

## Fields already in the model

| SPFM field | QuickBooks Online object | Purpose |
|---|---|---|
| `project.quickbooks.customerId` | Customer | The client |
| `project.quickbooks.projectId` | Project (a sub-customer of the Customer) | The job the costs and invoices are tagged to |
| `project.quickbooks.lastSyncedAt` | – | When the import last ran for this project |
| `inputs.products[].qbItemId` | Item (Products and services) | Door and hardware items on invoice and bill lines |

## Where each SPFM figure is expected to come from

| SPFM figure | QuickBooks source (to confirm with Samuel and his accountant) |
|---|---|
| Doors & hardware revenue | Invoice lines on the project for product items |
| Variations | Invoice lines using a "Variations" service item |
| Doors & hardware cost | Bills / purchases for product items, tagged to the project (Cost of Goods Sold) |
| Labour | Time activities or payroll allocated to the project |
| Subcontractors | Bills from subcontractor vendors tagged to the project |
| Other direct | Freight, consumables and permits tagged to the project |
| Overhead % | Stays in SPFM (a business rule, not a QuickBooks figure) |

The same mapping is in code in `src/lib/quickbooks.ts`.

## Open questions for Phase 2

- Does Shield use QuickBooks Projects, or classes/locations, to tag job costs?
- Which accounts hold subcontractor and other direct costs?
- Is labour tracked through QuickBooks Time / payroll, or entered in SPFM?
- One-off import per project, or a scheduled sync?
