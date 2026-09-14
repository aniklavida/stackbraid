import { useQuery } from "@tanstack/react-query";

import { rolesRepository } from "../data/roles-repository";

export function useRoles() {
  return useQuery({
    queryKey: ["roles"],
    queryFn: rolesRepository.listRoles,
    // The roles catalogue is administrator-curated and small by design
    // (contract/openapi.yaml's own description of `GET /v1/roles`) — it
    // changes rarely enough that a longer stale time avoids refetching it
    // on every dialog open.
    staleTime: 60_000,
  });
}
