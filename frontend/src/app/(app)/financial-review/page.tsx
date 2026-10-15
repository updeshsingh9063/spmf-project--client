"use client";

// Financial Review — accounting period close.
//
// Placeholder for Ticket 19. Disabled in the navigation until then, per Ticket 1:
// "Primary navigation includes Work Queue, Projects and Financial Review
// (Financial Review may be disabled until Ticket 19)."

export default function FinancialReviewPage() {
  return (
    <div className="mx-auto max-w-4xl">
      <header>
        <h1 className="display text-[1.75rem] leading-tight">Financial Review</h1>
        <p className="mt-1 text-sm text-ink-2">Confirm a period is ready before accounting close.</p>
      </header>

      <section className="panel mt-6 p-6">
        <h2 className="text-[0.95rem] font-semibold">Not built yet</h2>
        <p className="mt-1 max-w-[60ch] text-sm text-ink-2">
          This workspace arrives in Ticket&nbsp;19. It will show recognition events, deferred balances and the
          unresolved decisions affecting a selected period — and will refuse to report a clean close while material
          issues remain.
        </p>
        <p className="mt-3 max-w-[60ch] text-sm text-ink-2">
          It depends on the recognition engine (Tickets&nbsp;12–14) and the Work Queue (Ticket&nbsp;15), so it stays
          disabled until those exist.
        </p>
      </section>
    </div>
  );
}
