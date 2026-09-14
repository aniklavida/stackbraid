import type { Problem } from "@stackbraid/client-typescript";

/**
 * Every non-2xx response from either backend is an RFC 9457 Problem
 * (contract/openapi.yaml's `Problem` schema) — `code` is what this app
 * branches on, `title`/`detail` are what a person reads, `errors` is
 * per-field validation feedback keyed exactly like the form fields that
 * produced it.
 */
export class ApiError extends Error {
  readonly code?: string;
  readonly status: number;
  readonly fieldErrors: Record<string, string[]>;

  constructor(problem: Problem, status: number) {
    super(problem.detail ?? problem.title);
    this.name = "ApiError";
    this.code = problem.code;
    this.status = status;
    this.fieldErrors = (problem.errors as Record<string, string[]> | undefined) ?? {};
  }
}

interface ApiResult<TData> {
  data?: TData;
  error?: Problem;
  response?: Response;
}

/**
 * Turns a generated client call's `{ data } | { error }` result into a plain
 * return-or-throw, so every use case reads like ordinary async code instead
 * of re-checking `.error` at every call site.
 */
export async function unwrap<TData>(result: ApiResult<TData> | Promise<ApiResult<TData>>): Promise<TData> {
  const resolved = await result;
  if (resolved.error) {
    throw new ApiError(resolved.error, resolved.response?.status ?? 0);
  }
  // A 204 (logout, revoke-role) legitimately carries no body — `data` is
  // `undefined` on the wire and typed `void`. Absence of `error` is what
  // means success, not presence of `data`.
  return resolved.data as TData;
}
