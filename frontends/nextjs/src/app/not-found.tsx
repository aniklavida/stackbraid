import Link from "next/link";
import { Button } from "@/components/ui/button";

export default function NotFound() {
  return (
    <div className="flex min-h-screen flex-1 flex-col items-center justify-center bg-[var(--bg)] px-4 text-center text-[var(--ink)]">
      <div className="flex max-w-md flex-col items-center gap-4">
        <span className="font-mono text-xs font-semibold uppercase tracking-wider text-[var(--acc)]">
          404 Not Found
        </span>
        <h1 className="font-display text-3xl font-semibold tracking-tight">
          Page not found
        </h1>
        <p className="text-sm text-[var(--mut)] leading-relaxed">
          The requested page could not be found. Check the URL address or return to the application.
        </p>
        <div className="pt-2">
          <Button asChild>
            <Link href="/">Return home</Link>
          </Button>
        </div>
      </div>
    </div>
  );
}
