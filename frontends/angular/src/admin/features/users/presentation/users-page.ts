import { Component, inject, signal } from "@angular/core";
import { RouterLink } from "@angular/router";
import { MatButtonModule } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { TranslocoPipe, TranslocoService } from "@jsverse/transloco";

import { DEFAULT_USERS_FILTER, type AccountStatus, type UsersFilter } from "../domain/user-filters";
import { useDeactivateUser, useRestoreUser, useUsers } from "../application/use-users";

@Component({
  selector: "app-users-page",
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatFormFieldModule, MatInputModule, MatProgressBarModule, MatSelectModule, TranslocoPipe],
  templateUrl: "./users-page.html",
})
export class UsersPageComponent {
  private readonly snackBar = inject(MatSnackBar);
  private readonly transloco = inject(TranslocoService);

  protected readonly filter = signal<UsersFilter>(DEFAULT_USERS_FILTER);
  protected readonly users = useUsers(this.filter);
  protected readonly deactivate = useDeactivateUser();
  protected readonly restore = useRestoreUser();

  protected setSearch(value: string): void {
    this.filter.update((current) => ({ ...current, page: 1, search: value }));
  }

  protected setStatus(value: string): void {
    this.filter.update((current) => ({ ...current, page: 1, status: value === "all" ? undefined : (value as AccountStatus) }));
  }

  protected toggleDeleted(): void {
    this.filter.update((current) => ({ ...current, page: 1, includeDeleted: !current.includeDeleted }));
  }

  protected previousPage(): void {
    this.filter.update((current) => ({ ...current, page: current.page - 1 }));
  }

  protected nextPage(): void {
    this.filter.update((current) => ({ ...current, page: current.page + 1 }));
  }

  protected restoreUser(userId: string, displayName: string): void {
    this.restore.mutate(userId, {
      onSuccess: () => this.snackBar.open(this.transloco.translate("users.actions.restored", { name: displayName }), undefined, { duration: 3000 }),
    });
  }

  protected deactivateUser(userId: string, displayName: string): void {
    this.deactivate.mutate(userId, {
      onSuccess: () => this.snackBar.open(this.transloco.translate("users.actions.deactivated", { name: displayName }), undefined, { duration: 3000 }),
    });
  }
}