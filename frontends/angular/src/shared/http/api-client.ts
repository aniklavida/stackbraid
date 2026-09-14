import { client } from "@stackbraid/client-typescript/src/generated/client.gen";

import { apiBaseUrl } from "../config/env";
import { tokenStore } from "../auth/token-store";

/**
 * The one place this app configures the generated client — a base URL and
 * `credentials: "include"` so the httpOnly refresh cookie the backend sets
 * on `/v1/auth/*` is actually sent back on later requests. Everything else
 * in every feature calls the generated SDK functions directly; none of them
 * know which backend answers. Imported once, for its side effect, from
 * `app.config.ts` before anything else touches the client.
 */
client.setConfig({
  baseUrl: apiBaseUrl(),
  credentials: "include",
});

// Every outgoing request picks up the current access token, if any, from
// the in-memory session store. The store (not this interceptor) is what a
// component reads reactively — this only ever runs once per request.
client.interceptors.request.use((request) => {
  const token = tokenStore.getAccessToken();
  if (token) {
    request.headers.set("Authorization", `Bearer ${token}`);
  }
  return request;
});

export { client as apiClient };
