import { inject } from "@angular/core";
import { Router, type CanActivateFn } from "@angular/router";

import { AuthService } from "./auth.service";

/**
 * Route guard for anything that needs a signed-in visitor. Unlike the
 * Next.js version of this app — where the refresh cookie is scoped to the
 * backend's own origin and never visible to a proxy/middleware file, so the
 * guard has to run client-side after the fact — an Angular route guard can
 * simply await hydration before resolving the route: `ensureHydrated()`
 * settles once the initial cookie-based session check has run, so there is
 * no flash of protected content before the redirect.
 */
export const authGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  await auth.ensureHydrated();
  if (auth.status() === "authenticated") return true;
  return router.parseUrl("/login");
};

/** Same as `authGuard`, plus a permission check — used to gate the whole admin area. Every admin API call is authoritatively re-checked by the backend's own 403 regardless of what this guard decides first. */
export function permissionGuard(permission: string): CanActivateFn {
  return async () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    await auth.ensureHydrated();
    if (auth.status() !== "authenticated") return router.parseUrl("/login");
    if (!auth.hasPermission(permission)) return router.parseUrl("/");
    return true;
  };
}
