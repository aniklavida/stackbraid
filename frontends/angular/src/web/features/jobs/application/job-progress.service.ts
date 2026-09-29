import { Injectable, signal } from "@angular/core";
import type { RealtimeMessage } from "@stackbraid/client-typescript/realtime";
import { apiBaseUrl } from "../../../../shared/config/env";
import { tokenStore } from "../../../../shared/auth/token-store";
import type { JobProgressFrame, JobStatus } from "../domain/job";
import { defaultJobsSource, type JobsSource } from "../data/jobs-realtime.service";

export type { JobsSource };
export type ConnectionStatus = "connected" | "paused";

@Injectable()
export class JobProgressService {
  readonly status = signal<JobStatus>("idle");
  readonly progress = signal<number>(0);
  readonly frames = signal<JobProgressFrame[]>([]);
  readonly connectionStatus = signal<ConnectionStatus>("paused");

  private activeSub: { start: () => void; close: () => void } | null = null;
  private retryTimer: ReturnType<typeof setTimeout> | null = null;
  private stopped = false;
  private currentJobId = "";
  private currentSource: JobsSource = defaultJobsSource;

  start(jobId: string, source: JobsSource = defaultJobsSource): void {
    this.stop();
    this.stopped = false;
    this.currentJobId = jobId;
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

  startJob(): void {
    if (this.activeSub) {
      this.activeSub.start();
    }
  }

  private connect(): void {
    if (this.stopped || !this.currentJobId) return;

    const token = tokenStore.getAccessToken();
    if (!token) {
      this.connectionStatus.set("paused");
      return;
    }

    const targetJobId = this.currentJobId;

    this.currentSource(
      apiBaseUrl(),
      token,
      targetJobId,
      (message: RealtimeMessage) => {
        if (message.type === "job.progress" && message.jobId === targetJobId) {
          const frame: JobProgressFrame = {
            status: message.status as JobStatus,
            progress: message.progress,
            occurredAt: message.occurredAt,
          };
          this.status.set(frame.status);
          this.progress.set(frame.progress);
          this.frames.update((prev) => [...prev, frame]);
        }
      },
      () => {
        if (this.stopped) return;
        this.connectionStatus.set("paused");
        this.scheduleRetry();
      },
    )
      .then((sub) => {
        if (this.stopped || this.currentJobId !== targetJobId) {
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
