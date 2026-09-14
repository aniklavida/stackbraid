import type { TranslocoScope } from "@jsverse/transloco";

/**
 * This feature's own translations, loaded only when a route inside it is
 * actually visited — registered as this scope's inline loader on the
 * `auth` route group (`app/app.routes.ts`), never in a central messages
 * file. `errors` uses the same `code`-shaped keys the backend's own Problem
 * envelope sends (`IDENTITY.INVALID_CREDENTIALS` etc.), read dot-path style.
 */
export const AUTH_I18N_SCOPE: TranslocoScope = {
  scope: "auth",
  loader: {
    en: () => import("./presentation/messages/en.json").then((m) => m.default),
    es: () => import("./presentation/messages/es.json").then((m) => m.default),
  },
};
