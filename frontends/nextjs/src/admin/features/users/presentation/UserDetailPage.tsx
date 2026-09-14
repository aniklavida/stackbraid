"use client";

import Link from "next/link";
import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { toast } from "sonner";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { useRoles } from "@/admin/features/roles";
import { useAssignRole, useRevokeRole, useUser } from "../application/use-users";

export function UserDetailPage({ userId }: { userId: string }) {
  const t = useTranslations("users");
  const locale = useLocale();
  const { data: user, isPending, isError } = useUser(userId);
  const { data: roles } = useRoles();
  const assignRole = useAssignRole(userId);
  const revokeRole = useRevokeRole(userId);
  const [selectedRoleId, setSelectedRoleId] = useState<string>("");

  if (isPending) return <Skeleton className="h-64 w-full max-w-2xl" />;
  if (isError || !user) {
    return (
      <p role="alert" className="text-destructive">
        {t("error")}
      </p>
    );
  }

  const dateFormatter = new Intl.DateTimeFormat(locale, { dateStyle: "long" });
  const assignableRoles = (roles ?? []).filter((role) => !user.roles.some((held) => held.id === role.id));

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button asChild variant="ghost" size="sm" className="mb-2 -ml-3">
          <Link href="/admin/users">← {t("detail.back")}</Link>
        </Button>
        <h1 className="text-2xl font-semibold tracking-tight">{user.displayName}</h1>
        <p className="text-muted-foreground">{user.email}</p>
      </div>

      <Card className="max-w-2xl">
        <CardHeader>
          <CardTitle>{t("detail.account")}</CardTitle>
        </CardHeader>
        <CardContent>
          <dl className="grid grid-cols-[max-content_1fr] gap-x-6 gap-y-2 text-sm">
            <dt className="text-muted-foreground">{t("columns.status")}</dt>
            <dd>
              <Badge variant={user.status === "active" ? "default" : "secondary"}>
                {t(user.status === "active" ? "statusActive" : "statusInactive")}
              </Badge>
            </dd>
            <dt className="text-muted-foreground">{t("detail.createdAt")}</dt>
            <dd>{dateFormatter.format(new Date(user.createdAt))}</dd>
            <dt className="text-muted-foreground">{t("detail.lastLoginAt")}</dt>
            <dd>{user.lastLoginAt ? dateFormatter.format(new Date(user.lastLoginAt)) : t("detail.lastLoginAt_never")}</dd>
          </dl>
        </CardContent>
      </Card>

      <Card className="max-w-2xl">
        <CardHeader>
          <CardTitle>{t("detail.roles")}</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {user.roles.length === 0 && <p className="text-muted-foreground">{t("detail.noRoles")}</p>}
          <ul className="flex flex-col gap-2">
            {user.roles.map((role) => (
              <li key={role.id} className="flex items-center justify-between rounded-md border px-3 py-2">
                <span className="font-medium">{role.name}</span>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={revokeRole.isPending}
                  onClick={() =>
                    revokeRole.mutate(role.id, {
                      onSuccess: () => toast.success(`${role.name} revoked.`),
                    })
                  }
                >
                  {t("detail.revoke")}
                </Button>
              </li>
            ))}
          </ul>

          {assignableRoles.length > 0 && (
            <div className="flex items-end gap-2">
              <div className="flex flex-1 flex-col gap-2">
                <span className="text-sm font-medium">{t("detail.assignRole")}</span>
                <Select value={selectedRoleId} onValueChange={setSelectedRoleId}>
                  <SelectTrigger>
                    <SelectValue placeholder={t("detail.assignRole")} />
                  </SelectTrigger>
                  <SelectContent>
                    {assignableRoles.map((role) => (
                      <SelectItem key={role.id} value={role.id}>
                        {role.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <Button
                disabled={!selectedRoleId || assignRole.isPending}
                onClick={() => {
                  const role = assignableRoles.find((candidate) => candidate.id === selectedRoleId);
                  assignRole.mutate(selectedRoleId, {
                    onSuccess: () => {
                      toast.success(`${role?.name ?? selectedRoleId} assigned.`);
                      setSelectedRoleId("");
                    },
                  });
                }}
              >
                {t("detail.assign")}
              </Button>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
