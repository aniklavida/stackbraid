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
import { signIn, ValidationFailed } from "../application/use-cases";
import type { FieldErrors, LoginFormValues } from "../domain/validation";

@Component({
  selector: "app-login-form",
  standalone: true,
  imports: [FormsModule, RouterLink, MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule, TranslocoPipe],
  templateUrl: "./login-form.html",
})
export class LoginFormComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);

  protected readonly values = signal<LoginFormValues>({ email: "", password: "" });
  protected readonly fieldErrors = signal<FieldErrors<LoginFormValues>>({});
  protected readonly formError = signal<string | null>(null);
  protected readonly submitting = signal(false);

  protected setEmail(value: string): void {
    this.values.update((current) => ({ ...current, email: value }));
  }

  protected setPassword(value: string): void {
    this.values.update((current) => ({ ...current, password: value }));
  }

  protected async submit(): Promise<void> {
    this.formError.set(null);
    this.fieldErrors.set({});
    this.submitting.set(true);
    try {
      await signIn(this.values(), this.auth);
      await this.router.navigateByUrl("/profile");
    } catch (error) {
      if (error instanceof ValidationFailed) {
        this.fieldErrors.set(error.fieldErrors as FieldErrors<LoginFormValues>);
      } else if (error instanceof ApiError) {
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