import { describe, expect, it, vi } from "vitest";

import { authRepository } from "../data/auth-repository";
import { registerAccount, signIn, signOut, ValidationFailed } from "./use-cases";

vi.mock("../data/auth-repository", () => ({
  authRepository: {
    register: vi.fn(),
    signIn: vi.fn(),
    signOut: vi.fn(),
  },
}));

const TOKENS = { accessToken: "access", refreshToken: "refresh", tokenType: "Bearer" as const, expiresAt: "2026-09-14T10:00:00Z" };
const USER = { id: "u1", email: "fatima@example.com", displayName: "Fatima" };

function fakeSession() {
  return {
    adoptSession: vi.fn().mockResolvedValue(USER),
    clearSession: vi.fn(),
  };
}

describe("registerAccount", () => {
  it("rejects locally before ever calling the network, when the form itself is invalid", async () => {
    const session = fakeSession();
    await expect(registerAccount({ email: "bad", password: "short", displayName: "" }, session)).rejects.toBeInstanceOf(ValidationFailed);
    expect(authRepository.register).not.toHaveBeenCalled();
  });

  it("registers, then signs in with the same credentials, then adopts the resulting session — registration alone issues no tokens", async () => {
    vi.mocked(authRepository.register).mockResolvedValue({ id: "u1" } as never);
    vi.mocked(authRepository.signIn).mockResolvedValue(TOKENS);
    const session = fakeSession();

    const values = { email: "fatima@example.com", password: "correct-horse-battery", displayName: "Fatima Rahman" };
    const user = await registerAccount(values, session);

    expect(authRepository.register).toHaveBeenCalledWith(values);
    expect(authRepository.signIn).toHaveBeenCalledWith({ email: values.email, password: values.password });
    expect(session.adoptSession).toHaveBeenCalledWith(TOKENS);
    expect(user).toEqual(USER);
  });
});

describe("signIn", () => {
  it("adopts the session returned by a successful login", async () => {
    vi.mocked(authRepository.signIn).mockResolvedValue(TOKENS);
    const session = fakeSession();

    await signIn({ email: "fatima@example.com", password: "correct-horse-battery" }, session);

    expect(session.adoptSession).toHaveBeenCalledWith(TOKENS);
  });
});

describe("signOut", () => {
  it("clears the local session even when the network call fails, so an offline logout still logs this tab out", async () => {
    vi.mocked(authRepository.signOut).mockRejectedValue(new Error("network down"));
    const session = fakeSession();

    await signOut(session);

    expect(session.clearSession).toHaveBeenCalled();
  });
});
