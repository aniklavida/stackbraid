import * as React from "react"
import { cn } from "cn"

function Input({ className, type, ...props }: React.ComponentProps<"input">) {
  return (
    <input
      type={type}
      data-slot="input"
      className={cn(
        "flex h-[var(--h-md)] w-full min-w-0 rounded-[var(--r-md)] border border-[var(--bd)] bg-[var(--surf)] px-3 py-1.5 text-sm text-[var(--ink)] shadow-[var(--shadow-card)] transition-colors placeholder:text-[var(--faint)] outline-none focus-visible:border-[var(--acc)] focus-visible:ring-2 focus-visible:ring-[var(--acc)]/30 disabled:cursor-not-allowed disabled:opacity-45 disabled:bg-[var(--subtle)] aria-invalid:border-[var(--bad)] aria-invalid:ring-2 aria-invalid:ring-[var(--bad)]/20 file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-[var(--ink)]",
        className
      )}
      {...props}
    />
  )
}

export { Input }
