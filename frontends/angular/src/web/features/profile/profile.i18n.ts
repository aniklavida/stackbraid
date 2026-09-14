import type { TranslocoScope } from "@jsverse/transloco";

export const PROFILE_I18N_SCOPE: TranslocoScope = {
  scope: "profile",
  loader: {
    en: () => import("./presentation/messages/en.json").then((m) => m.default),
    es: () => import("./presentation/messages/es.json").then((m) => m.default),
  },
};
