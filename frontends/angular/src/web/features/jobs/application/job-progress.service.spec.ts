import { describe, expect, it, vi } from "vitest";
import type { RealtimeMessage } from "@stackbraid/client-typescript/realtime";
import { tokenStore } from "../../../../shared/auth/token-store";
import { JobProgressService } from "./job-progress.service";
import type { JobsSource } from "../data/jobs-realtime.service";

describe("JobProgressService", () => {
  it("subscribes to job channel, starts the job, and receives live progress frames", async () => {
    tokenStore.setSession({
      accessToken: "test-token",
      expiresAt: "2026-12-31T23:59:59Z",
      user: {
        id: "u-1",
        email: "user@example.com",
        displayName: "User",
        status: "active",
        roles: [],
        createdAt: "2026-01-01T00:00:00Z",
        updatedAt: "2026-01-01T00:00:00Z",
        deletedAt: null,
      },
    });

    const jobId = "job-angular-test";
    let capturedOnMessage!: (msg: RealtimeMessage) => void;
    let capturedOnClose!: () => void;
    const startMock = vi.fn();
    const closeMock = vi.fn();

    const fakeSource: JobsSource = vi.fn().mockImplementation((_url, _token, _jobId, onMessage, onClose) => {
      capturedOnMessage = onMessage;
      capturedOnClose = onClose;
      return Promise.resolve({ start: startMock, close: closeMock });
    });

    const service = new JobProgressService();
    service.start(jobId, fakeSource);

    await Promise.resolve();

    expect(service.connectionStatus()).toBe("connected");
    expect(service.status()).toBe("idle");
    expect(service.progress()).toBe(0);

    // Start job
    service.startJob();
    expect(startMock).toHaveBeenCalled();

    // Frame 1: queued 0%
    capturedOnMessage({
      type: "job.progress",
      jobId,
      status: "queued",
      progress: 0,
      occurredAt: "2026-09-30T10:00:00Z",
    });

    expect(service.status()).toBe("queued");
    expect(service.progress()).toBe(0);
    expect(service.frames()).toHaveLength(1);

    // Frame 2: running 40%
    capturedOnMessage({
      type: "job.progress",
      jobId,
      status: "running",
      progress: 40,
      occurredAt: "2026-09-30T10:00:01Z",
    });

    expect(service.status()).toBe("running");
    expect(service.progress()).toBe(40);

    // Frame 3: succeeded 100%
    capturedOnMessage({
      type: "job.progress",
      jobId,
      status: "succeeded",
      progress: 100,
      occurredAt: "2026-09-30T10:00:02Z",
    });

    expect(service.status()).toBe("succeeded");
    expect(service.progress()).toBe(100);
    expect(service.frames()).toHaveLength(3);

    // Disconnect
    capturedOnClose();
    expect(service.connectionStatus()).toBe("paused");

    service.stop();
  });
});
