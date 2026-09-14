import { Injectable } from "@angular/core";
import type { Translation, TranslocoLoader } from "@jsverse/transloco";

/**
 * The default (unscoped) translations — cross-cutting chrome only (nav
 * labels, the language switcher itself, generic actions). Everything with
 * business meaning ships from inside the feature that owns it, as an inline
 * scope loader next to that feature's own presentation code (see
 * `web/features/auth/auth.i18n.ts` and its siblings) — this file is what
 * "shared/ carries no business meaning" means for translations.
 */
@Injectable({ providedIn: "root" })
export class RootTranslocoLoader implements TranslocoLoader {
  getTranslation(lang: string): Promise<Translation> {
    return lang === "es"
      ? import("./messages/es.json").then((module) => module.default)
      : import("./messages/en.json").then((module) => module.default);
  }
}
