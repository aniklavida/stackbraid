import { describe, expect, it, vi } from "vitest";
import { renderHook, act } from "@testing-library/react";
import type { RealtimeMessage } from "@stackbraid/client-typescript/realtime";
import { tokenStore } from "@/shared/auth/token-store";
import { useNotifications } from "./use-notifications";
import type { NotificationsSource } from "../data/notifications-source";

describe("useNotifications", () => {
  it("connects to the realtime notifications channel and receives events live", async () => {
    tokenStore.setSession({
      accessToken: "test-token",
      expiresAt: "2026-12-31T23:59:59Z",
      user: { id: "u-1", email: "user@example.com", displayName: "User", status: "active", roles: [], createdAt: "2026-01-01T00:00:00Z", updatedAt: "2026-01-01T00:00:00Z", deletedAt: null },
    });

    let messageHandler: ((msg: RealtimeMessage) => void) | null = null;
    let closeHandler: (() => void) | null = null;

    const fakeSource: NotificationsSource = vi.fn().mockImplementation((_url, _token, onMessage, onClose) => {
      messageHandler = onMessage;
      closeHandler = onClose;
      return Promise.resolve({ close: vi.fn() });
    });

    const { result } = renderHook(() => useNotifications(fakeSource));

    // Wait for promise resolution
    await act(async () => {
      await Promise.resolve();
    });

    expect(result.current.connectionStatus).toBe("connected");
    expect(result.current.notifications).toHaveLength(0);

    // Simulate user.role_changed event
    act(() => {
      messageHandler?.({
        type: "user.role_changed",
        userId: "u-1",
        roles: [{ id: "r-admin", name: "admin", description: "Admin", permissions: [] }],
        occurredAt: "2026-09-30T10:00:00Z",
      });
    });

    expect(result.current.notifications).toHaveLength(1);
    expect(result.current.notifications[0]!.type).toBe("user.role_changed");
    expect(result.current.notifications[0]!.detail).toBe("admin");

    // Simulate user.deactivated event
    act(() => {
      messageHandler?.({
        type: "user.deactivated",
        userId: "u-1",
        occurredAt: "2026-09-30T10:05:00Z",
      });
    });

    expect(result.current.notifications).toHaveLength(2);
    expect(result.current.notifications[0]!.type).toBe("user.deactivated");

    // Simulate disconnect: connection closes
    act(() => {
      closeHandler?.();
    });

    expect(result.current.connectionStatus).toBe("paused");
  });
});
