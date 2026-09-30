import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "cn"
import { Slot } from "radix-ui"

const badgeVariants = cva(
  "group/badge inline-flex h-5 w-fit shrink-0 items-center justify-center gap-1.5 overflow-hidden rounded-[var(--r-xs)] px-2 py-0.5 text-[11.5px] font-semibold whitespace-nowrap transition-colors [&>svg]:pointer-events-none [&>svg]:size-3",
  {
    variants: {
      variant: {
        default:
          "bg-[var(--accbg)] text-[var(--acc)] border border-[var(--acc)]/25",
        secondary:
          "bg-[var(--subtle)] text-[var(--ink2)] border border-[var(--bd)]",
        destructive:
          "bg-[var(--badbg)] text-[var(--bad)] border border-[var(--bad)]/25",
        outline:
          "border border-[var(--bd)] text-[var(--ink2)] bg-transparent",
        success:
          "bg-[var(--okbg)] text-[var(--ok)] border border-[var(--ok)]/25",
        warning:
          "bg-[var(--warnbg)] text-[var(--warn)] border border-[var(--warn)]/25",
        info:
          "bg-[var(--infobg)] text-[var(--info)] border border-[var(--info)]/25",
        ghost:
          "text-[var(--mut)] hover:text-[var(--ink)] bg-transparent",
        link:
          "text-[var(--acc)] underline-offset-4 hover:underline",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
)

function Badge({
  className,
  variant = "default",
  asChild = false,
  ...props
}: React.ComponentProps<"span"> &
  VariantProps<typeof badgeVariants> & { asChild?: boolean }) {
  const Comp = asChild ? Slot.Root : "span"

  return (
    <Comp
      data-slot="badge"
      data-variant={variant}
      className={cn(badgeVariants({ variant }), className)}
      {...props}
    />
  )
}

export { Badge, badgeVariants }
