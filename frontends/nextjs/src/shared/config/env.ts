/**
 * Cross-cutting configuration — no business meaning of its own.
 *
 * `NEXT_PUBLIC_API_BASE_URL` is the one setting that decides which backend
 * this app talks to. Every generated-client call reads it through
 * `shared/http`; nothing else in the app hard-codes a host, so pointing the
 * same build at a different backend server never touches code.
 */
export function apiBaseUrl(): string {
  return process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://127.0.0.1:8080";
}
