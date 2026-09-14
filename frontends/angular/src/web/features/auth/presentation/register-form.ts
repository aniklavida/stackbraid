import { Component, inject, signal } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { Router, RouterLink } from "@angular/router";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { TranslocoPipe, TranslocoService } from "@jsverse/transloco";

import { AuthService } from "../../../../shared/auth/auth.service";
import { ApiError } from "../../../../shared/http/api-error";
import { registerAccount, ValidationFailed } from "../application/use-cases";
import type { FieldErrors, RegisterFormValues } from "../domain/validation";

@Component({
  selector: "app-register-form",
  standalone: true,
  imports: [FormsModule, RouterLink, MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule, TranslocoPipe],
  templateUrl: "./register-form.html",
})
export class RegisterFormComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);

  protected readonly values = signal<RegisterFormValues>({ email: "", password: "", displayName: "" });
  protected readonly fieldErrors = signal<FieldErrors<RegisterFormValues>>({});
  protected readonly formError = signal<string | null>(null);
  protected readonly submitting = signal(false);

  protected setField<K extends keyof RegisterFormValues>(field: K, value: string): void {
    this.values.update((current) => ({ ...current, [field]: value }));
  }

  protected async submit(): Promise<void> {
    this.formError.set(null);
    this.fieldErrors.set({});
    this.submitting.set(true);
    try {
      await registerAccount(this.values(), this.auth);
      await this.router.navigateByUrl("/profile");
    } catch (error) {
      if (error instanceof ValidationFailed) {
        this.fieldErrors.set(error.fieldErrors as FieldErrors<RegisterFormValues>);
      } else if (error instanceof ApiError) {
        if (Object.keys(error.fieldErrors).length > 0) {
          const mapped: FieldErrors<RegisterFormValues> = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            mapped[field as keyof RegisterFormValues] = messages[0];
          }
          this.fieldErrors.set(mapped);
        }
        const key = `auth.errors.${error.code}`;
        const translated = this.transloco.translate(key);
        this.formError.set(translated !== key ? translated : this.transloco.translate("auth.errors.generic"));
      } else {
        this.formError.set(this.transloco.translate("auth.errors.generic"));
      }
    } finally {
      this.submitting.set(false);
    }
  }
}
