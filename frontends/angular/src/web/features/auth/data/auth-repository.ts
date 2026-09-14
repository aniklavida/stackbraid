import { login, logout, registerUser } from "@stackbraid/client-typescript";
import type { TokenPair, User } from "@stackbraid/client-typescript";

import { unwrap } from "../../../../shared/http/api-error";
import type { LoginFormValues, RegisterFormValues } from "../domain/validation";

/**
 * The only place this feature touches the generated client. Everything
 * above this line thinks in terms of `RegisterFormValues`/`TokenPair`, not
 * fetch calls or response envelopes.
 */
export const authRepository = {
  register(values: RegisterFormValues): Promise<User> {
    return unwrap(registerUser({ body: values }));
  },

  signIn(values: LoginFormValues): Promise<TokenPair> {
    return unwrap(login({ body: values }));
  },

  signOut(): Promise<void> {
    return unwrap(logout());
  },
};
