"use client";

import { useEffect } from "react";
import { Button } from "@/components/ui/button";

export default function ErrorBoundary({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    // Log error cleanly without leaking details
    console.error(error);
  }, [error]);

  return (
    <div className="flex min-h-screen flex-1 flex-col items-center justify-center bg-[var(--bg)] px-4 text-center text-[var(--ink)]">
      <div className="flex max-w-md flex-col items-center gap-4">
        <span className="font-mono text-xs font-semibold uppercase tracking-wider text-[var(--bad)]">
          Application error
        </span>
        <h1 className="font-display text-2xl font-semibold tracking-tight">
          Something went wrong
        </h1>
        <p className="text-sm text-[var(--mut)] leading-relaxed">
          An unexpected error occurred while loading this view.
        </p>
        <div className="pt-2">
          <Button onClick={() => reset()}>Try again</Button>
        </div>
      </div>
    </div>
  );
}
