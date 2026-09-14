// Realtime: proves both backends push byte-identical JSON over their own
// transport for the events `docs/SPEC.md` §13 scopes — a notification
// stream and a job-progress channel. `contract/openapi.yaml`'s
// `x-realtime-channels` documents the asymmetry (SignalR for .NET, a
// native WebSocket for Python); `src/realtime.mjs` is what lets this file
// stay ignorant of which one it is talking to.

import { request } from '../http.mjs';
import { fail } from '../assert.mjs';
import { Skip } from '../report.mjs';
import { connectRealtimeChannel } from '../realtime.mjs';
import { validateRealtimeMessage, violationsMessage } from '../schema.mjs';
import { resolvePrivilegedTokens } from './shared.mjs';

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 * Neither documented realtime path (`/v1/hubs/*` nor `/v1/ws/*`) existing
 * at all is not a shape violation to fail on — it is what a backend that
 * has not built the realtime layer yet, or a fixture that was never meant
 * to (`fixtures/stub-server`, whose whole job is REST envelope-shape
 * detection), looks like. Only a connection that succeeds and then
 * misbehaves is a real conformance failure.
 */
async function connectOrSkip(baseUrl, channel, accessToken, jobId) {
  try {
    return await connectRealtimeChannel(baseUrl, channel, accessToken, jobId);
  } catch (err) {
    throw new Skip(`no realtime endpoint answered at either documented path for the '${channel}' channel: ${err.message}`);
  }
}

export async function registerRealtimeChecks(harness, ctx) {
  const accessToken = ctx.primaryUser.activeTokens.accessToken;

  await harness.run('realtime — job.progress reaches a client already connected to the job channel', async () => {
    const jobId = globalThis.crypto.randomUUID();
    const conn = await connectOrSkip(ctx.baseUrl, 'jobs', accessToken, jobId);
    try {
      // Give the subscribe/group-join a moment to land server-side before
      // starting the job — otherwise the first frame could be raised
      // before this connection is actually in the job's group.
      await sleep(150);
      conn.start();

      const frames = [];
      for (let i = 0; i < 4; i += 1) {
        frames.push(await conn.waitForMessage(5_000));
      }

      const violations = [];
      frames.forEach((frame, i) => validateRealtimeMessage(frame, `frame[${i}]`, violations));
      if (violations.length) fail(violationsMessage('JobProgressMessage frames', violations));

      frames.forEach((frame, i) => {
        if (frame.type !== 'job.progress') {
          fail(`frame ${i} has the wrong message type`, { field: `frame[${i}].type`, expected: 'job.progress', actual: frame.type });
        }
        if (frame.jobId !== jobId) {
          fail(`frame ${i} carries a different jobId than the one this connection subscribed to`, {
            field: `frame[${i}].jobId`,
            expected: jobId,
            actual: frame.jobId,
          });
        }
      });

      const last = frames[frames.length - 1];
      if (last.status !== 'succeeded' || last.progress !== 100) {
        fail('the job never reached completion', { field: 'frames[last]', expected: "{status: 'succeeded', progress: 100}", actual: JSON.stringify(last) });
      }
    } finally {
      conn.close();
    }
  });

  const { tokens: privilegedTokens, source } = await resolvePrivilegedTokens(ctx);

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

  await privileged("realtime — user.role_changed reaches the notifications channel with the user's full role set", async () => {
    const rolesRes = await request(ctx.baseUrl, { path: '/v1/roles', accessToken: privilegedTokens.accessToken });
    if (rolesRes.status !== 200 || !Array.isArray(rolesRes.body) || rolesRes.body.length === 0) {
      throw new Skip('no roles exist on this backend to assign — nothing to test');
    }
    const role = rolesRes.body[0];

    const conn = await connectOrSkip(ctx.baseUrl, 'notifications', accessToken);
    try {
      await sleep(150);
      const assignRes = await request(ctx.baseUrl, {
        method: 'POST',
        path: `/v1/users/${ctx.primaryUser.id}/roles`,
        accessToken: privilegedTokens.accessToken,
        body: { roleId: role.id },
      });
      if (assignRes.status !== 200) {
        fail('unexpected status assigning a role to trigger the realtime check', { field: 'status', expected: 200, actual: assignRes.status });
      }

      const message = await conn.waitForMessage(5_000);
      const violations = [];
      validateRealtimeMessage(message, 'message', violations);
      if (violations.length) fail(violationsMessage('UserRoleChangedMessage shape', violations));

      if (message.type !== 'user.role_changed') {
        fail('wrong message type', { field: 'message.type', expected: 'user.role_changed', actual: message.type });
      }
      if (message.userId !== ctx.primaryUser.id) {
        fail('message.userId does not match the user whose role changed', { field: 'message.userId', expected: ctx.primaryUser.id, actual: message.userId });
      }
      if (!(message.roles || []).some((r) => r.id === role.id)) {
        fail('the assigned role is missing from the full role set the message carries — contract/openapi.yaml documents this as the full set, not a diff', {
          field: 'message.roles',
          expected: `to include role ${role.id}`,
          actual: (message.roles || []).map((r) => r.id),
        });
      }
    } finally {
      conn.close();
    }
  });

  await privileged('realtime — user.deactivated reaches the notifications channel', async () => {
    const conn = await connectOrSkip(ctx.baseUrl, 'notifications', accessToken);
    try {
      await sleep(150);
      const res = await request(ctx.baseUrl, {
        method: 'POST',
        path: `/v1/users/${ctx.primaryUser.id}/deactivate`,
        accessToken: privilegedTokens.accessToken,
      });
      if (res.status !== 200) {
        fail('unexpected status deactivating the user to trigger the realtime check', { field: 'status', expected: 200, actual: res.status });
      }

      const message = await conn.waitForMessage(5_000);
      const violations = [];
      validateRealtimeMessage(message, 'message', violations);
      if (violations.length) fail(violationsMessage('UserDeactivatedMessage shape', violations));

      if (message.type !== 'user.deactivated') {
        fail('wrong message type', { field: 'message.type', expected: 'user.deactivated', actual: message.type });
      }
      if (message.userId !== ctx.primaryUser.id) {
        fail('message.userId does not match the deactivated user', { field: 'message.userId', expected: ctx.primaryUser.id, actual: message.userId });
      }
    } finally {
      conn.close();
    }
  });
}
