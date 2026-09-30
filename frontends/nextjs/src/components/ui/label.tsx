"use client"

import * as React from "react"
import { cn } from "cn"
import { Label as LabelPrimitive } from "radix-ui"

function Label({
  className,
  ...props
}: React.ComponentProps<typeof LabelPrimitive.Root>) {
  return (
    <LabelPrimitive.Root
      data-slot="label"
      className={cn(
        "text-xs font-medium text-[var(--mut)] select-none peer-disabled:cursor-not-allowed peer-disabled:opacity-45 block mb-1.5",
        className
      )}
      {...props}
    />
  )
}

export { Label }
