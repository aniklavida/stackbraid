import { Injectable, signal } from "@angular/core";
import type { RealtimeMessage } from "@stackbraid/client-typescript/realtime";
import { apiBaseUrl } from "../../../../shared/config/env";
import { tokenStore } from "../../../../shared/auth/token-store";
import type { RealtimeNotification } from "../domain/profile";
import { defaultNotificationsSource, type NotificationsSource } from "../data/notifications-source";

export type { NotificationsSource };
export type ConnectionStatus = "connected" | "paused";

@Injectable({ providedIn: "root" })
export class NotificationsService {
  readonly notifications = signal<RealtimeNotification[]>([]);
  readonly connectionStatus = signal<ConnectionStatus>("paused");

  private activeSub: { close: () => void } | null = null;
  private retryTimer: ReturnType<typeof setTimeout> | null = null;
  private stopped = false;
  private currentSource: NotificationsSource = defaultNotificationsSource;

  start(source: NotificationsSource = defaultNotificationsSource): void {
    this.stopped = false;
    this.currentSource = source;
    this.connect();
  }

  stop(): void {
    this.stopped = true;
    if (this.retryTimer) {
      clearTimeout(this.retryTimer);
      this.retryTimer = null;
    }
    if (this.activeSub) {
      this.activeSub.close();
      this.activeSub = null;
    }
    this.connectionStatus.set("paused");
  }

  private connect(): void {
    if (this.stopped) return;

    const token = tokenStore.getAccessToken();
    if (!token) {
      this.connectionStatus.set("paused");
      return;
    }

    this.currentSource(
      apiBaseUrl(),
      token,
      (message: RealtimeMessage) => {
        if (message.type === "user.deactivated") {
          const item: RealtimeNotification = {
            id: `${message.occurredAt}-${Math.random().toString(36).slice(2, 7)}`,
            type: "user.deactivated",
            occurredAt: message.occurredAt,
            detail: message.userId,
          };
          this.notifications.update((prev) => [item, ...prev]);
        } else if (message.type === "user.role_changed") {
          const roleNames = message.roles.map((r) => r.name).join(", ");
          const item: RealtimeNotification = {
            id: `${message.occurredAt}-${Math.random().toString(36).slice(2, 7)}`,
            type: "user.role_changed",
            occurredAt: message.occurredAt,
            detail: roleNames,
          };
          this.notifications.update((prev) => [item, ...prev]);
        }
      },
      () => {
        if (this.stopped) return;
        this.connectionStatus.set("paused");
        this.scheduleRetry();
      },
    )
      .then((sub) => {
        if (this.stopped) {
          sub.close();
        } else {
          this.activeSub = sub;
          this.connectionStatus.set("connected");
        }
      })
      .catch(() => {
        if (!this.stopped) {
          this.connectionStatus.set("paused");
          this.scheduleRetry();
        }
      });
  }

  private scheduleRetry(): void {
    if (this.retryTimer) clearTimeout(this.retryTimer);
    this.retryTimer = setTimeout(() => {
      if (!this.stopped) this.connect();
    }, 3000);
  }
}
