import { connectRealtimeChannel, type RealtimeMessage } from "@stackbraid/client-typescript/realtime";

export interface NotificationsSubscription {
  close: () => void;
}

export type NotificationsSource = (
  baseUrl: string,
  accessToken: string,
  onMessage: (msg: RealtimeMessage) => void,
  onClose: () => void,
) => Promise<NotificationsSubscription>;

export const defaultNotificationsSource: NotificationsSource = async (
  baseUrl,
  accessToken,
  onMessage,
  onClose,
) => {
  const conn = await connectRealtimeChannel(baseUrl, "notifications", accessToken);
  conn.onMessage(onMessage);
  conn.onClose(onClose);
  return {
    close: () => conn.close(),
  };
};
