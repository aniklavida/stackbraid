export type JobStatus = "idle" | "queued" | "running" | "succeeded" | "failed";

export interface JobProgressFrame {
  status: JobStatus;
  progress: number;
  occurredAt: string;
}

export function isJobFinished(status: JobStatus): boolean {
  return status === "succeeded" || status === "failed";
}
