// Cross-cutting error-envelope checks that aren't tied to one resource.
// Every individual endpoint check elsewhere also runs its errors through
// `expectProblem` (see checks/shared.mjs) — these two are about the
// envelope being *consistent*, not just individually well-shaped.

import { request } from '../http.mjs';
import { fail } from '../assert.mjs';
import { validateProblem, violationsMessage } from '../schema.mjs';

const ALLOWED_PROBLEM_FIELDS = new Set(['type', 'title', 'status', 'detail', 'instance', 'code', 'traceId', 'errors']);

export async function registerErrorEnvelopeChecks(harness, ctx) {
  await harness.run('Error envelope — no undocumented top-level fields', async () => {
    const res = await request(ctx.baseUrl, { method: 'POST', path: '/v1/auth/login', body: { email: 'nobody@example.stackbraid.test', password: 'irrelevant' } });
    if (res.status !== 401) fail('expected a login with unknown credentials to 401', { field: 'status', expected: 401, actual: res.status });
    const extra = Object.keys(res.body || {}).filter((k) => !ALLOWED_PROBLEM_FIELDS.has(k));
    if (extra.length > 0) {
      fail("Problem body carries fields outside RFC 9457 + StackBraid's documented extension (code, traceId, errors)", {
        field: 'body',
        expected: `only ${[...ALLOWED_PROBLEM_FIELDS].join(', ')}`,
        actual: extra,
      });
    }
  });

  await harness.run('Error envelope — traceId correlates one request, not a constant', async () => {
    const [a, b] = await Promise.all([
      request(ctx.baseUrl, { method: 'POST', path: '/v1/auth/login', body: { email: 'nobody@example.stackbraid.test', password: 'irrelevant' } }),
      request(ctx.baseUrl, { method: 'POST', path: '/v1/auth/login', body: { email: 'nobody@example.stackbraid.test', password: 'irrelevant' } }),
    ]);
    const violations = [];
    validateProblem(a.body, 'a', violations);
    validateProblem(b.body, 'b', violations);
    if (violations.length) fail(violationsMessage('Problem shape', violations));
    if (a.body.traceId === b.body.traceId) {
      fail('traceId was identical across two independent requests', {
        field: 'body.traceId',
        expected: 'a distinct value per request',
        actual: a.body.traceId,
      });
    }
  });
}
