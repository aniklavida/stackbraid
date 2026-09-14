"use client";

import Link from "next/link";
import { useTranslations } from "next-intl";

import { Button } from "@/components/ui/button";
import { useAuth } from "@/shared/auth/AuthProvider";

export function HomePage() {
  const t = useTranslations("home");
  const auth = useAuth();

  if (auth.status === "loading") {
    return <p className="text-muted-foreground">{t("loading")}</p>;
  }

  if (auth.status === "authenticated" && auth.user) {
    return (
      <div className="flex max-w-xl flex-col gap-4">
        <h1 className="text-3xl font-semibold tracking-tight">{t("signedIn.greeting", { name: auth.user.displayName })}</h1>
        <p className="text-muted-foreground">{t("signedIn.body")}</p>
        <Button asChild className="w-fit">
          <Link href="/profile">{t("signedIn.profileCta")}</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="flex max-w-xl flex-col gap-4">
      <h1 className="text-3xl font-semibold tracking-tight">{t("signedOut.heading")}</h1>
      <p className="text-muted-foreground">{t("signedOut.body")}</p>
      <div className="flex gap-3">
        <Button asChild>
          <Link href="/login">{t("signedOut.loginCta")}</Link>
        </Button>
        <Button asChild variant="outline">
          <Link href="/register">{t("signedOut.registerCta")}</Link>
        </Button>
      </div>
    </div>
  );
}
