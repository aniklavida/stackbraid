import { useEffect, useRef, useState } from "react";
import type { RealtimeMessage } from "@stackbraid/client-typescript/realtime";
import { apiBaseUrl } from "@/shared/config/env";
import { tokenStore } from "@/shared/auth/token-store";
import type { RealtimeNotification } from "../domain/notifications";
import { defaultNotificationsSource, type NotificationsSource } from "../data/notifications-source";

export type { NotificationsSource };
export type ConnectionStatus = "connected" | "paused";

export function useNotifications(source: NotificationsSource = defaultNotificationsSource) {
  const [notifications, setNotifications] = useState<RealtimeNotification[]>([]);
  const [connectionStatus, setConnectionStatus] = useState<ConnectionStatus>("paused");
  const [retryCount, setRetryCount] = useState<number>(0);
  const retryTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const activeSubRef = useRef<{ close: () => void } | null>(null);

  useEffect(() => {
    const token = tokenStore.getAccessToken();
    if (!token) {
      return;
    }

    let cancelled = false;

    source(
      apiBaseUrl(),
      token,
      (message: RealtimeMessage) => {
        if (cancelled) return;
        if (message.type === "user.deactivated") {
          setNotifications((prev) => [
            {
              id: `${message.occurredAt}-${Math.random().toString(36).slice(2, 7)}`,
              type: "user.deactivated",
              occurredAt: message.occurredAt,
              detail: message.userId,
            },
            ...prev,
          ]);
        } else if (message.type === "user.role_changed") {
          const roleNames = message.roles.map((r) => r.name).join(", ");
          setNotifications((prev) => [
            {
              id: `${message.occurredAt}-${Math.random().toString(36).slice(2, 7)}`,
              type: "user.role_changed",
              occurredAt: message.occurredAt,
              detail: roleNames,
            },
            ...prev,
          ]);
        }
      },
      () => {
        if (cancelled) return;
        setConnectionStatus("paused");
        if (retryTimerRef.current) clearTimeout(retryTimerRef.current);
        retryTimerRef.current = setTimeout(() => {
          if (!cancelled) setRetryCount((c) => c + 1);
        }, 3000);
      },
    )
      .then((sub) => {
        if (cancelled) {
          sub.close();
        } else {
          activeSubRef.current = sub;
          setConnectionStatus("connected");
        }
      })
      .catch(() => {
        if (!cancelled) {
          setConnectionStatus("paused");
          if (retryTimerRef.current) clearTimeout(retryTimerRef.current);
          retryTimerRef.current = setTimeout(() => {
            if (!cancelled) setRetryCount((c) => c + 1);
          }, 3000);
        }
      });

    return () => {
      cancelled = true;
      if (activeSubRef.current) {
        activeSubRef.current.close();
        activeSubRef.current = null;
      }
      if (retryTimerRef.current) {
        clearTimeout(retryTimerRef.current);
        retryTimerRef.current = null;
      }
    };
  }, [source, retryCount]);

  return {
    notifications,
    connectionStatus,
    clearNotifications: () => setNotifications([]),
  };
}
