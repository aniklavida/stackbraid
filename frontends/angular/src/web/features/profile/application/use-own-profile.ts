import { injectQuery } from "@tanstack/angular-query-experimental";

import { profileRepository } from "../data/profile-repository";

export function useOwnProfile() {
  return injectQuery(() => ({
    queryKey: ["profile", "me"],
    queryFn: profileRepository.getOwnProfile,
  }));
}