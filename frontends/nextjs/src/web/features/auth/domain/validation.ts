/**
 * Validation rules — the one thing this feature owns that no other layer
 * should. Mirrors the contract exactly (`RegisterRequest`/`LoginRequest` in
 * contract/openapi.yaml) so a form never lets through what the backend
 * would reject anyway; the backend still re-validates independently, this
 * is only for a fast, local error message.
 */
export interface RegisterFormValues {
  email: string;
  password: string;
  displayName: string;
}

export interface LoginFormValues {
  email: string;
  password: string;
}

export type FieldErrors<T> = Partial<Record<keyof T, string>>;

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function validateRegisterForm(values: RegisterFormValues): FieldErrors<RegisterFormValues> {
  const errors: FieldErrors<RegisterFormValues> = {};
  if (!EMAIL_PATTERN.test(values.email)) {
    errors.email = "Enter a valid email address.";
  }
  if (values.password.length < 8) {
    errors.password = "Use at least 8 characters.";
  }
  if (values.displayName.trim().length < 1) {
    errors.displayName = "Tell us what to call you.";
  } else if (values.displayName.length > 200) {
    errors.displayName = "Keep it under 200 characters.";
  }
  return errors;
}

export function validateLoginForm(values: LoginFormValues): FieldErrors<LoginFormValues> {
  const errors: FieldErrors<LoginFormValues> = {};
  if (!EMAIL_PATTERN.test(values.email)) {
    errors.email = "Enter a valid email address.";
  }
  if (values.password.length === 0) {
    errors.password = "Enter your password.";
  }
  return errors;
}
