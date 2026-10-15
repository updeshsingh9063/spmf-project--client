"use client";

// Work Queue — the operational home of SPFM.
//
// Placeholder for Ticket 15, which generates items from unresolved system states:
// payment received before shipment, partial shipment awaiting allocation, past-due
// invoices, unmatched QuickBooks activity, and periods with unresolved decisions.
//
// Ticket 1 deliberately stops here. The reset replaces the gross-margin dashboard
// that used to occupy this route; building the queue before the domain model
// (Ticket 2) and the calculation service (Ticket 3) exist would be guesswork.

import Link from "next/link";
import { Icon } from "@/components/ui";

const PLANNED_ITEM_TYPES = [
  {
    issue: "Payment received before shipment",
    action: "Confirm the revenue stays deferred, or link it to a shipment",
    ticket: 12,
  },
  {
    issue: "Partial shipment recorded",
    action: "Allocate contract value to the shipped portion",
    ticket: 13,
  },
  {
    issue: "Past-due invoice / unpaid balance",
    action: "Review collection status and follow-up context",
    ticket: 17,
  },
  {
    issue: "Unmatched QuickBooks activity",
    action: "Match to the right project, or resolve as non-project",
    ticket: 16,
  },
  {
    issue: "Accounting period has unresolved decisions",
    action: "Open the decisions blocking the period close",
    ticket: 19,
  },
];

export default function WorkQueuePage() {
  return (
    <div className="mx-auto max-w-4xl">
      <header>
        <h1 className="display text-[1.75rem] leading-tight">Work Queue</h1>
        <p className="mt-1 text-sm text-ink-2">
          Projects and records that need a financial decision or follow-up.
        </p>
      </header>

      <section className="panel mt-6 p-6" aria-labelledby="empty-h">
        <h2 id="empty-h" className="text-[0.95rem] font-semibold">
          Nothing to resolve yet
        </h2>
        <p className="mt-1 max-w-[60ch] text-sm text-ink-2">
          The queue fills from QuickBooks activity and shipment state. Neither is connected yet, so there is nothing
          for it to surface.
        </p>

        <div className="mt-5 rounded-md border border-line bg-paper px-4 py-3">
          <p className="text-xs font-medium text-ink">Realignment in progress</p>
          <p className="mt-1 text-xs leading-relaxed text-ink-2">
            This screen replaced the gross-margin dashboard as part of Ticket&nbsp;1. SPFM is a financial operations
            layer over QuickBooks, not a job-costing calculator — the Work Queue is its home, not a profitability
            summary.
          </p>
        </div>

        <h3 className="mt-6 text-xs font-semibold tracking-wide text-ink-2 uppercase">
          What will appear here
        </h3>
        <ul className="mt-3 divide-y divide-line-soft">
          {PLANNED_ITEM_TYPES.map((item) => (
            <li key={item.issue} className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1 py-2.5">
              <div className="min-w-0">
                <p className="text-sm font-medium">{item.issue}</p>
                <p className="text-xs text-ink-2">{item.action}</p>
              </div>
              <span className="shrink-0 font-mono text-xs text-muted">Ticket {item.ticket}</span>
            </li>
          ))}
        </ul>
      </section>

      <p className="mt-5 text-sm text-ink-2">
        <Link href="/projects" className="font-medium text-brass-deep hover:underline">
          Go to Projects
        </Link>{" "}
        <Icon name="back" size={12} />
      </p>
    </div>
  );
}
