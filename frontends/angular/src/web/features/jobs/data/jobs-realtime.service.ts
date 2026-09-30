import { connectRealtimeChannel, type RealtimeMessage } from "@stackbraid/client-typescript/realtime";

export interface JobsSubscription {
  start: () => void;
  close: () => void;
}

export type JobsSource = (
  baseUrl: string,
  accessToken: string,
  jobId: string,
  onMessage: (msg: RealtimeMessage) => void,
  onClose: () => void,
) => Promise<JobsSubscription>;

export const defaultJobsSource: JobsSource = async (
  baseUrl,
  accessToken,
  jobId,
  onMessage,
  onClose,
) => {
  const conn = await connectRealtimeChannel(baseUrl, "jobs", accessToken, jobId);
  conn.onMessage(onMessage);
  conn.onClose(onClose);
  return {
    start: () => conn.start(),
    close: () => conn.close(),
  };
};
