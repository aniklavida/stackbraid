import { describe, expect, it, vi } from "vitest";
import type { RealtimeMessage } from "@stackbraid/client-typescript/realtime";
import { tokenStore } from "../../../../shared/auth/token-store";
import { NotificationsService } from "./use-notifications.service";
import type { NotificationsSource } from "../data/notifications-source";

describe("NotificationsService", () => {
  it("connects to the notifications channel, receives live events, and pauses on disconnect", async () => {
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

    let capturedOnMessage!: (msg: RealtimeMessage) => void;
    let capturedOnClose!: () => void;

    const fakeSource: NotificationsSource = vi.fn().mockImplementation((_url, _token, onMessage, onClose) => {
      capturedOnMessage = onMessage;
      capturedOnClose = onClose;
      return Promise.resolve({ close: vi.fn() });
    });

    const service = new NotificationsService();
    service.start(fakeSource);

    await Promise.resolve();

    expect(service.connectionStatus()).toBe("connected");
    expect(service.notifications()).toHaveLength(0);

    // Receive user.role_changed
    capturedOnMessage({
      type: "user.role_changed",
      userId: "u-1",
      roles: [{ id: "r-admin", name: "admin", description: "Admin", permissions: [] }],
      occurredAt: "2026-09-30T10:00:00Z",
    });

    expect(service.notifications()).toHaveLength(1);
    expect(service.notifications()[0]?.type).toBe("user.role_changed");
    expect(service.notifications()[0]?.detail).toBe("admin");

    // Receive user.deactivated
    capturedOnMessage({
      type: "user.deactivated",
      userId: "u-1",
      occurredAt: "2026-09-30T10:05:00Z",
    });

    expect(service.notifications()).toHaveLength(2);
    expect(service.notifications()[0]?.type).toBe("user.deactivated");

    // Simulate disconnect
    capturedOnClose();
    expect(service.connectionStatus()).toBe("paused");

    service.stop();
  });
});
