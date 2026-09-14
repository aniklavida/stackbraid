import type { TranslocoScope } from "@jsverse/transloco";

export const ADMIN_LAYOUT_I18N_SCOPE: TranslocoScope = {
  scope: "admin",
  loader: {
    en: () => import("./messages/en.json").then((m) => m.default),
    es: () => import("./messages/es.json").then((m) => m.default),
  },
};
