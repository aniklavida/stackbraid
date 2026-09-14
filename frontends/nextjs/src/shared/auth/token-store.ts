import type { User } from "@stackbraid/client-typescript";

/**
 * The in-memory half of the token-storage decision recorded for every
 * frontend: an httpOnly refresh cookie plus an access token that never
 * touches disk. This module only ever runs in the browser — each tab gets
 * its own JS module instance, so there is nothing here for one visitor to
 * read from another.
 *
 * A minimal external store (subscribe/getSnapshot) rather than React state
 * living in one component, so both `useSyncExternalStore` (inside React)
 * and the plain `getAccessToken()` a fetch interceptor needs (outside
 * React) read the same value. `getSnapshot()` always returns the same
 * object reference until something actually changes, which is what
 * `useSyncExternalStore` requires to avoid re-rendering forever.
 */
export interface Session {
  accessToken: string;
  expiresAt: string;
  user: User;
}

interface StoreState {
  hydrated: boolean;
  session: Session | null;
}

let state: StoreState = { hydrated: false, session: null };
const listeners = new Set<() => void>();

function emit(): void {
  for (const listener of listeners) listener();
}

export const tokenStore = {
  subscribe(listener: () => void): () => void {
    listeners.add(listener);
    return () => listeners.delete(listener);
  },

  getSnapshot(): StoreState {
    return state;
  },

  getAccessToken(): string | null {
    return state.session?.accessToken ?? null;
  },

  setSession(next: Session): void {
    state = { hydrated: true, session: next };
    emit();
  },

  clear(): void {
    state = { hydrated: true, session: null };
    emit();
  },
};
