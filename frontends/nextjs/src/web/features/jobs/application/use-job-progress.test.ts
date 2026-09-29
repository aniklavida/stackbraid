import { describe, expect, it, vi } from "vitest";
import { renderHook, act } from "@testing-library/react";
import type { RealtimeMessage } from "@stackbraid/client-typescript/realtime";
import { tokenStore } from "@/shared/auth/token-store";
import { useJobProgress } from "./use-job-progress";
import type { JobsSource } from "../data/jobs-realtime-source";

describe("useJobProgress", () => {
  it("subscribes to job channel, starts the job, and receives live progress frames", async () => {
    tokenStore.setSession({
      accessToken: "test-token",
      expiresAt: "2026-12-31T23:59:59Z",
      user: { id: "u-1", email: "user@example.com", displayName: "User", status: "active", roles: [], createdAt: "2026-01-01T00:00:00Z", updatedAt: "2026-01-01T00:00:00Z", deletedAt: null },
    });

    const jobId = "job-12345";
    let messageHandler: ((msg: RealtimeMessage) => void) | null = null;
    let closeHandler: (() => void) | null = null;
    const startMock = vi.fn();
    const closeMock = vi.fn();

    const fakeSource: JobsSource = vi.fn().mockImplementation((_url, _token, _jobId, onMessage, onClose) => {
      messageHandler = onMessage;
      closeHandler = onClose;
      return Promise.resolve({ start: startMock, close: closeMock });
    });

    const { result } = renderHook(() => useJobProgress(jobId, fakeSource));

    await act(async () => {
      await Promise.resolve();
    });

    expect(result.current.connectionStatus).toBe("connected");
    expect(result.current.status).toBe("idle");
    expect(result.current.progress).toBe(0);

    // Call startJob
    act(() => {
      result.current.startJob();
    });
    expect(startMock).toHaveBeenCalled();

    // Receive first frame: queued 0%
    act(() => {
      messageHandler?.({
        type: "job.progress",
        jobId,
        status: "queued",
        progress: 0,
        occurredAt: "2026-09-30T10:00:00Z",
      });
    });

    expect(result.current.status).toBe("queued");
    expect(result.current.progress).toBe(0);
    expect(result.current.frames).toHaveLength(1);

    // Receive running 40%
    act(() => {
      messageHandler?.({
        type: "job.progress",
        jobId,
        status: "running",
        progress: 40,
        occurredAt: "2026-09-30T10:00:01Z",
      });
    });

    expect(result.current.status).toBe("running");
    expect(result.current.progress).toBe(40);

    // Receive succeeded 100%
    act(() => {
      messageHandler?.({
        type: "job.progress",
        jobId,
        status: "succeeded",
        progress: 100,
        occurredAt: "2026-09-30T10:00:02Z",
      });
    });

    expect(result.current.status).toBe("succeeded");
    expect(result.current.progress).toBe(100);
    expect(result.current.frames).toHaveLength(3);

    // Simulate disconnect: connection closes
    act(() => {
      closeHandler?.();
    });

    expect(result.current.connectionStatus).toBe("paused");
  });
});
