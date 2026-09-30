export type RealtimeNotificationType = "user.deactivated" | "user.role_changed";

export interface RealtimeNotification {
  id: string;
  type: RealtimeNotificationType;
  occurredAt: string;
  detail: string;
}
