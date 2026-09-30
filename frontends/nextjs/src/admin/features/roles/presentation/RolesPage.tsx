"use client";

import { useTranslations } from "next-intl";

import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { useRoles } from "../application/use-roles";

export function RolesPage() {
  const t = useTranslations("roles");
  const { data: roles, isPending, isError } = useRoles();

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="font-display text-2xl font-semibold tracking-tight text-[var(--ink)]">{t("title")}</h1>
        <p className="text-xs text-[var(--mut)]">{t("subtitle")}</p>
      </div>

      {isPending && (
        <div className="flex flex-col gap-2">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-36 w-full" />
        </div>
      )}
      {isError && (
        <div role="alert" className="rounded-[var(--r-md)] border border-[var(--bad)]/25 bg-[var(--badbg)] p-4 text-xs text-[var(--bad)] font-medium">
          {t("error")}
        </div>
      )}

      {roles && (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("columns.name")}</TableHead>
              <TableHead>{t("columns.description")}</TableHead>
              <TableHead>{t("columns.permissions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {roles.map((role) => (
              <TableRow key={role.id}>
                <TableCell className="font-medium text-[var(--ink)]">{role.name}</TableCell>
                <TableCell className="text-xs text-[var(--mut)]">{role.description}</TableCell>
                <TableCell>
                  <div className="flex flex-wrap gap-1">
                    {role.permissions.map((permission) => (
                      <Badge key={permission} variant="outline" className="font-mono text-xs">
                        {permission}
                      </Badge>
                    ))}
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
}
