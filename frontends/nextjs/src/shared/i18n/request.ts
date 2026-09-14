import { cookies } from "next/headers";
import { getRequestConfig } from "next-intl/server";

export const SUPPORTED_LOCALES = ["en", "es"] as const;
export type SupportedLocale = (typeof SUPPORTED_LOCALES)[number];
export const DEFAULT_LOCALE: SupportedLocale = "en";

function resolveLocale(value: string | undefined): SupportedLocale {
  return (SUPPORTED_LOCALES as readonly string[]).includes(value ?? "") ? (value as SupportedLocale) : DEFAULT_LOCALE;
}

/**
 * There is no route-based locale prefix (`/en/...`) here — a small skeleton
 * app does not need one, and it would collide with the `(web)`/`(admin)`
 * route groups the shell already relies on. The locale instead comes from
 * a plain `NEXT_LOCALE` cookie (`shared/i18n/LocaleSwitcher.tsx` sets it),
 * defaulting to English.
 *
 * Each namespace below is imported from the feature that owns it —
 * `shared/i18n/messages` holds only truly cross-cutting strings (nav
 * labels, the language switcher itself). This is what "every feature ships
 * its own translations" means in a framework whose i18n library expects
 * one message tree per request: the tree is assembled here, but no
 * feature's strings live anywhere but inside that feature's own folder.
 */
export default getRequestConfig(async () => {
  const cookieStore = await cookies();
  const locale = resolveLocale(cookieStore.get("NEXT_LOCALE")?.value);

  const [common, auth, home, profile, users, roles, admin] = await Promise.all([
    import(`./messages/${locale}.json`),
    import(`../../web/features/auth/presentation/messages/${locale}.json`),
    import(`../../web/features/home/presentation/messages/${locale}.json`),
    import(`../../web/features/profile/presentation/messages/${locale}.json`),
    import(`../../admin/features/users/presentation/messages/${locale}.json`),
    import(`../../admin/features/roles/presentation/messages/${locale}.json`),
    import(`../../admin/layout/messages/${locale}.json`),
  ]);

  return {
    locale,
    messages: {
      ...common.default,
      auth: auth.default,
      home: home.default,
      profile: profile.default,
      users: users.default,
      roles: roles.default,
      admin: admin.default,
    },
  };
});
