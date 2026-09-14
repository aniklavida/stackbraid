import { signal } from "@angular/core";
import type { User } from "@stackbraid/client-typescript";

/**
 * The in-memory half of the token-storage decision recorded for every
 * frontend: an httpOnly refresh cookie plus an access token that never
 * touches disk. A plain Angular signal is this app's own external store —
 * a fetch interceptor (`shared/http/api-client.ts`) reads `getAccessToken()`
 * outside any injection context, and `AuthService` reads the signal itself
 * for components to react to.
 */
export interface Session {
  accessToken: string;
  expiresAt: string;
  user: User;
}

export interface TokenStoreState {
  hydrated: boolean;
  session: Session | null;
}

const state = signal<TokenStoreState>({ hydrated: false, session: null });

export const tokenStore = {
  state: state.asReadonly(),

  getAccessToken(): string | null {
    return state().session?.accessToken ?? null;
  },

  setSession(next: Session): void {
    state.set({ hydrated: true, session: next });
  },

  clear(): void {
    state.set({ hydrated: true, session: null });
  },
};