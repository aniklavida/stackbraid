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
import { useDeactivateUser, useUsers } from "../application/use-users";

export function UsersPage() {
  const t = useTranslations("users");
  const [filter, setFilter] = useState(DEFAULT_USERS_FILTER);
  const { data, isPending, isError, isPlaceholderData } = useUsers(filter);
  const deactivate = useDeactivateUser();

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
        <p className="text-muted-foreground">{t("subtitle")}</p>
      </div>

      <div className="flex flex-wrap gap-3">
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
          <SelectTrigger className="w-44">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t("statusAll")}</SelectItem>
            <SelectItem value="active">{t("statusActive")}</SelectItem>
            <SelectItem value="inactive">{t("statusInactive")}</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {isPending && <Skeleton className="h-64 w-full" />}
      {isError && (
        <p role="alert" className="text-destructive">
          {t("error")}
        </p>
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
                  <TableCell colSpan={5} className="text-center text-muted-foreground">
                    {t("empty")}
                  </TableCell>
                </TableRow>
              )}
              {data.items.map((user) => (
                <TableRow key={user.id}>
                  <TableCell className="font-medium">{user.displayName}</TableCell>
                  <TableCell className="text-muted-foreground">{user.email}</TableCell>
                  <TableCell>
                    <Badge variant={user.status === "active" ? "default" : "secondary"}>
                      {t(user.status === "active" ? "statusActive" : "statusInactive")}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <div className="flex flex-wrap gap-1">
                      {user.roles.map((role) => (
                        <Badge key={role.id} variant="outline">
                          {role.name}
                        </Badge>
                      ))}
                    </div>
                  </TableCell>
                  <TableCell className="flex justify-end gap-2">
                    <Button asChild variant="ghost" size="sm">
                      <Link href={`/admin/users/${user.id}`}>{t("actions.view")}</Link>
                    </Button>
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
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>

          <div className="flex items-center justify-between text-sm text-muted-foreground">
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
