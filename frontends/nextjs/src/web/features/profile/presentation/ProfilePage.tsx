"use client";

import { useLocale, useTranslations } from "next-intl";

import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { Skeleton } from "@/components/ui/skeleton";
import { useOwnProfile } from "../application/use-own-profile";
import { roleNames } from "../domain/profile";

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
        <CardHeader>
          <Skeleton className="h-6 w-32" />
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
      <p role="alert" className="text-destructive">
        {t("error")}
      </p>
    );
  }

  const dateFormatter = new Intl.DateTimeFormat(locale, { dateStyle: "long" });

  return (
    <Card className="w-full max-w-lg">
      <CardHeader className="flex flex-row items-center gap-4">
        <Avatar className="h-12 w-12">
          <AvatarFallback>{initials(profile.displayName)}</AvatarFallback>
        </Avatar>
        <div>
          <CardTitle>
            <h1>{profile.displayName}</h1>
          </CardTitle>
          <p className="text-sm text-muted-foreground">{profile.email}</p>
        </div>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        <Separator />
        <dl className="grid grid-cols-[max-content_1fr] gap-x-6 gap-y-3 text-sm">
          <dt className="text-muted-foreground">{t("status")}</dt>
          <dd>
            <Badge variant={profile.status === "active" ? "default" : "secondary"}>
              {t(profile.status === "active" ? "status_active" : "status_inactive")}
            </Badge>
          </dd>

          <dt className="text-muted-foreground">{t("roles")}</dt>
          <dd className="flex flex-wrap gap-1">
            {roleNames(profile).map((name) => (
              <Badge key={name} variant="outline">
                {name}
              </Badge>
            ))}
          </dd>

          <dt className="text-muted-foreground">{t("memberSince")}</dt>
          <dd>{dateFormatter.format(new Date(profile.createdAt))}</dd>

          <dt className="text-muted-foreground">{t("lastLogin")}</dt>
          <dd>{profile.lastLoginAt ? dateFormatter.format(new Date(profile.lastLoginAt)) : t("lastLogin_never")}</dd>
        </dl>
      </CardContent>
    </Card>
  );
}
