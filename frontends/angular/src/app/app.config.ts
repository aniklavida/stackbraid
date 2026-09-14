import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from "@angular/core";
import { provideRouter } from "@angular/router";
import { provideAnimationsAsync } from "@angular/platform-browser/animations/async";
import { provideTanStackQuery, QueryClient } from "@tanstack/angular-query-experimental";
import { provideTransloco } from "@jsverse/transloco";

import { storedLocale } from "../shared/i18n/locale-switcher";
import { RootTranslocoLoader } from "../shared/i18n/root-transloco-loader";
import { routes } from "./app.routes";

// Imported once, for its side effect, before anything else touches the
// generated client — see the module itself for what it configures.
import "../shared/http/api-client";

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes),
    provideAnimationsAsync(),
    provideTanStackQuery(new QueryClient()),
    provideTransloco({
      config: {
        availableLangs: ["en", "es"],
        defaultLang: storedLocale(),
        reRenderOnLangChange: true,
        prodMode: true,
      },
      loader: RootTranslocoLoader,
    }),
  ],
};
