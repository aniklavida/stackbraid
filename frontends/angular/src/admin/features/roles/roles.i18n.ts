import type { TranslocoScope } from "@jsverse/transloco";

export const ROLES_I18N_SCOPE: TranslocoScope = {
  scope: "roles",
  loader: {
    en: () => import("./presentation/messages/en.json").then((m) => m.default),
    es: () => import("./presentation/messages/es.json").then((m) => m.default),
  },
};
