import { assignRole, deactivateUser, getUser, listUsers, revokeRole } from "@stackbraid/client-typescript";
import type { User, UserPage } from "@stackbraid/client-typescript";

import { unwrap } from "../../../../shared/http/api-error";
import type { UsersFilter } from "../domain/user-filters";

export const usersRepository = {
  listUsers(filter: UsersFilter): Promise<UserPage> {
    return unwrap(
      listUsers({
        query: {
          page: filter.page,
          pageSize: filter.pageSize,
          search: filter.search || undefined,
          status: filter.status,
        },
      }),
    );
  },

  getUser(userId: string): Promise<User> {
    return unwrap(getUser({ path: { userId } }));
  },

  deactivateUser(userId: string): Promise<User> {
    return unwrap(deactivateUser({ path: { userId } }));
  },

  assignRole(userId: string, roleId: string): Promise<User> {
    return unwrap(assignRole({ path: { userId }, body: { roleId } }));
  },

  revokeRole(userId: string, roleId: string): Promise<void> {
    return unwrap(revokeRole({ path: { userId, roleId } }));
  },
};
