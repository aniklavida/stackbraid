/**
 * Cross-cutting configuration — no business meaning of its own.
 *
 * Unlike a build-time env var, this reads a value injected by `public/env.js`
 * (loaded from `index.html` before the app bundle) so the same compiled build
 * can be pointed at the .NET backend or the Python backend by editing one
 * static file, never the source: `apiBaseUrl()` is the one place every
 * generated-client call goes through, via `shared/http/api-client.ts`.
 */
interface StackBraidRuntimeEnv {
  apiBaseUrl?: string;
}

declare global {
  interface Window {
    __STACKBRAID_ENV__?: StackBraidRuntimeEnv;
  }
}

export function apiBaseUrl(): string {
  return globalThis.window?.__STACKBRAID_ENV__?.apiBaseUrl ?? "http://127.0.0.1:8080";
}