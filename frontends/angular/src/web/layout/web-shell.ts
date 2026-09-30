import { Component, inject } from "@angular/core";
import { Router, RouterLink, RouterOutlet } from "@angular/router";
import { TranslocoPipe } from "@jsverse/transloco";

import { AuthService } from "../../shared/auth/auth.service";
import { LocaleSwitcherComponent } from "../../shared/i18n/locale-switcher";
import { ThemeToggleComponent } from "../../shared/theme/theme-toggle.component";
import { signOut } from "../features/auth";

@Component({
  selector: "app-web-shell",
  standalone: true,
  imports: [RouterLink, RouterOutlet, TranslocoPipe, LocaleSwitcherComponent, ThemeToggleComponent],
  templateUrl: "./web-shell.html",
})
export class WebShellComponent {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected async logout(): Promise<void> {
    await signOut(this.auth);
    await this.router.navigateByUrl("/");
  }
}