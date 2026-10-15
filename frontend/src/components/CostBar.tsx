"use client";

import { useState } from "react";
import { fmtMoney } from "@/lib/format";
import type { SpfmResults } from "@/lib/types";

// Where each dollar of the contract goes: direct costs, overhead and what's left.
export function costSegments(r: SpfmResults) {
  return [
    { key: "productCost", label: "Doors & hardware", value: r.productCost, color: "var(--color-s1)" },
    { key: "labourCost", label: "Labour", value: r.labourCost, color: "var(--color-s2)" },
    { key: "subcontractorCost", label: "Subcontractors", value: r.subcontractorCost, color: "var(--color-s3)" },
    { key: "otherDirectCost", label: "Other direct", value: r.otherDirectCost, color: "var(--color-s4)" },
    { key: "overhead", label: "Overhead", value: r.overhead, color: "var(--color-s5)" },
  ];
}

export function CostBar({ results }: { results: SpfmResults }) {
  const [hover, setHover] = useState<string | null>(null);
  const segs = costSegments(results).filter((s) => s.value > 0);
  const totalCost = results.directCost + results.overhead;
  const profit = results.netProfit;
  const scale = Math.max(results.revenue, totalCost) || 1;
  const all = profit > 0 ? [...segs, { key: "profit", label: "Net profit", value: profit, color: "var(--color-steel)" }] : segs;
  const active = all.find((s) => s.key === hover);

  return (
    <div>
      <div className="mb-2 flex h-5 items-baseline justify-between text-xs text-muted" aria-live="polite">
        <span>
          {active ? (
            <>
              <span className="font-medium text-ink">{active.label}</span> {fmtMoney(active.value)} (
              {((active.value / (results.revenue || 1)) * 100).toFixed(1)}% of contract)
            </>
          ) : (
            "Where the contract value goes"
          )}
        </span>
        {profit < 0 && <span className="font-medium text-bad">Costs exceed contract by {fmtMoney(-profit)}</span>}
      </div>
      <div className="relative">
        <div className="flex h-9 gap-[2px] overflow-hidden rounded-[4px]" role="img" aria-label="Cost breakdown of the contract value">
          {all.map((s) => (
            <div
              key={s.key}
              onMouseEnter={() => setHover(s.key)}
              onMouseLeave={() => setHover(null)}
              className="h-full transition-opacity"
              style={{
                width: `${(s.value / scale) * 100}%`,
                background: s.color,
                opacity: hover && hover !== s.key ? 0.45 : 1,
              }}
            />
          ))}
        </div>
        {profit < 0 && (
          <div
            className="pointer-events-none absolute -top-1 -bottom-1 w-[2px] bg-bad"
            style={{ left: `${(results.revenue / scale) * 100}%` }}
            title="Contract value"
          />
        )}
      </div>
      <ul className="mt-3 flex flex-wrap gap-x-4 gap-y-1.5 text-xs text-ink-2">
        {all.map((s) => (
          <li key={s.key} className="inline-flex items-center gap-1.5">
            <span className="inline-block h-2.5 w-2.5 rounded-[2px]" style={{ background: s.color }} aria-hidden="true" />
            {s.label}
          </li>
        ))}
      </ul>
    </div>
  );
}
