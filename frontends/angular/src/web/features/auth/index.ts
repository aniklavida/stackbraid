// The only way anything outside `web/features/auth/` may reach into it —
// mirrors the "only through Contracts" rule the backends enforce between
// features, restated for a frontend where a feature's barrel file is its
// Contracts. Enforced by `.dependency-cruiser.mjs`.
export { AUTH_I18N_SCOPE } from "./auth.i18n";
export { LoginFormComponent } from "./presentation/login-form";
export { RegisterFormComponent } from "./presentation/register-form";
export { signOut } from "./application/use-cases";
