"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useTranslations } from "next-intl";
import type { ReactNode } from "react";

import { cn } from "@/lib/utils";
import { LocaleSwitcher } from "@/shared/i18n/LocaleSwitcher";
import { ThemeToggle } from "@/shared/theme/ThemeToggle";
import { RequireAuth } from "@/shared/auth/RequireAuth";

const NAV_ITEMS = [
  { href: "/admin/users", key: "users" as const },
  { href: "/admin/roles", key: "roles" as const },
];

export function AdminShell({ children }: { children: ReactNode }) {
  const t = useTranslations("admin");
  const pathname = usePathname();

  return (
    <RequireAuth requirePermission="users:read">
      <div className="flex min-h-screen flex-1 flex-col md:flex-row bg-[var(--bg)] text-[var(--ink)]">
        <aside className="flex flex-col md:w-56 lg:w-60 border-b md:border-b-0 md:border-r border-[var(--bd)] bg-[var(--rail)] px-4 py-3 md:py-5 shrink-0">
          <div className="mb-3 md:mb-6 flex items-center justify-between md:justify-start gap-2">
            <span className="font-display text-base md:text-lg font-semibold tracking-tight text-[var(--ink)]">
              {t("title")}
            </span>
            <Link
              href="/"
              className="md:hidden text-xs font-medium text-[var(--mut)] hover:text-[var(--ink)] hover:underline inline-flex items-center gap-1"
            >
              ← {t("nav.backToSite")}
            </Link>
          </div>
          <nav className="flex flex-row md:flex-col gap-1 overflow-x-auto pb-1 md:pb-0">
            {NAV_ITEMS.map((item) => {
              const isActive = pathname.startsWith(item.href);
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  className={cn(
                    "rounded-[var(--r-sm)] px-3 py-1.5 md:py-2 text-xs md:text-sm font-medium whitespace-nowrap transition-colors text-[var(--ink2)] hover:bg-[var(--hov)] hover:text-[var(--ink)]",
                    isActive && "bg-[var(--act)] text-[var(--ink)] font-semibold shadow-[var(--shadow-card)]"
                  )}
                >
                  {t(`nav.${item.key}`)}
                </Link>
              );
            })}
          </nav>
          <div className="hidden md:block mt-auto pt-4 border-t border-[var(--bd)]">
            <Link
              href="/"
              className="text-xs font-medium text-[var(--mut)] hover:text-[var(--ink)] hover:underline inline-flex items-center gap-1"
            >
              ← {t("nav.backToSite")}
            </Link>
          </div>
        </aside>
        <div className="flex flex-1 flex-col min-w-0">
          <header className="sticky top-0 z-30 flex items-center justify-between border-b border-[var(--bd)] bg-[var(--surf)] px-4 sm:px-6 h-[var(--h-lg)]">
            <div className="text-xs font-mono uppercase tracking-wider text-[var(--mut)]">
              Console
            </div>
            <div className="flex items-center gap-2">
              <LocaleSwitcher />
              <ThemeToggle />
            </div>
          </header>
          <main className="flex-1 px-4 sm:px-8 py-6 sm:py-8 overflow-y-auto">
            <div className="mx-auto max-w-5xl">
              {children}
            </div>
          </main>
        </div>
      </div>
    </RequireAuth>
  );
}
