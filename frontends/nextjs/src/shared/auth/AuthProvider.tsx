"use client";

import { createContext, useContext, useEffect, useRef, useSyncExternalStore, type ReactNode } from "react";
import { getCurrentUser, refreshToken as refreshTokenRequest } from "@stackbraid/client-typescript";
import type { TokenPair, User } from "@stackbraid/client-typescript";

import { unwrap } from "../http/api-error";
import { tokenStore } from "./token-store";

export type AuthStatus = "loading" | "authenticated" | "anonymous";

export interface AuthContextValue {
  status: AuthStatus;
  user: User | null;
  /** Commits a token pair this component did not fetch itself (login/register/refresh all return one) — fetches the profile and publishes the session. */
  adoptSession(tokens: TokenPair): Promise<User>;
  /** Drops the in-memory session. Does not itself call `/v1/auth/logout` — callers that also need to revoke the refresh token do that first. */
  clearSession(): void;
  hasPermission(permission: string): boolean;
}

const AuthContext = createContext<AuthContextValue | null>(null);

async function adoptSession(tokens: TokenPair): Promise<User> {
  // Passed explicitly rather than relying on the request interceptor: the
  // token store has not been set yet, because this call is what confirms
  // the token pair is actually good for something before it is published.
  const user = await unwrap(getCurrentUser({ headers: { Authorization: `Bearer ${tokens.accessToken}` } }));
  tokenStore.setSession({ accessToken: tokens.accessToken, expiresAt: tokens.expiresAt, user });
  return user;
}

/**
 * Hydrates a session from the httpOnly refresh cookie on first load — the
 * whole reason that cookie exists is so a page refresh does not force a new
 * login. A cookie-less visitor (nothing set yet, or it expired) ends up
 * `anonymous`, silently; that is the ordinary logged-out state, not an
 * error.
 */
const serverSnapshot = { hydrated: false, session: null };

export function AuthProvider({ children }: { children: ReactNode }) {
  const { hydrated, session } = useSyncExternalStore(tokenStore.subscribe, tokenStore.getSnapshot, () => serverSnapshot);
  const refreshTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (tokenStore.getSnapshot().hydrated) return;
    let cancelled = false;
    (async () => {
      try {
        const tokens = await unwrap(refreshTokenRequest());
        if (!cancelled) await adoptSession(tokens);
      } catch {
        if (!cancelled) tokenStore.clear();
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (refreshTimer.current) clearTimeout(refreshTimer.current);
    if (!session) return;

    // Silently renew ~30s before the access token's own expiry rather than
    // waiting for a request to fail — a short-lived token (the contract's
    // default is 900s) would otherwise expire mid-session for no reason a
    // user should ever see.
    const msUntilExpiry = new Date(session.expiresAt).getTime() - Date.now() - 30_000;
    refreshTimer.current = setTimeout(
      async () => {
        try {
          const tokens = await unwrap(refreshTokenRequest());
          await adoptSession(tokens);
        } catch {
          tokenStore.clear();
        }
      },
      Math.max(msUntilExpiry, 1_000),
    );
    return () => {
      if (refreshTimer.current) clearTimeout(refreshTimer.current);
    };
  }, [session]);

  const value: AuthContextValue = {
    status: !hydrated ? "loading" : session ? "authenticated" : "anonymous",
    user: session?.user ?? null,
    adoptSession,
    clearSession: () => tokenStore.clear(),
    hasPermission: (permission: string) =>
      session?.user?.roles.some((role) => role.permissions.includes(permission)) ?? false,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth() must be used inside <AuthProvider>.");
  return ctx;
}
