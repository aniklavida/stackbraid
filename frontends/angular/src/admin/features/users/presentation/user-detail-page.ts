import { Component, computed, inject, signal } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { map } from "rxjs";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { TranslocoPipe, TranslocoService } from "@jsverse/transloco";

import { useRoles } from "../../roles";
import { useAssignRole, useRestoreUser, useRevokeRole, useUser, useUserAudit } from "../application/use-users";

@Component({
  selector: "app-user-detail-page",
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatCardModule, MatFormFieldModule, MatProgressBarModule, MatSelectModule, TranslocoPipe],
  templateUrl: "./user-detail-page.html",
})
export class UserDetailPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);
  private readonly transloco = inject(TranslocoService);

  protected readonly userId = toSignal(this.route.paramMap.pipe(map((params) => params.get("userId")!)), { requireSync: true });

  protected readonly user = useUser(this.userId);
  protected readonly roles = useRoles();
  protected readonly assignRole = useAssignRole(this.userId);
  protected readonly revokeRole = useRevokeRole(this.userId);
  protected readonly audit = useUserAudit(this.userId);
  protected readonly restore = useRestoreUser();
  protected readonly selectedRoleId = signal("");

  protected readonly assignableRoles = computed(() => {
    const held = this.user.data()?.roles ?? [];
    return (this.roles.data() ?? []).filter((role) => !held.some((heldRole) => heldRole.id === role.id));
  });

  protected formatDate(value: string): string {
    return new Intl.DateTimeFormat(this.transloco.getActiveLang(), { dateStyle: "long" }).format(new Date(value));
  }

  protected restoreUser(): void {
    this.restore.mutate(this.userId(), {
      onSuccess: () => this.snackBar.open(this.transloco.translate("users.actions.restored", { name: this.user.data()?.displayName ?? "" }), undefined, { duration: 3000 }),
    });
  }

  protected revoke(roleId: string, roleName: string): void {
    this.revokeRole.mutate(roleId, {
      onSuccess: () => this.snackBar.open(this.transloco.translate("users.detail.revoked", { name: roleName }), undefined, { duration: 3000 }),
    });
  }

  protected assign(): void {
    const roleId = this.selectedRoleId();
    const role = this.assignableRoles().find((candidate) => candidate.id === roleId);
    if (!roleId) return;
    this.assignRole.mutate(roleId, {
      onSuccess: () => {
        this.snackBar.open(this.transloco.translate("users.detail.assigned", { name: role?.name ?? roleId }), undefined, { duration: 3000 });
        this.selectedRoleId.set("");
      },
    });
  }
}
