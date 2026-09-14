import { Component } from "@angular/core";
import { RouterLink, RouterLinkActive, RouterOutlet } from "@angular/router";
import { TranslocoPipe } from "@jsverse/transloco";

import { LocaleSwitcherComponent } from "../../shared/i18n/locale-switcher";

/**
 * Unlike the Next.js version of this shell — which cannot gate this area in
 * middleware because the refresh cookie is scoped to the backend's own
 * origin — this component needs no `RequireAuth` wrapper of its own. The
 * `permissionGuard("users:read")` on the parent `/admin` route
 * (`app/app.routes.ts`) already keeps every route below from ever
 * activating without the role; every real admin call is still
 * authoritatively re-checked by the backend's own 403 regardless.
 */
@Component({
  selector: "app-admin-shell",
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet, TranslocoPipe, LocaleSwitcherComponent],
  templateUrl: "./admin-shell.html",
})
export class AdminShellComponent {}