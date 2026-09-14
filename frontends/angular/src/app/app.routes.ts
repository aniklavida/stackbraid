import type { Routes } from "@angular/router";
import { provideTranslocoScope } from "@jsverse/transloco";

import { authGuard, permissionGuard } from "../shared/auth/auth.guard";
import { AUTH_I18N_SCOPE } from "../web/features/auth";
import { HOME_I18N_SCOPE } from "../web/features/home";
import { PROFILE_I18N_SCOPE } from "../web/features/profile";
import { USERS_I18N_SCOPE } from "../admin/features/users";
import { ROLES_I18N_SCOPE } from "../admin/features/roles";
import { ADMIN_LAYOUT_I18N_SCOPE } from "../admin/layout/admin.i18n";

export const routes: Routes = [
  {
    path: "",
    loadComponent: () => import("../web/layout/web-shell").then((m) => m.WebShellComponent),
    providers: [provideTranslocoScope(HOME_I18N_SCOPE), provideTranslocoScope(AUTH_I18N_SCOPE)],
    children: [
      { path: "", loadComponent: () => import("../web/features/home").then((m) => m.HomePageComponent) },
      { path: "login", loadComponent: () => import("../web/features/auth").then((m) => m.LoginFormComponent) },
      { path: "register", loadComponent: () => import("../web/features/auth").then((m) => m.RegisterFormComponent) },
      {
        path: "profile",
        canActivate: [authGuard],
        providers: [provideTranslocoScope(PROFILE_I18N_SCOPE)],
        loadComponent: () => import("../web/features/profile").then((m) => m.ProfilePageComponent),
      },
    ],
  },
  {
    path: "admin",
    canActivate: [permissionGuard("users:read")],
    providers: [provideTranslocoScope(ADMIN_LAYOUT_I18N_SCOPE)],
    loadComponent: () => import("../admin/layout/admin-shell").then((m) => m.AdminShellComponent),
    children: [
      { path: "", pathMatch: "full", redirectTo: "users" },
      {
        path: "users",
        providers: [provideTranslocoScope(USERS_I18N_SCOPE)],
        loadComponent: () => import("../admin/features/users").then((m) => m.UsersPageComponent),
      },
      {
        path: "users/:userId",
        providers: [provideTranslocoScope(USERS_I18N_SCOPE), provideTranslocoScope(ROLES_I18N_SCOPE)],
        loadComponent: () => import("../admin/features/users").then((m) => m.UserDetailPageComponent),
      },
      {
        path: "roles",
        providers: [provideTranslocoScope(ROLES_I18N_SCOPE)],
        loadComponent: () => import("../admin/features/roles").then((m) => m.RolesPageComponent),
      },
    ],
  },
];
