import { getCurrentUser } from "@stackbraid/client-typescript";

import { unwrap } from "../../../../shared/http/api-error";
import type { Profile } from "../domain/profile";

export const profileRepository = {
  async getOwnProfile(): Promise<Profile> {
    const user = await unwrap(getCurrentUser());
    return {
      displayName: user.displayName,
      email: user.email,
      status: user.status,
      roles: user.roles.map((role) => ({ id: role.id, name: role.name })),
      createdAt: user.createdAt,
      lastLoginAt: user.lastLoginAt ?? null,
    };
  },
};
