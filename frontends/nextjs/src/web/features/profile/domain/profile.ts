/**
 * `/v1/users/{userId}` needs `users:write` to update and `users:read` to
 * even read another account — permissions an ordinary account never holds
 * (it only carries `users:read:self`). `/v1/auth/me` is the one endpoint a
 * signed-in user can always call about themselves, so this feature is a
 * read-only mirror of that response, not a self-service edit form the
 * backend would reject anyway.
 *
 * Modelled independently of the generated client's `User` DTO — domain
 * depends on nothing, the wire shape included; `data/profile-repository.ts`
 * is the only place that shape is ever named.
 */
export interface ProfileRole {
  id: string;
  name: string;
}

export interface Profile {
  displayName: string;
  email: string;
  status: "active" | "inactive";
  roles: ProfileRole[];
  createdAt: string;
  lastLoginAt: string | null;
}

export function roleNames(profile: Profile): string[] {
  return profile.roles.map((role) => role.name);
}
