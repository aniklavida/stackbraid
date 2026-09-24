// A domain-owned type, not borrowed from the generated client — domain
// depends on nothing, the wire schema included. It happens to line up with
// the contract's `UserStatus` enum today; `data/users-repository.ts` is the
// only place that ever has to know it's the same shape.
export type AccountStatus = "active" | "inactive";

export interface UsersFilter {
  page: number;
  pageSize: number;
  search?: string;
  status?: AccountStatus;
  includeDeleted?: boolean;
}

export const DEFAULT_USERS_FILTER: UsersFilter = { page: 1, pageSize: 20 };
