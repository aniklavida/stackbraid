import type { User } from "@stackbraid/client-typescript";

import type { SessionPort } from "../../../../shared/auth/auth.service";
import { authRepository } from "../data/auth-repository";
import { type LoginFormValues, type RegisterFormValues, validateLoginForm, validateRegisterForm } from "../domain/validation";

export class ValidationFailed extends Error {
  constructor(readonly fieldErrors: Record<string, string>) {
    super("One or more fields need attention.");
    this.name = "ValidationFailed";
  }
}

export async function registerAccount(values: RegisterFormValues, session: SessionPort): Promise<User> {
  const fieldErrors = validateRegisterForm(values);
  if (Object.keys(fieldErrors).length > 0) throw new ValidationFailed(fieldErrors as Record<string, string>);

  // `POST /v1/auth/register` only creates the account — the contract is
  // explicit that it does not issue a session (unlike login/refresh, it
  // returns the created `User`, not a `TokenPair`). Signing in right after,
  // with the same credentials this form already collected, is this app's
  // choice of UX, not something the endpoint does for it.
  await authRepository.register(values);
  const tokens = await authRepository.signIn({ email: values.email, password: values.password });
  return session.adoptSession(tokens);
}

export async function signIn(values: LoginFormValues, session: SessionPort): Promise<User> {
  const fieldErrors = validateLoginForm(values);
  if (Object.keys(fieldErrors).length > 0) throw new ValidationFailed(fieldErrors as Record<string, string>);

  const tokens = await authRepository.signIn(values);
  return session.adoptSession(tokens);
}

export async function signOut(session: SessionPort): Promise<void> {
  try {
    await authRepository.signOut();
  } catch {
    // Swallowed deliberately: the session is cleared locally either way, so
    // an offline (or already-expired) logout still logs this tab out rather
    // than surfacing a network error for an action that, from the user's
    // point of view, already succeeded.
  } finally {
    session.clearSession();
  }
}