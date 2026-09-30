"use client";

import Link from "next/link";
import { useTranslations } from "next-intl";
import { ShieldCheck, Cpu, Code2 } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { useAuth } from "@/shared/auth/AuthProvider";

export function HomePage() {
  const t = useTranslations("home");
  const auth = useAuth();

  if (auth.status === "loading") {
    return (
      <div className="flex w-full max-w-2xl flex-col items-center justify-center py-16 gap-3">
        <div className="size-5 rounded-full border-2 border-[var(--acc)] border-t-transparent animate-spin" />
        <p className="text-sm font-medium text-[var(--mut)]">{t("loading")}</p>
      </div>
    );
  }

  if (auth.status === "authenticated" && auth.user) {
    return (
      <div className="flex w-full max-w-2xl flex-col gap-8">
        <div className="flex flex-col gap-3">
          <h1 className="font-display text-3xl sm:text-4xl font-semibold tracking-tight text-[var(--ink)]">
            {t("signedIn.greeting", { name: auth.user.displayName })}
          </h1>
          <p className="text-base text-[var(--mut)] leading-relaxed">
            {t("signedIn.body")}
          </p>
          <div className="pt-2">
            <Button asChild>
              <Link href="/profile">{t("signedIn.profileCta")}</Link>
            </Button>
          </div>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-4 border-t border-[var(--line)]">
          <Card size="sm">
            <CardHeader className="gap-2">
              <ShieldCheck className="size-4 text-[var(--acc)]" />
              <CardTitle className="text-sm font-semibold">Session</CardTitle>
              <CardDescription className="text-xs">
                Active JWT with httpOnly refresh cookie verification.
              </CardDescription>
            </CardHeader>
          </Card>
          <Card size="sm">
            <CardHeader className="gap-2">
              <Cpu className="size-4 text-[var(--acc)]" />
              <CardTitle className="text-sm font-semibold">Jobs</CardTitle>
              <CardDescription className="text-xs">
                Realtime progress events streamed across multi-tab sessions.
              </CardDescription>
            </CardHeader>
          </Card>
          <Card size="sm">
            <CardHeader className="gap-2">
              <Code2 className="size-4 text-[var(--acc)]" />
              <CardTitle className="text-sm font-semibold">Contract</CardTitle>
              <CardDescription className="text-xs">
                100% conforming endpoints verified against OpenAPI.
              </CardDescription>
            </CardHeader>
          </Card>
        </div>
      </div>
    );
  }

  return (
    <div className="flex w-full max-w-2xl flex-col gap-8">
      <div className="flex flex-col gap-4">
        <h1 className="font-display text-3xl sm:text-4xl font-semibold tracking-tight text-[var(--ink)]">
          {t("signedOut.heading")}
        </h1>
        <p className="text-base text-[var(--mut)] leading-relaxed">
          {t("signedOut.body")}
        </p>
        <div className="flex items-center gap-3 pt-2">
          <Button asChild>
            <Link href="/login">{t("signedOut.loginCta")}</Link>
          </Button>
          <Button asChild variant="outline">
            <Link href="/register">{t("signedOut.registerCta")}</Link>
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-4 border-t border-[var(--line)]">
        <Card size="sm">
          <CardHeader className="gap-2">
            <ShieldCheck className="size-4 text-[var(--acc)]" />
            <CardTitle className="text-sm font-semibold">Identity flow</CardTitle>
            <CardDescription className="text-xs">
              Role-gated administrative surfaces and authenticated user profile.
            </CardDescription>
          </CardHeader>
        </Card>
        <Card size="sm">
          <CardHeader className="gap-2">
            <Cpu className="size-4 text-[var(--acc)]" />
            <CardTitle className="text-sm font-semibold">Realtime jobs</CardTitle>
            <CardDescription className="text-xs">
              Live event-streamed job execution across tabs and clients.
            </CardDescription>
          </CardHeader>
        </Card>
        <Card size="sm">
          <CardHeader className="gap-2">
            <Code2 className="size-4 text-[var(--acc)]" />
            <CardTitle className="text-sm font-semibold">Shared contract</CardTitle>
            <CardDescription className="text-xs">
              OpenAPI-backed type-safe clients across TypeScript and Dart.
            </CardDescription>
          </CardHeader>
        </Card>
      </div>
    </div>
  );
}
