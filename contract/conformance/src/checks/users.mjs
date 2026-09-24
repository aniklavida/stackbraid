// GET/PATCH /v1/users, deactivate — pagination shape, filtering, and the
// User schema on every response.

import { request } from '../http.mjs';
import { fail } from '../assert.mjs';
import { Skip } from '../report.mjs';
import { validateAuditEntry, validateUser, validatePageEnvelope, violationsMessage } from '../schema.mjs';
import { expectProblem, resolvePrivilegedTokens, uniqueEmail } from './shared.mjs';

export async function registerUserChecks(harness, ctx) {
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

  await privileged('GET /v1/users — returns an offset-paginated page (page/pageSize/totalItems/totalPages)', async () => {
    const res = await request(ctx.baseUrl, { path: '/v1/users?page=1&pageSize=2', accessToken });
    if (res.status !== 200) fail('unexpected status listing users', { field: 'status', expected: 200, actual: res.status });
    const violations = [];
    validatePageEnvelope(res.body, 'body', violations);
    (res.body?.items || []).forEach((u, i) => validateUser(u, `body.items[${i}]`, violations));
    if (violations.length) fail(violationsMessage('UserPage shape', violations));

    if (res.body.page !== 1) fail('page param was not honoured', { field: 'body.page', expected: 1, actual: res.body.page });
    if (res.body.pageSize !== 2) fail('pageSize param was not honoured', { field: 'body.pageSize', expected: 2, actual: res.body.pageSize });
    if (res.body.items.length > 2) fail('more items were returned than pageSize allows', { field: 'body.items.length', expected: '<= 2', actual: res.body.items.length });

    const expectedTotalPages = Math.ceil(res.body.totalItems / res.body.pageSize) || 0;
    if (res.body.totalPages !== expectedTotalPages) {
      fail('totalPages is not consistent with totalItems and pageSize', { field: 'body.totalPages', expected: expectedTotalPages, actual: res.body.totalPages });
    }
  });

  await privileged('GET /v1/users — status filter only returns matching accounts', async () => {
    const res = await request(ctx.baseUrl, { path: '/v1/users?status=active&pageSize=50', accessToken });
    if (res.status !== 200) fail('unexpected status filtering users by status', { field: 'status', expected: 200, actual: res.status });
    const bad = (res.body?.items || []).find((u) => u.status !== 'active');
    if (bad) fail('status=active filter returned a non-active user', { field: 'items[].status', expected: 'active', actual: bad.status });
  });

  await privileged('GET /v1/users — search filters by email/displayName substring', async () => {
    const needle = ctx.primaryUser.email.split('@')[0].slice(0, 12);
    const res = await request(ctx.baseUrl, { path: `/v1/users?search=${encodeURIComponent(needle)}&pageSize=50`, accessToken });
    if (res.status !== 200) fail('unexpected status searching users', { field: 'status', expected: 200, actual: res.status });
    const found = (res.body?.items || []).some((u) => u.id === ctx.primaryUser.id);
    if (!found) {
      fail('search did not return the known test user it should match', {
        field: 'body.items',
        expected: `to include user ${ctx.primaryUser.id}`,
        actual: (res.body?.items || []).map((u) => u.id),
      });
    }
  });

  await privileged('GET /v1/users — invalid pageSize is rejected (400, Problem.errors)', async () => {
    const res = await request(ctx.baseUrl, { path: '/v1/users?pageSize=0', accessToken });
    await expectProblem(res, { status: 400, expectErrors: true, context: 'pageSize=0' });
  });

  await privileged('GET /v1/users/{userId} — returns a single user', async () => {
    const res = await request(ctx.baseUrl, { path: `/v1/users/${ctx.primaryUser.id}`, accessToken });
    if (res.status !== 200) fail('unexpected status fetching a known user', { field: 'status', expected: 200, actual: res.status });
    const violations = [];
    validateUser(res.body, 'body', violations);
    if (violations.length) fail(violationsMessage('User shape from GET /v1/users/{userId}', violations));
  });

  await privileged('GET /v1/users/{userId} — unknown id is 404 (Problem)', async () => {
    const res = await request(ctx.baseUrl, { path: '/v1/users/00000000-0000-0000-0000-000000000000', accessToken });
    await expectProblem(res, { status: 404, context: 'unknown userId' });
  });

  await privileged('PATCH /v1/users/{userId} — partial update is applied and reflected', async () => {
    const newName = `Conformance Renamed ${Date.now()}`;
    const res = await request(ctx.baseUrl, { method: 'PATCH', path: `/v1/users/${ctx.primaryUser.id}`, accessToken, body: { displayName: newName } });
    if (res.status !== 200) fail('unexpected status updating a user', { field: 'status', expected: 200, actual: res.status });
    const violations = [];
    validateUser(res.body, 'body', violations);
    if (violations.length) fail(violationsMessage('User shape from PATCH /v1/users/{userId}', violations));
    if (res.body.displayName !== newName) fail('updated displayName was not reflected in the response', { field: 'body.displayName', expected: newName, actual: res.body.displayName });
  });

  await privileged('POST /v1/users/{userId}/deactivate — deactivates, then is idempotent', async () => {
    const email = uniqueEmail('deactivate-target');
    const reg = await request(ctx.baseUrl, { method: 'POST', path: '/v1/auth/register', body: { email, password: 'Conformance!2026', displayName: 'Deactivate Target' } });
    if (reg.status !== 201) fail('could not register a throwaway user for the deactivate check', { field: 'status', expected: 201, actual: reg.status });
    const targetId = reg.body.id;
    const correlationId = `conformance-${Date.now()}`;

    const first = await request(ctx.baseUrl, { method: 'POST', path: `/v1/users/${targetId}/deactivate`, accessToken, headers: { 'X-Correlation-Id': correlationId } });
    if (first.status !== 200) fail('unexpected status deactivating a user', { field: 'status', expected: 200, actual: first.status });
    if (first.body.status !== 'inactive') fail('deactivate did not set status to inactive', { field: 'body.status', expected: 'inactive', actual: first.body.status });
    if (first.body.deletedAt === null || first.body.deletedAt === undefined) fail('deactivate did not set deletedAt', { field: 'body.deletedAt', expected: 'RFC 3339 UTC timestamp', actual: first.body.deletedAt });

    const second = await request(ctx.baseUrl, { method: 'POST', path: `/v1/users/${targetId}/deactivate`, accessToken });
    if (second.status !== 200) fail('deactivating an already-inactive user did not return 200 (not idempotent)', { field: 'status', expected: 200, actual: second.status });
    if (second.body.status !== 'inactive') fail('re-deactivation changed status away from inactive', { field: 'body.status', expected: 'inactive', actual: second.body.status });

    const normalList = await request(ctx.baseUrl, { path: '/v1/users?search=Deactivate%20Target&pageSize=50', accessToken });
    if ((normalList.body?.items || []).some((user) => user.id === targetId)) fail('soft-deleted user appeared in the normal list', { field: 'body.items', expected: 'deleted user absent', actual: targetId });

    const adminList = await request(ctx.baseUrl, { path: '/v1/users?includeDeleted=true&search=Deactivate%20Target&pageSize=50', accessToken });
    const deleted = (adminList.body?.items || []).find((user) => user.id === targetId);
    if (!deleted) fail('includeDeleted=true did not return the soft-deleted user', { field: 'body.items', expected: targetId, actual: (adminList.body?.items || []).map((user) => user.id) });

    const audit = await request(ctx.baseUrl, { path: `/v1/audit?userId=${targetId}`, accessToken });
    if (audit.status !== 200 || !Array.isArray(audit.body?.items)) fail('audit history was not retrievable', { field: 'body.items', expected: 'array', actual: audit.body });
    const deactivateAudit = (audit.body?.items || []).find((entry) => entry.action === 'deleted' && entry.correlationId === correlationId);
    if (!deactivateAudit) fail('deactivation audit event did not carry the request correlation ID', { field: 'items[].correlationId', expected: correlationId, actual: audit.body?.items });
    const auditViolations = [];
    (audit.body?.items || []).forEach((entry, index) => validateAuditEntry(entry, `body.items[${index}]`, auditViolations));
    if (auditViolations.length) fail(violationsMessage('AuditEntry shape', auditViolations));

    const restored = await request(ctx.baseUrl, { method: 'POST', path: `/v1/users/${targetId}/restore`, accessToken });
    if (restored.status !== 200 || restored.body.status !== 'active' || restored.body.deletedAt !== null) fail('restore did not return an active, non-deleted user', { field: 'body', expected: 'active user with deletedAt null', actual: restored.body });
  });
