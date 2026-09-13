// GET /v1/roles, assign/revoke — the (deliberately unpaginated) Role
// catalogue and per-user role membership.

import { request } from '../http.mjs';
import { fail } from '../assert.mjs';
import { Skip } from '../report.mjs';
import { validateRole, violationsMessage } from '../schema.mjs';
import { expectProblem, resolvePrivilegedTokens } from './shared.mjs';

export async function registerRoleChecks(harness, ctx) {
  const { tokens, source } = await resolvePrivilegedTokens(ctx);
  const accessToken = tokens.accessToken;

  async function privileged(name, fn) {
    await harness.run(name, async () => {
      try {
        await fn();
      } catch (err) {
        if (err?.details?.actual === 403 && source !== 'admin credentials (CONFORMANCE_ADMIN_EMAIL)') {
          throw new Skip(
            `${name} needs a permission the freshly registered test user does not hold (got 403). ` +
              'Set CONFORMANCE_ADMIN_EMAIL / CONFORMANCE_ADMIN_PASSWORD to run this against a seeded administrator.',
          );
        }
        throw err;
      }
    });
  }

  let roles = [];
  await privileged('GET /v1/roles — returns the full, unpaginated catalogue', async () => {
    const res = await request(ctx.baseUrl, { path: '/v1/roles', accessToken });
    if (res.status !== 200) fail('unexpected status listing roles', { field: 'status', expected: 200, actual: res.status });
    if (!Array.isArray(res.body)) {
      fail('roles is documented as the full catalogue (a bare array), not a Page envelope', { field: 'body', expected: 'array', actual: typeof res.body });
    }
    const violations = [];
    (res.body || []).forEach((r, i) => validateRole(r, `body[${i}]`, violations));
    if (violations.length) fail(violationsMessage('Role[] shape', violations));
    roles = res.body || [];
  });

  if (roles.length === 0) {
    await harness.run('POST /v1/users/{userId}/roles — assign a role', async () => {
      throw new Skip('no roles exist on this backend to assign — nothing to test');
    });
    return;
  }

  const role = roles[0];

  await privileged('POST /v1/users/{userId}/roles — assigns a role, is a no-op on repeat', async () => {
    const res = await request(ctx.baseUrl, { method: 'POST', path: `/v1/users/${ctx.primaryUser.id}/roles`, accessToken, body: { roleId: role.id } });
    if (res.status !== 200) fail('unexpected status assigning a role', { field: 'status', expected: 200, actual: res.status });
    const hasRole = (res.body?.roles || []).some((r) => r.id === role.id);
    if (!hasRole) {
      fail('assigned role does not appear on the user', { field: 'body.roles', expected: `to include role ${role.id}`, actual: (res.body?.roles || []).map((r) => r.id) });
    }
    const again = await request(ctx.baseUrl, { method: 'POST', path: `/v1/users/${ctx.primaryUser.id}/roles`, accessToken, body: { roleId: role.id } });
    if (again.status !== 200) fail('re-assigning an already-held role did not return 200 (not a no-op)', { field: 'status', expected: 200, actual: again.status });
  });

  await privileged('POST /v1/users/{userId}/roles — unknown roleId is 404 (Problem)', async () => {
    const res = await request(ctx.baseUrl, {
      method: 'POST',
      path: `/v1/users/${ctx.primaryUser.id}/roles`,
      accessToken,
      body: { roleId: '00000000-0000-0000-0000-000000000000' },
    });
    await expectProblem(res, { status: 404, context: 'assigning an unknown roleId' });
  });

  await privileged('DELETE /v1/users/{userId}/roles/{roleId} — revokes a role, then is idempotent', async () => {
    const first = await request(ctx.baseUrl, { method: 'DELETE', path: `/v1/users/${ctx.primaryUser.id}/roles/${role.id}`, accessToken });
    if (first.status !== 204) fail('unexpected status revoking a role', { field: 'status', expected: 204, actual: first.status });
    const second = await request(ctx.baseUrl, { method: 'DELETE', path: `/v1/users/${ctx.primaryUser.id}/roles/${role.id}`, accessToken });
    if (second.status !== 204) fail('revoking a role the user no longer holds did not return 204 (not idempotent)', { field: 'status', expected: 204, actual: second.status });
  });
}
