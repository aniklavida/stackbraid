import { injectQuery } from "@tanstack/angular-query-experimental";

import { rolesRepository } from "../data/roles-repository";

export function useRoles() {
  return injectQuery(() => ({
    queryKey: ["roles"],
    queryFn: rolesRepository.listRoles,
    // The roles catalogue is administrator-curated and small by design — it
    // changes rarely enough that a longer stale time avoids refetching it on
    // every dialog open.
    staleTime: 60_000,
  }));
}