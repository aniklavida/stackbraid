"use client";

import { useLocale, useTranslations } from "next-intl";
import { Bell } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { useNotifications, type NotificationsSource } from "../application/use-notifications";

export function NotificationArea({ source }: { source?: NotificationsSource }) {
  const t = useTranslations("profile");
  const locale = useLocale();
  const { notifications, connectionStatus } = useNotifications(source);

  const dateFormatter = new Intl.DateTimeFormat(locale, {
    dateStyle: "short",
    timeStyle: "medium",
  });

  return (
    <Card className="w-full max-w-lg">
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-3">
        <CardTitle className="text-base font-semibold">
          <h2 className="font-display text-base font-semibold tracking-tight text-[var(--ink)]">{t("notificationsTitle")}</h2>
        </CardTitle>
        {connectionStatus === "connected" ? (
          <Badge variant="success">
            {t("notificationsLive")}
          </Badge>
        ) : (
          <Badge variant="warning" role="status">
            {t("notificationsPaused")}
          </Badge>
        )}
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        <Separator />
        {notifications.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-6 text-center text-xs text-[var(--mut)]">
            <Bell className="size-5 mb-1.5 text-[var(--faint)]" />
            <p>{t("notificationsEmpty")}</p>
          </div>
        ) : (
          <ul className="flex flex-col gap-2">
            {notifications.map((item) => (
              <li
                key={item.id}
                className="flex flex-col gap-1 rounded-[var(--r-sm)] border border-[var(--bd)] bg-[var(--subtle)]/40 p-2.5 text-xs"
              >
                <div className="flex items-center justify-between font-medium">
                  <span className="text-[var(--ink)]">
                    {item.type === "user.deactivated"
                      ? t("userDeactivated")
                      : `${t("userRoleChanged")}: ${item.detail}`}
                  </span>
                  <span className="font-mono text-[11px] text-[var(--mut)] font-normal">
                    {dateFormatter.format(new Date(item.occurredAt))}
                  </span>
                </div>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
