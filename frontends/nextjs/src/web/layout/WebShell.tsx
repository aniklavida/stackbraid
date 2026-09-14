"use client";

import Link from "next/link";
import { useTranslations } from "next-intl";
import type { ReactNode } from "react";

import { Button } from "@/components/ui/button";
import { LocaleSwitcher } from "@/shared/i18n/LocaleSwitcher";
import { useAuth } from "@/shared/auth/AuthProvider";
import { signOut } from "../features/auth";

export function WebShell({ children }: { children: ReactNode }) {
  const t = useTranslations();
  const auth = useAuth();

  return (
    <div className="flex min-h-full flex-1 flex-col">
      <header className="border-b">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-6 py-4">
          <Link href="/" className="text-lg font-semibold tracking-tight">
            {t("appName")}
          </Link>
          <nav className="flex items-center gap-2">
            <Button asChild variant="ghost" size="sm">
              <Link href="/">{t("nav.home")}</Link>
            </Button>
            {auth.status === "authenticated" && (
              <Button asChild variant="ghost" size="sm">
                <Link href="/profile">{t("nav.profile")}</Link>
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
            <LocaleSwitcher />
          </nav>
        </div>
      </header>
      <main className="mx-auto flex w-full max-w-5xl flex-1 items-start justify-center px-6 py-16">{children}</main>
      <footer className="border-t">
        <div className="mx-auto max-w-5xl px-6 py-6 text-sm text-muted-foreground">{t("footer.tagline")}</div>
      </footer>
    </div>
  );
}
