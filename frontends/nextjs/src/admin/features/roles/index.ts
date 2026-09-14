/**
 * This feature's public surface — the only part another feature (`users`,
 * specifically, for its role-assignment picker) may import. Everything
 * under `domain/`, `data/`, `application/` and `presentation/` is internal;
 * dependency-cruiser enforces that only this barrel crosses the boundary.
 */
export { useRoles } from "./application/use-roles";
export { RolesPage } from "./presentation/RolesPage";
