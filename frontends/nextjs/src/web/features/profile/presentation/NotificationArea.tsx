"use client";

import { useLocale, useTranslations } from "next-intl";
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
          <h2>{t("notificationsTitle")}</h2>
        </CardTitle>
        {connectionStatus === "connected" ? (
          <Badge variant="outline" className="border-emerald-500 bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300">
            {t("notificationsLive")}
          </Badge>
        ) : (
          <Badge variant="outline" className="border-amber-500 bg-amber-50 text-amber-700 dark:bg-amber-950 dark:text-amber-300" role="status">
            {t("notificationsPaused")}
          </Badge>
        )}
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        <Separator />
        {notifications.length === 0 ? (
          <p className="text-sm text-muted-foreground">{t("notificationsEmpty")}</p>
        ) : (
          <ul className="flex flex-col gap-2">
            {notifications.map((item) => (
              <li
                key={item.id}
                className="flex flex-col gap-0.5 rounded-md border p-2 text-sm"
              >
                <div className="flex items-center justify-between font-medium">
                  <span>
                    {item.type === "user.deactivated"
                      ? t("userDeactivated")
                      : `${t("userRoleChanged")}: ${item.detail}`}
                  </span>
                  <span className="text-xs text-muted-foreground font-normal">
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
