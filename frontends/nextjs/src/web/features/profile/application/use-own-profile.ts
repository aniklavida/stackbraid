import { useQuery } from "@tanstack/react-query";

import { profileRepository } from "../data/profile-repository";

export function useOwnProfile() {
  return useQuery({
    queryKey: ["profile", "me"],
    queryFn: profileRepository.getOwnProfile,
  });
}
