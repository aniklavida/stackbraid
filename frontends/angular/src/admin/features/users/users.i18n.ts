import type { TranslocoScope } from "@jsverse/transloco";

export const USERS_I18N_SCOPE: TranslocoScope = {
  scope: "users",
  loader: {
    en: () => import("./presentation/messages/en.json").then((m) => m.default),
    es: () => import("./presentation/messages/es.json").then((m) => m.default),
  },
};
