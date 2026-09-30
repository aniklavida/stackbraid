import { useCallback, useEffect, useRef, useState } from "react";
import type { RealtimeMessage } from "@stackbraid/client-typescript/realtime";
import { apiBaseUrl } from "@/shared/config/env";
import { tokenStore } from "@/shared/auth/token-store";
import type { JobProgressFrame, JobStatus } from "../domain/job";
import { defaultJobsSource, type JobsSource } from "../data/jobs-realtime-source";

export type { JobsSource };
export type ConnectionStatus = "connected" | "paused";

export function useJobProgress(jobId: string, source: JobsSource = defaultJobsSource) {
  const [status, setStatus] = useState<JobStatus>("idle");
  const [progress, setProgress] = useState<number>(0);
  const [frames, setFrames] = useState<JobProgressFrame[]>([]);
  const [connectionStatus, setConnectionStatus] = useState<ConnectionStatus>("paused");
  const [retryCount, setRetryCount] = useState<number>(0);

  const subRef = useRef<{ start: () => void; close: () => void } | null>(null);
  const retryTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    const token = tokenStore.getAccessToken();
    if (!token || !jobId) {
      return;
    }

    let cancelled = false;

    source(
      apiBaseUrl(),
      token,
      jobId,
      (message: RealtimeMessage) => {
        if (cancelled) return;
        if (message.type === "job.progress" && message.jobId === jobId) {
          const frame: JobProgressFrame = {
            status: message.status as JobStatus,
            progress: message.progress,
            occurredAt: message.occurredAt,
          };
          setStatus(frame.status);
          setProgress(frame.progress);
          setFrames((prev) => [...prev, frame]);
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
          subRef.current = sub;
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
      if (subRef.current) {
        subRef.current.close();
        subRef.current = null;
      }
      if (retryTimerRef.current) {
        clearTimeout(retryTimerRef.current);
        retryTimerRef.current = null;
      }
    };
  }, [jobId, source, retryCount]);

  const startJob = useCallback(() => {
    if (subRef.current) {
      subRef.current.start();
    }
  }, []);

  return {
    status,
    progress,
    frames,
    connectionStatus,
    startJob,
  };
}
