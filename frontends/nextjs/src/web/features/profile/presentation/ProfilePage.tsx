"use client";

import { useLocale, useTranslations } from "next-intl";

import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { Skeleton } from "@/components/ui/skeleton";
import { useOwnProfile } from "../application/use-own-profile";
import { roleNames } from "../domain/profile";
import { NotificationArea } from "./NotificationArea";

function initials(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]!.toUpperCase())
    .join("");
}

export function ProfilePage() {
  const t = useTranslations("profile");
  const locale = useLocale();
  const { data: profile, isPending, isError } = useOwnProfile();

  if (isPending) {
    return (
      <Card className="w-full max-w-lg">
        <CardHeader className="flex flex-row items-center gap-4">
          <Skeleton className="size-12 rounded-full" />
          <div className="flex flex-col gap-2">
            <Skeleton className="h-5 w-36" />
            <Skeleton className="h-3.5 w-48" />
          </div>
        </CardHeader>
        <CardContent className="flex flex-col gap-3">
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-2/3" />
          <Skeleton className="h-4 w-1/2" />
        </CardContent>
      </Card>
    );
  }

  if (isError || !profile) {
    return (
      <div role="alert" className="w-full max-w-lg rounded-[var(--r-md)] border border-[var(--bad)]/25 bg-[var(--badbg)] p-4 text-xs text-[var(--bad)] font-medium">
        {t("error")}
      </div>
    );
  }

  const dateFormatter = new Intl.DateTimeFormat(locale, { dateStyle: "long" });

  return (
    <div className="flex w-full max-w-lg flex-col gap-6">
      <Card className="w-full">
        <CardHeader className="flex flex-row items-center gap-4">
          <Avatar className="size-12 border border-[var(--bd)] bg-[var(--chip)]">
            <AvatarFallback className="font-semibold text-sm text-[var(--ink)]">{initials(profile.displayName)}</AvatarFallback>
          </Avatar>
          <div className="flex flex-col gap-0.5">
            <CardTitle>
              <h1 className="font-display text-xl font-semibold tracking-tight text-[var(--ink)]">{profile.displayName}</h1>
            </CardTitle>
            <p className="font-mono text-xs text-[var(--mut)]">{profile.email}</p>
          </div>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <Separator />
          <dl className="grid grid-cols-[max-content_1fr] gap-x-6 gap-y-3 text-xs">
            <dt className="font-medium text-[var(--mut)]">{t("status")}</dt>
            <dd>
              <Badge variant={profile.status === "active" ? "success" : "secondary"}>
                {t(profile.status === "active" ? "status_active" : "status_inactive")}
              </Badge>
            </dd>

            <dt className="font-medium text-[var(--mut)]">{t("roles")}</dt>
            <dd className="flex flex-wrap gap-1">
              {roleNames(profile).map((name) => (
                <Badge key={name} variant="outline" className="font-mono text-xs">
                  {name}
                </Badge>
              ))}
            </dd>

            <dt className="font-medium text-[var(--mut)]">{t("memberSince")}</dt>
            <dd className="font-mono text-[var(--ink)]">{dateFormatter.format(new Date(profile.createdAt))}</dd>

            <dt className="font-medium text-[var(--mut)]">{t("lastLogin")}</dt>
            <dd className="font-mono text-[var(--ink)]">{profile.lastLoginAt ? dateFormatter.format(new Date(profile.lastLoginAt)) : t("lastLogin_never")}</dd>
          </dl>
        </CardContent>
      </Card>
      <NotificationArea />
    </div>
  );
}
