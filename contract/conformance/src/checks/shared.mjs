// Helpers shared across check groups: the Problem-envelope assertion every
// non-2xx response must satisfy, an admin-credential escape hatch for
// permission-gated endpoints, and unique test-data generation so repeated
// runs against the same backend never collide.

import { request } from '../http.mjs';
import { fail } from '../assert.mjs';
import { validateProblem, violationsMessage } from '../schema.mjs';

/**
 * Assert a response is a well-formed RFC 9457 Problem Details response
 * with the given status, served as `application/problem+json`, extended
 * with StackBraid's `code` and `traceId`. Returns the parsed body.
 */
export async function expectProblem(res, { status, expectErrors = false, context }) {
  if (res.status !== status) {
    fail(`unexpected HTTP status for ${context}`, { field: 'status', expected: status, actual: res.status });
  }
  const contentType = res.contentType || '';
  if (!contentType.includes('application/problem+json')) {
    fail(`wrong content-type for ${context} — every non-2xx response must be RFC 9457 Problem Details`, {
      field: 'content-type',
      expected: 'application/problem+json',
      actual: contentType || '(none)',
    });
  }
  const violations = [];
  validateProblem(res.body, 'body', violations, { expectErrors });
  if (violations.length) fail(violationsMessage(`Problem shape for ${context}`, violations));
  return res.body;
}

export function uniqueEmail(tag = 'conformance') {
  const id = (globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random()}`).replace(/-/g, '').slice(0, 16);
  return `${tag}+${id}@example.stackbraid.test`;
}

/**
 * Endpoints gated by a permission (listing/managing users and roles) need a
 * caller who actually holds it. The suite has no knowledge of the backend,
 * so it cannot know how a given deployment seeds its administrator — it
 * only knows the freshly registered test user it created for itself.
 *
 * Resolution, in order:
 *   1. If CONFORMANCE_ADMIN_EMAIL / CONFORMANCE_ADMIN_PASSWORD are set, log
 *      in with them and use that session.
 *   2. Otherwise, fall back to the suite's own freshly registered user.
 *      Checks using this token treat a resulting 403 as a Skip (missing
 *      privilege), not a Fail — the suite still passed no shape or
 *      behaviour check that ran, it just could not reach one that needs an
 *      administrator this run was never given.
 */
export async function resolvePrivilegedTokens(ctx) {
  const adminEmail = process.env.CONFORMANCE_ADMIN_EMAIL;
  const adminPassword = process.env.CONFORMANCE_ADMIN_PASSWORD;
  if (adminEmail && adminPassword) {
    const res = await request(ctx.baseUrl, { method: 'POST', path: '/v1/auth/login', body: { email: adminEmail, password: adminPassword } });
    if (res.status === 200 && res.body && typeof res.body.accessToken === 'string') {
      return { tokens: res.body, source: 'admin credentials (CONFORMANCE_ADMIN_EMAIL)' };
    }
  }
  return { tokens: ctx.primaryUser.activeTokens, source: 'freshly registered user' };
}
