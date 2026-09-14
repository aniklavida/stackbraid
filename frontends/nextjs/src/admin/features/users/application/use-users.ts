import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { usersRepository } from "../data/users-repository";
import type { UsersFilter } from "../domain/user-filters";

export function useUsers(filter: UsersFilter) {
  return useQuery({
    queryKey: ["users", filter],
    queryFn: () => usersRepository.listUsers(filter),
    placeholderData: (previous) => previous,
  });
}

export function useUser(userId: string) {
  return useQuery({
    queryKey: ["users", "detail", userId],
    queryFn: () => usersRepository.getUser(userId),
  });
}

export function useDeactivateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (userId: string) => usersRepository.deactivateUser(userId),
    onSuccess: (user) => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
      queryClient.setQueryData(["users", "detail", user.id], user);
    },
  });
}

export function useAssignRole(userId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (roleId: string) => usersRepository.assignRole(userId, roleId),
    onSuccess: (user) => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
      queryClient.setQueryData(["users", "detail", user.id], user);
    },
  });
}

export function useRevokeRole(userId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (roleId: string) => usersRepository.revokeRole(userId, roleId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
      queryClient.invalidateQueries({ queryKey: ["users", "detail", userId] });
    },
  });
}
