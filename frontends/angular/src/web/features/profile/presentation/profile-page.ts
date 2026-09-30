import { Component, OnDestroy, inject } from "@angular/core";
import { MatCardModule } from "@angular/material/card";
import { MatChipsModule } from "@angular/material/chips";
import { MatDividerModule } from "@angular/material/divider";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { TranslocoPipe, TranslocoService } from "@jsverse/transloco";

import { useOwnProfile } from "../application/use-own-profile";
import { NotificationsService } from "../application/use-notifications.service";
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
export class ProfilePageComponent implements OnDestroy {
  private readonly transloco = inject(TranslocoService);
  protected readonly profile = useOwnProfile();
  protected readonly notificationsService = inject(NotificationsService);
  protected readonly initials = initials;
  protected readonly roleNames = roleNames;

  constructor() {
    this.notificationsService.start();
  }

  ngOnDestroy(): void {
    this.notificationsService.stop();
  }

  protected formatDate(value: string): string {
    return new Intl.DateTimeFormat(this.transloco.getActiveLang(), { dateStyle: "long" }).format(new Date(value));
  }

  protected formatTime(value: string): string {
    return new Intl.DateTimeFormat(this.transloco.getActiveLang(), { dateStyle: "short", timeStyle: "medium" }).format(new Date(value));
  }
}