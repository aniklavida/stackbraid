import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "cn"
import { Slot } from "radix-ui"

const buttonVariants = cva(
  "group/button inline-flex shrink-0 items-center justify-center font-medium whitespace-nowrap transition-colors outline-none select-none focus-visible:ring-2 focus-visible:ring-[var(--acc)] focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--bg)] disabled:pointer-events-none disabled:opacity-45 disabled:cursor-not-allowed [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
  {
    variants: {
      variant: {
        default:
          "bg-[var(--acc)] text-[var(--on-acc)] hover:bg-[var(--acc2)] active:translate-y-px",
        outline:
          "border border-[var(--bd)] bg-[var(--surf)] text-[var(--ink)] hover:bg-[var(--subtle)] hover:border-[var(--bd2)] active:translate-y-px",
        secondary:
          "bg-[var(--subtle)] text-[var(--ink)] hover:bg-[var(--hov)] active:translate-y-px",
        ghost:
          "bg-transparent text-[var(--ink)] hover:bg-[var(--hov)] active:bg-[var(--act)]",
        destructive:
          "bg-[var(--badbg)] text-destructive border border-[var(--bad)]/25 hover:bg-[var(--bad)] hover:text-white active:translate-y-px",
        link:
          "text-[var(--acc)] underline-offset-4 hover:underline p-0 h-auto font-normal",
      },
      size: {
        default: "h-[var(--h-md)] gap-2 px-3.5 text-sm rounded-[var(--r-md)]",
        xs: "h-[var(--h-xs)] gap-1 px-2 text-xs rounded-[var(--r-xs)] [&_svg:not([class*='size-'])]:size-3",
        sm: "h-[var(--h-sm)] gap-1.5 px-2.5 text-xs rounded-[var(--r-sm)] [&_svg:not([class*='size-'])]:size-3.5",
        lg: "h-[var(--h-lg)] gap-2 px-4 text-base rounded-[var(--r-md)]",
        xl: "h-[var(--h-xl)] gap-2.5 px-5 text-base rounded-[var(--r-md)]",
        icon: "size-[var(--h-md)] rounded-[var(--r-md)] p-0",
        "icon-sm": "size-[var(--h-sm)] rounded-[var(--r-sm)] p-0",
        "icon-xs": "size-[var(--h-xs)] rounded-[var(--r-xs)] p-0",
        "icon-lg": "size-[var(--h-lg)] rounded-[var(--r-md)] p-0",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

function Button({
  className,
  variant = "default",
  size = "default",
  asChild = false,
  ...props
}: React.ComponentProps<"button"> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean
  }) {
  const Comp = asChild ? Slot.Root : "button"

  return (
    <Comp
      data-slot="button"
      data-variant={variant}
      data-size={size}
      className={cn(buttonVariants({ variant, size, className }))}
      {...props}
    />
  )
}

export { Button, buttonVariants }
