"use client";

import Link from "next/link";
import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { toast } from "sonner";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { useRoles } from "@/admin/features/roles";
import { useAssignRole, useRestoreUser, useRevokeRole, useUser, useUserAudit } from "../application/use-users";

export function UserDetailPage({ userId }: { userId: string }) {
  const t = useTranslations("users");
  const locale = useLocale();
  const { data: user, isPending, isError } = useUser(userId);
  const { data: roles } = useRoles();
  const { data: audit, isPending: auditPending } = useUserAudit(userId);
  const restore = useRestoreUser();
  const assignRole = useAssignRole(userId);
  const revokeRole = useRevokeRole(userId);
  const [selectedRoleId, setSelectedRoleId] = useState<string>("");

  if (isPending) {
    return (
      <div className="flex flex-col gap-4 max-w-2xl">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-48 w-full" />
      </div>
    );
  }

  if (isError || !user) {
    return (
      <div role="alert" className="max-w-2xl rounded-[var(--r-md)] border border-[var(--bad)]/25 bg-[var(--badbg)] p-4 text-xs text-[var(--bad)] font-medium">
        {t("error")}
      </div>
    );
  }

  const dateFormatter = new Intl.DateTimeFormat(locale, { dateStyle: "long" });
  const assignableRoles = (roles ?? []).filter((role) => !user.roles.some((held) => held.id === role.id));

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button asChild variant="ghost" size="sm" className="mb-2 -ml-2.5 text-xs text-[var(--mut)] hover:text-[var(--ink)]">
          <Link href="/admin/users">← {t("detail.back")}</Link>
        </Button>
        <h1 className="font-display text-2xl font-semibold tracking-tight text-[var(--ink)]">{user.displayName}</h1>
        <p className="font-mono text-xs text-[var(--mut)]">{user.email}</p>
      </div>

      <Card className="max-w-2xl">
        <CardHeader>
          <CardTitle>
            <h2 className="font-display text-base font-semibold tracking-tight text-[var(--ink)]">{t("detail.account")}</h2>
          </CardTitle>
        </CardHeader>
        <CardContent>
          <dl className="grid grid-cols-[max-content_1fr] gap-x-6 gap-y-2.5 text-xs">
            <dt className="font-medium text-[var(--mut)]">{t("columns.status")}</dt>
            <dd>
              <Badge variant={user.status === "active" ? "success" : "secondary"}>
                {t(user.status === "active" ? "statusActive" : "statusInactive")}
              </Badge>
            </dd>
            <dt className="font-medium text-[var(--mut)]">{t("detail.createdAt")}</dt>
            <dd className="font-mono text-[var(--ink)]">{dateFormatter.format(new Date(user.createdAt))}</dd>
            <dt className="font-medium text-[var(--mut)]">{t("detail.lastLoginAt")}</dt>
            <dd className="font-mono text-[var(--ink)]">{user.lastLoginAt ? dateFormatter.format(new Date(user.lastLoginAt)) : t("detail.lastLoginAt_never")}</dd>
            {user.deletedAt && (
              <>
                <dt className="font-medium text-[var(--mut)]">{t("detail.deletedAt")}</dt>
                <dd className="font-mono text-[var(--ink)]">{dateFormatter.format(new Date(user.deletedAt))}</dd>
              </>
            )}
          </dl>
          {user.deletedAt && (
            <Button
              variant="outline"
              size="sm"
              className="mt-4"
              onClick={() => restore.mutate(userId, { onSuccess: () => toast.success(t("actions.restored", { name: user.displayName })) })}
              disabled={restore.isPending}
            >
              {t("actions.restore")}
            </Button>
          )}
        </CardContent>
      </Card>

      <Card className="max-w-2xl">
        <CardHeader>
          <CardTitle>
            <h2 className="font-display text-base font-semibold tracking-tight text-[var(--ink)]">{t("detail.audit")}</h2>
          </CardTitle>
        </CardHeader>
        <CardContent>
          {auditPending && <Skeleton className="h-20 w-full" />}
          {audit?.items.length === 0 && <p className="text-xs text-[var(--mut)]">{t("detail.noAudit")}</p>}
          <ul className="flex flex-col gap-1.5 text-xs">
            {audit?.items.map((entry) => (
              <li key={entry.id} className="flex flex-wrap items-center justify-between gap-2 border-b border-[var(--line)] py-1.5 last:border-b-0">
                <span className="font-medium text-[var(--ink)]">{entry.action}</span>
                <span className="font-mono text-[11px] text-[var(--mut)]">{new Date(entry.occurredAt).toLocaleString(locale)} · {entry.correlationId}</span>
              </li>
            ))}
          </ul>
        </CardContent>
      </Card>

      <Card className="max-w-2xl">
        <CardHeader>
          <CardTitle>
            <h2 className="font-display text-base font-semibold tracking-tight text-[var(--ink)]">{t("detail.roles")}</h2>
          </CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {user.roles.length === 0 && <p className="text-xs text-[var(--mut)]">{t("detail.noRoles")}</p>}
          <ul className="flex flex-col gap-2">
            {user.roles.map((role) => (
              <li key={role.id} className="flex items-center justify-between rounded-[var(--r-sm)] border border-[var(--bd)] bg-[var(--subtle)]/40 px-3 py-2">
                <span className="font-mono text-xs font-medium text-[var(--ink)]">{role.name}</span>
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
            <div className="flex flex-col sm:flex-row items-stretch sm:items-end gap-2 pt-2">
              <div className="flex flex-1 flex-col gap-1">
                <Label htmlFor="assign-role-select">{t("detail.assignRole")}</Label>
                <Select value={selectedRoleId} onValueChange={setSelectedRoleId}>
                  <SelectTrigger id="assign-role-select" className="w-full">
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
