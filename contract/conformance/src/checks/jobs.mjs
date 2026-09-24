// Background-job behaviour that both backends must implement identically:
// a queued job completes, a failing job retries with backoff then succeeds,
// one that always fails ends up dead-lettered with its error preserved, and
// a job's status is visible only to the caller who started it.
//
// This drives the backends' conformance-only job surface
// (`POST /v1/jobs/scenarios`), which exists to make retry/dead-letter
// behaviour deterministic over HTTP. When that surface is not enabled, the
// group skips rather than failing — the same treatment the expiry check gives
// a backend with a long access-token TTL.

import { request } from '../http.mjs';
import { fail } from '../assert.mjs';
import { Skip } from '../report.mjs';
import { uniqueEmail } from './shared.mjs';

const PASSWORD = 'Conformance!2026';
const TERMINAL_STATES = new Set(['succeeded', 'dead-lettered']);
const POLL_INTERVAL_MS = 100;
const POLL_TIMEOUT_MS = 20_000;

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function login(ctx, email) {
  const res = await request(ctx.baseUrl, { method: 'POST', path: '/v1/auth/login', body: { email, password: PASSWORD } });
  if (res.status !== 200) fail('login for the jobs checks failed', { field: 'status', expected: 200, actual: res.status });
  return res.body.accessToken;
}

async function enqueue(baseUrl, accessToken, scenario) {
  return request(baseUrl, {
    method: 'POST',
    path: '/v1/jobs/scenarios',
    body: { scenario },
    accessToken,
  });
}

async function waitForTerminal(baseUrl, accessToken, jobId) {
  const deadline = Date.now() + POLL_TIMEOUT_MS;
  let last = null;
  while (Date.now() < deadline) {
    const res = await request(baseUrl, { path: `/v1/jobs/${jobId}`, accessToken });
    if (res.status !== 200) {
      fail('the owning caller could not read their job status', { field: 'status', expected: 200, actual: res.status });
    }
    last = res.body;
    if (last && TERMINAL_STATES.has(last.status)) return last;
    await sleep(POLL_INTERVAL_MS);
  }
  fail('a job did not reach a terminal state before the timeout', {
    field: 'status',
    expected: 'succeeded or dead-lettered',
    actual: last && last.status,
  });
}

export async function registerJobChecks(harness, ctx) {
  const token = await login(ctx, ctx.primaryUser.email);
  const probe = await enqueue(ctx.baseUrl, token, 'succeeds');

  if (probe.status === 404) {
    await harness.run('Jobs — background-job enqueue/complete/retry/dead-letter', () => {
      throw new Skip(
        'The jobs conformance surface is not enabled on this backend (POST /v1/jobs/scenarios returned 404). ' +
          'Enable it in the backend under test to exercise these checks.',
      );
    });
    return;
  }

  if (probe.status !== 202) {
    fail('enqueueing a conformance job did not return 202 Accepted', { field: 'status', expected: 202, actual: probe.status });
  }
  if (!probe.body || typeof probe.body.jobId !== 'string') {
    fail('the enqueue response did not carry a jobId', { field: 'body.jobId', expected: 'a UUID string', actual: probe.body });
  }

  await harness.run('Jobs — a queued job completes and is visible to its owner', async () => {
    const status = await waitForTerminal(ctx.baseUrl, token, probe.body.jobId);
    if (status.status !== 'succeeded') fail('a succeeding job did not reach "succeeded"', { field: 'status', expected: 'succeeded', actual: status.status });
    if (status.attempts !== 1) fail('a job that succeeds first time reported the wrong attempt count', { field: 'attempts', expected: 1, actual: status.attempts });
  });

  await harness.run('Jobs — a failing job retries with backoff and then succeeds', async () => {
    const freshToken = await login(ctx, ctx.primaryUser.email);
    const res = await enqueue(ctx.baseUrl, freshToken, 'retries');
    if (res.status !== 202) fail('enqueueing the retry scenario failed', { field: 'status', expected: 202, actual: res.status });
    const status = await waitForTerminal(ctx.baseUrl, freshToken, res.body.jobId);
    if (status.status !== 'succeeded') fail('a job that fails then succeeds did not reach "succeeded"', { field: 'status', expected: 'succeeded', actual: status.status });
    if (status.attempts !== 3) fail('the job did not record the expected retry attempts', { field: 'attempts', expected: 3, actual: status.attempts });
  });

  await harness.run('Jobs — a job that always fails is dead-lettered with its error preserved', async () => {
    const freshToken = await login(ctx, ctx.primaryUser.email);
    const res = await enqueue(ctx.baseUrl, freshToken, 'dead-letter');
    if (res.status !== 202) fail('enqueueing the dead-letter scenario failed', { field: 'status', expected: 202, actual: res.status });
    const status = await waitForTerminal(ctx.baseUrl, freshToken, res.body.jobId);
    if (status.status !== 'dead-lettered') fail('a permanently failing job did not reach "dead-lettered"', { field: 'status', expected: 'dead-lettered', actual: status.status });
    if (status.attempts !== 2) fail('the dead-lettered job recorded the wrong attempt count', { field: 'attempts', expected: 2, actual: status.attempts });
    if (!status.lastError) fail('the dead-lettered job did not preserve the handler error', { field: 'lastError', expected: 'a non-empty error', actual: status.lastError });
  });

  await harness.run('Jobs — status is owner-scoped (another user cannot read it)', async () => {
    const secondEmail = uniqueEmail('jobs-secondary');
    const register = await request(ctx.baseUrl, {
      method: 'POST',
      path: '/v1/auth/register',
      body: { email: secondEmail, password: PASSWORD, displayName: 'Jobs Secondary' },
    });
    if (register.status !== 201) fail('registering the secondary user for the owner-scope check failed', { field: 'status', expected: 201, actual: register.status });
    const secondToken = await login(ctx, secondEmail);
    const res = await request(ctx.baseUrl, { path: `/v1/jobs/${probe.body.jobId}`, accessToken: secondToken });
    if (res.status !== 404) {
      fail('a caller who did not start the job could read its status', { field: 'status', expected: 404, actual: res.status });
    }
  });
}
