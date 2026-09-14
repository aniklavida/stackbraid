import type { TranslocoScope } from "@jsverse/transloco";

export const HOME_I18N_SCOPE: TranslocoScope = {
  scope: "home",
  loader: {
    en: () => import("./presentation/messages/en.json").then((m) => m.default),
    es: () => import("./presentation/messages/es.json").then((m) => m.default),
  },
};
