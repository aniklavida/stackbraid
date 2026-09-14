import { injectMutation, injectQuery, injectQueryClient, keepPreviousData } from "@tanstack/angular-query-experimental";
import type { Signal } from "@angular/core";

import { usersRepository } from "../data/users-repository";
import type { UsersFilter } from "../domain/user-filters";

export function useUsers(filter: Signal<UsersFilter>) {
  return injectQuery(() => ({
    queryKey: ["users", filter()],
    queryFn: () => usersRepository.listUsers(filter()),
    placeholderData: keepPreviousData,
  }));
}

export function useUser(userId: Signal<string>) {
  return injectQuery(() => ({
    queryKey: ["users", "detail", userId()],
    queryFn: () => usersRepository.getUser(userId()),
  }));
}

export function useDeactivateUser() {
  const queryClient = injectQueryClient();
  return injectMutation(() => ({
    mutationFn: (userId: string) => usersRepository.deactivateUser(userId),
    onSuccess: (user) => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
      queryClient.setQueryData(["users", "detail", user.id], user);
    },
  }));
}

export function useAssignRole(userId: Signal<string>) {
  const queryClient = injectQueryClient();
  return injectMutation(() => ({
    mutationFn: (roleId: string) => usersRepository.assignRole(userId(), roleId),
    onSuccess: (user) => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
      queryClient.setQueryData(["users", "detail", user.id], user);
    },
  }));
}

export function useRevokeRole(userId: Signal<string>) {
  const queryClient = injectQueryClient();
  return injectMutation(() => ({
    mutationFn: (roleId: string) => usersRepository.revokeRole(userId(), roleId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
      queryClient.invalidateQueries({ queryKey: ["users", "detail", userId()] });
    },
  }));
}