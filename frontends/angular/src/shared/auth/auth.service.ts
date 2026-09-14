import { Injectable, computed, signal } from "@angular/core";
import { getCurrentUser, refreshToken as refreshTokenRequest } from "@stackbraid/client-typescript";
import type { TokenPair, User } from "@stackbraid/client-typescript";

import { unwrap } from "../http/api-error";
import { tokenStore } from "./token-store";

export type AuthStatus = "loading" | "authenticated" | "anonymous";

/**
 * The session port a feature's use cases need — a use case orchestrates
 * this and its own repository, but never imports a component, so it stays
 * testable as plain async functions (see `web/features/auth/application`).
 */
export interface SessionPort {
  adoptSession(tokens: TokenPair): Promise<User>;
  clearSession(): void;
}

/**
 * Hydrates a session from the httpOnly refresh cookie on first load — the
 * whole reason that cookie exists is so a page refresh does not force a new
 * login. A cookie-less visitor (nothing set yet, or it expired) ends up
 * `anonymous`, silently; that is the ordinary logged-out state, not an
 * error. Root-provided: one instance for the whole app, same as the token
 * store it wraps.
 */
@Injectable({ providedIn: "root" })
export class AuthService implements SessionPort {
  private readonly refreshTimer = signal<ReturnType<typeof setTimeout> | null>(null);
  private hydrationStarted = false;
  private hydrationDone: Promise<void> | null = null;

  readonly status = computed<AuthStatus>(() => {
    const { hydrated, session } = tokenStore.state();
    if (!hydrated) return "loading";
    return session ? "authenticated" : "anonymous";
  });

  readonly user = computed<User | null>(() => tokenStore.state().session?.user ?? null);

  constructor() {
    // Started unconditionally at app startup — not only when a guard asks —
    // so an anonymous visitor's home page also leaves "loading" once the
    // cookie check settles, the same as every other route.
    void this.ensureHydrated();
  }

  /** Resolves once the initial cookie-based hydration attempt has settled — a route guard awaits this before deciding anything. */
  ensureHydrated(): Promise<void> {
    if (tokenStore.state().hydrated) return Promise.resolve();
    if (!this.hydrationStarted) {
      this.hydrationStarted = true;
      this.hydrationDone = this.hydrate();
    }
    return this.hydrationDone ?? Promise.resolve();
  }

  private async hydrate(): Promise<void> {
    try {
      const tokens = await unwrap(refreshTokenRequest());
      await this.adoptSession(tokens);
    } catch {
      tokenStore.clear();
    }
  }

  async adoptSession(tokens: TokenPair): Promise<User> {
    // Passed explicitly rather than relying on the request interceptor: the
    // token store has not been set yet, because this call is what confirms
    // the token pair is actually good for something before it is published.
    const user = await unwrap(getCurrentUser({ headers: { Authorization: `Bearer ${tokens.accessToken}` } }));
    tokenStore.setSession({ accessToken: tokens.accessToken, expiresAt: tokens.expiresAt, user });
    this.scheduleRenewal(tokens.expiresAt);
    return user;
  }

  clearSession(): void {
    const timer = this.refreshTimer();
    if (timer) clearTimeout(timer);
    tokenStore.clear();
  }

  hasPermission(permission: string): boolean {
    return this.user()?.roles.some((role) => role.permissions.includes(permission)) ?? false;
  }

  private scheduleRenewal(expiresAt: string): void {
    const previous = this.refreshTimer();
    if (previous) clearTimeout(previous);

    // Silently renew ~30s before the access token's own expiry rather than
    // waiting for a request to fail — a short-lived token (the contract's
    // default is 900s) would otherwise expire mid-session for no reason a
    // user should ever see.
    const msUntilExpiry = new Date(expiresAt).getTime() - Date.now() - 30_000;
    const timer = setTimeout(
      async () => {
        try {
          const tokens = await unwrap(refreshTokenRequest());
          await this.adoptSession(tokens);
        } catch {
          tokenStore.clear();
        }
      },
      Math.max(msUntilExpiry, 1_000),
    );
    this.refreshTimer.set(timer);
  }
}
