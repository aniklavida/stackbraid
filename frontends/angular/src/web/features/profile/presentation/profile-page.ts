import { Component, inject } from "@angular/core";
import { MatCardModule } from "@angular/material/card";
import { MatChipsModule } from "@angular/material/chips";
import { MatDividerModule } from "@angular/material/divider";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { TranslocoPipe, TranslocoService } from "@jsverse/transloco";

import { useOwnProfile } from "../application/use-own-profile";
import { roleNames } from "../domain/profile";

function initials(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]!.toUpperCase())
    .join("");
}

@Component({
  selector: "app-profile-page",
  standalone: true,
  imports: [MatCardModule, MatChipsModule, MatDividerModule, MatProgressBarModule, TranslocoPipe],
  templateUrl: "./profile-page.html",
})
export class ProfilePageComponent {
  private readonly transloco = inject(TranslocoService);
  protected readonly profile = useOwnProfile();
  protected readonly initials = initials;
  protected readonly roleNames = roleNames;

  protected formatDate(value: string): string {
    return new Intl.DateTimeFormat(this.transloco.getActiveLang(), { dateStyle: "long" }).format(new Date(value));
  }
}