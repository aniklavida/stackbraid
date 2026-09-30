"use client";

import Link from "next/link";
import { useState } from "react";
import { useTranslations } from "next-intl";
import { toast } from "sonner";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { DEFAULT_USERS_FILTER, type AccountStatus } from "../domain/user-filters";
import { useDeactivateUser, useRestoreUser, useUsers } from "../application/use-users";

export function UsersPage() {
  const t = useTranslations("users");
  const [filter, setFilter] = useState(DEFAULT_USERS_FILTER);
  const { data, isPending, isError, isPlaceholderData } = useUsers(filter);
  const deactivate = useDeactivateUser();
  const restore = useRestoreUser();

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="font-display text-2xl font-semibold tracking-tight text-[var(--ink)]">{t("title")}</h1>
        <p className="text-xs text-[var(--mut)]">{t("subtitle")}</p>
      </div>

      <div className="flex flex-wrap items-center gap-2.5">
        <Input
          placeholder={t("searchPlaceholder")}
          className="max-w-xs"
          defaultValue={filter.search}
          onChange={(event) => setFilter((prev) => ({ ...prev, page: 1, search: event.target.value }))}
        />
        <Select
          value={filter.status ?? "all"}
          onValueChange={(value) => setFilter((prev) => ({ ...prev, page: 1, status: value === "all" ? undefined : (value as AccountStatus) }))}
        >
          <SelectTrigger className="w-36">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t("statusAll")}</SelectItem>
            <SelectItem value="active">{t("statusActive")}</SelectItem>
            <SelectItem value="inactive">{t("statusInactive")}</SelectItem>
          </SelectContent>
        </Select>
        <Button
          variant="outline"
          size="default"
          onClick={() => setFilter((prev) => ({ ...prev, page: 1, includeDeleted: !prev.includeDeleted }))}
        >
          {t(filter.includeDeleted ? "actions.hideDeleted" : "actions.includeDeleted")}
        </Button>
      </div>

      {isPending && (
        <div className="flex flex-col gap-2">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-48 w-full" />
        </div>
      )}
      {isError && (
        <div role="alert" className="rounded-[var(--r-md)] border border-[var(--bad)]/25 bg-[var(--badbg)] p-4 text-xs text-[var(--bad)] font-medium">
          {t("error")}
        </div>
      )}

      {data && (
        <>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t("columns.name")}</TableHead>
                <TableHead>{t("columns.email")}</TableHead>
                <TableHead>{t("columns.status")}</TableHead>
                <TableHead>{t("columns.roles")}</TableHead>
                <TableHead className="text-right">{t("columns.actions")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={5} className="py-8 text-center text-xs text-[var(--mut)]">
                    {t("empty")}
                  </TableCell>
                </TableRow>
              )}
              {data.items.map((user) => (
                <TableRow key={user.id}>
                  <TableCell className="font-medium text-[var(--ink)]">{user.displayName}</TableCell>
                  <TableCell className="font-mono text-xs text-[var(--mut)]">{user.email}</TableCell>
                  <TableCell>
                    <Badge variant={user.deletedAt ? "destructive" : user.status === "active" ? "success" : "secondary"}>
                      {t(user.deletedAt ? "statusDeleted" : user.status === "active" ? "statusActive" : "statusInactive")}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <div className="flex flex-wrap gap-1">
                      {user.roles.map((role) => (
                        <Badge key={role.id} variant="outline" className="font-mono text-xs">
                          {role.name}
                        </Badge>
                      ))}
                    </div>
                  </TableCell>
                  <TableCell className="flex justify-end gap-2">
                    <Button asChild variant="ghost" size="sm">
                      <Link href={`/admin/users/${user.id}`}>{t("actions.view")}</Link>
                    </Button>
                    {user.deletedAt ? (
                      <Button
                        variant="outline"
                        size="sm"
                        disabled={restore.isPending}
                        onClick={() => restore.mutate(user.id, { onSuccess: () => toast.success(t("actions.restored", { name: user.displayName })) })}
                      >
                        {t("actions.restore")}
                      </Button>
                    ) : (
                      <Button
                        variant="outline"
                        size="sm"
                        disabled={user.status === "inactive" || deactivate.isPending}
                        onClick={() =>
                          deactivate.mutate(user.id, {
                            onSuccess: () => toast.success(t("actions.deactivated", { name: user.displayName })),
                          })
                        }
                      >
                        {t("actions.deactivate")}
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>

          <div className="flex flex-wrap items-center justify-between gap-3 text-xs text-[var(--mut)]">
            <span>{t("pagination.summary", { page: data.page, totalPages: Math.max(data.totalPages, 1), totalItems: data.totalItems })}</span>
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={filter.page <= 1}
                onClick={() => setFilter((prev) => ({ ...prev, page: prev.page - 1 }))}
              >
                {t("pagination.previous")}
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled={isPlaceholderData || filter.page >= data.totalPages}
                onClick={() => setFilter((prev) => ({ ...prev, page: prev.page + 1 }))}
              >
                {t("pagination.next")}
              </Button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
