"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useTranslations } from "next-intl";
import type { ReactNode } from "react";

import { cn } from "@/lib/utils";
import { LocaleSwitcher } from "@/shared/i18n/LocaleSwitcher";
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
      <div className="flex min-h-full flex-1">
        <aside className="flex w-56 flex-col border-r px-4 py-6">
          <span className="mb-6 text-lg font-semibold tracking-tight">{t("title")}</span>
          <nav className="flex flex-col gap-1">
            {NAV_ITEMS.map((item) => (
              <Link
                key={item.href}
                href={item.href}
                className={cn(
                  "rounded-md px-3 py-2 text-sm font-medium transition-colors hover:bg-muted",
                  pathname.startsWith(item.href) && "bg-muted",
                )}
              >
                {t(`nav.${item.key}`)}
              </Link>
            ))}
          </nav>
          <Link href="/" className="mt-auto text-sm text-muted-foreground hover:underline">
            {t("nav.backToSite")}
          </Link>
        </aside>
        <div className="flex flex-1 flex-col">
          <header className="flex justify-end border-b px-6 py-3">
            <LocaleSwitcher />
          </header>
          <main className="flex-1 px-6 py-8">{children}</main>
        </div>
      </div>
    </RequireAuth>
  );
}
