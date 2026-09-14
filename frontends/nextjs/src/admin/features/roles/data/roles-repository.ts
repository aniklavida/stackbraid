import { listRoles } from "@stackbraid/client-typescript";
import type { Role } from "@stackbraid/client-typescript";

import { unwrap } from "@/shared/http/api-error";

export const rolesRepository = {
  listRoles(): Promise<Role[]> {
    return unwrap(listRoles());
  },
};
