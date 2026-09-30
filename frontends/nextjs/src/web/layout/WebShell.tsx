"use client";

import Link from "next/link";
import { useTranslations } from "next-intl";
import type { ReactNode } from "react";

import { Button } from "@/components/ui/button";
import { LocaleSwitcher } from "@/shared/i18n/LocaleSwitcher";
import { ThemeToggle } from "@/shared/theme/ThemeToggle";
import { useAuth } from "@/shared/auth/AuthProvider";
import { signOut } from "../features/auth";

export function WebShell({ children }: { children: ReactNode }) {
  const t = useTranslations();
  const auth = useAuth();

  return (
    <div className="flex min-h-screen flex-1 flex-col bg-[var(--bg)] text-[var(--ink)]">
      <header className="sticky top-0 z-40 border-b border-[var(--bd)] bg-[var(--surf)]/90 backdrop-blur-sm">
        <div className="mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-y-2 px-3 sm:px-6 py-2.5 sm:py-0 min-h-[var(--h-xl)]">
          <Link
            href="/"
            className="font-display text-base sm:text-lg font-semibold tracking-tight text-[var(--ink)] hover:text-[var(--acc)] transition-colors"
          >
            {t("appName")}
          </Link>
          <nav className="flex flex-wrap items-center gap-1 sm:gap-1.5">
            <Button asChild variant="ghost" size="sm">
              <Link href="/">{t("nav.home")}</Link>
            </Button>
            {auth.status === "authenticated" && (
              <Button asChild variant="ghost" size="sm">
                <Link href="/profile">{t("nav.profile")}</Link>
              </Button>
            )}
            {auth.status === "authenticated" && (
              <Button asChild variant="ghost" size="sm">
                <Link href="/jobs">{t("nav.jobs")}</Link>
              </Button>
            )}
            {auth.status === "authenticated" && auth.hasPermission("users:read") && (
              <Button asChild variant="ghost" size="sm">
                <Link href="/admin">{t("nav.admin")}</Link>
              </Button>
            )}
            {auth.status === "authenticated" ? (
              <Button variant="outline" size="sm" onClick={() => signOut(auth)}>
                {t("nav.logout")}
              </Button>
            ) : (
              <>
                <Button asChild variant="ghost" size="sm">
                  <Link href="/login">{t("nav.login")}</Link>
                </Button>
                <Button asChild size="sm">
                  <Link href="/register">{t("nav.register")}</Link>
                </Button>
              </>
            )}
            <div className="flex items-center gap-1 border-l border-[var(--bd)] pl-2 ml-1">
              <LocaleSwitcher />
              <ThemeToggle />
            </div>
          </nav>
        </div>
      </header>
      <main className="mx-auto flex w-full max-w-5xl flex-1 items-start justify-center px-4 sm:px-6 py-8 sm:py-12">
        {children}
      </main>
      <footer className="border-t border-[var(--bd)] bg-[var(--surf)]">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-4 sm:px-6 py-4 text-xs text-[var(--mut)]">
          <span>{t("footer.tagline")}</span>
          <span className="font-mono text-[11px] text-[var(--faint)]">StackBraid</span>
        </div>
      </footer>
    </div>
  );
}
