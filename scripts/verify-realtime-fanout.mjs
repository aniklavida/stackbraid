#!/usr/bin/env node
// Proves multi-instance fan-out for real: two running instances of the
// *same* backend, both configured against the same Redis (or Valkey)
// backplane, sharing one Postgres database. A client connects to
// instance A; an event is raised through instance B (a REST call for the
// notification stream, a job started over B's own realtime connection for
// the job-progress channel); the client on A must receive it. Nothing here
// is checked by asserting against instance B's own state — a message
// arriving on a connection to a *different* process is the only thing that
// actually distinguishes a working backplane from one instance quietly
// answering its own request.
//
// Manual, local verification — like scripts/*/start-local-postgres.sh, this
// is not wired into CI (it needs two live server processes and a running
// Redis/Valkey already pointed at each other) and needs no dependency of
// its own: Node's built-in fetch and WebSocket are enough.
//
// Usage:
//   node scripts/verify-realtime-fanout.mjs <baseUrlA> <baseUrlB>
//
// Env:
//   CONFORMANCE_ADMIN_EMAIL / CONFORMANCE_ADMIN_PASSWORD — an administrator
//   seeded on the shared database (both instances point at the same one),
//   needed to assign a role and deactivate the test account.

import { connectRealtimeChannel } from '../contract/conformance/src/realtime.mjs';

const [baseUrlA, baseUrlB] = process.argv.slice(2);
if (!baseUrlA || !baseUrlB) {
  console.error('Usage: node scripts/verify-realtime-fanout.mjs <baseUrlA> <baseUrlB>');
  process.exit(2);
}

const ADMIN_EMAIL = process.env.CONFORMANCE_ADMIN_EMAIL;
const ADMIN_PASSWORD = process.env.CONFORMANCE_ADMIN_PASSWORD;
if (!ADMIN_EMAIL || !ADMIN_PASSWORD) {
  console.error('Set CONFORMANCE_ADMIN_EMAIL / CONFORMANCE_ADMIN_PASSWORD to an administrator seeded on the shared database.');
  process.exit(2);
}

let failures = 0;

function report(name, ok, detail) {
  console.log(`[${ok ? 'PASS' : 'FAIL'}] ${name}${detail ? ` — ${detail}` : ''}`);
  if (!ok) failures += 1;
}

async function postJson(baseUrl, path, body, accessToken) {
  const headers = { 'content-type': 'application/json', accept: 'application/json' };
  if (accessToken) headers.authorization = `Bearer ${accessToken}`;
  const res = await fetch(new URL(path, baseUrl), { method: 'POST', headers, body: JSON.stringify(body ?? {}) });
  const text = await res.text();
  return { status: res.status, body: text ? JSON.parse(text) : null };
}

async function getJson(baseUrl, path, accessToken) {
  const res = await fetch(new URL(path, baseUrl), { headers: { authorization: `Bearer ${accessToken}`, accept: 'application/json' } });
  return { status: res.status, body: await res.json() };
}

async function main() {
  console.log(`Instance A (receives): ${baseUrlA}`);
  console.log(`Instance B (raises):   ${baseUrlB}\n`);

  const email = `fanout+${Date.now()}@example.stackbraid.test`;
  const password = 'Conformance!2026';
  const registered = await postJson(baseUrlA, '/v1/auth/register', { email, password, displayName: 'Fan-out Probe' });
  if (registered.status !== 201) throw new Error(`could not register the probe account via instance A: HTTP ${registered.status}`);
  const userId = registered.body.id;
  // Registration does not issue a session (contract/openapi.yaml) — log in separately.
  const login = await postJson(baseUrlA, '/v1/auth/login', { email, password });
  if (login.status !== 200) throw new Error(`could not log the probe account in via instance A: HTTP ${login.status}`);
  const accessToken = login.body.accessToken;

  const adminLogin = await postJson(baseUrlB, '/v1/auth/login', { email: ADMIN_EMAIL, password: ADMIN_PASSWORD });
  if (adminLogin.status !== 200) throw new Error(`admin login against instance B failed: HTTP ${adminLogin.status}`);
  const adminToken = adminLogin.body.accessToken;

  // --- Notifications: connect to A, raise the event through B ------------
  {
    const conn = await connectRealtimeChannel(baseUrlA, 'notifications', accessToken);
    try {
      await new Promise((r) => setTimeout(r, 200));

      const roles = await getJson(baseUrlB, '/v1/roles', adminToken);
      if (roles.status !== 200 || !Array.isArray(roles.body) || roles.body.length === 0) {
        report('notifications fan-out (user.role_changed, raised on B, received on A)', false, 'no roles exist on this backend to assign');
      } else {
        const role = roles.body[0];
        const assign = await postJson(baseUrlB, `/v1/users/${userId}/roles`, { roleId: role.id }, adminToken);
        if (assign.status !== 200) throw new Error(`assigning a role via instance B failed: HTTP ${assign.status}`);

        const message = await conn.waitForMessage(5_000);
        const ok = message.type === 'user.role_changed' && message.userId === userId && message.roles.some((r) => r.id === role.id);
        report(
          'notifications fan-out (user.role_changed, raised on B, received on A)',
          ok,
          ok ? `via ${conn.transport}` : `unexpected message: ${JSON.stringify(message)}`,
        );
      }
    } finally {
      conn.close();
    }
  }

  {
    const conn = await connectRealtimeChannel(baseUrlA, 'notifications', accessToken);
    try {
      await new Promise((r) => setTimeout(r, 200));
      const deactivate = await postJson(baseUrlB, `/v1/users/${userId}/deactivate`, {}, adminToken);
      if (deactivate.status !== 200) throw new Error(`deactivating via instance B failed: HTTP ${deactivate.status}`);

      const message = await conn.waitForMessage(5_000);
      const ok = message.type === 'user.deactivated' && message.userId === userId;
      report(
        'notifications fan-out (user.deactivated, raised on B, received on A)',
        ok,
        ok ? `via ${conn.transport}` : `unexpected message: ${JSON.stringify(message)}`,
      );
    } finally {
      conn.close();
    }
  }

  // --- Jobs: connect to A, start the job through B ------------------------
  {
    const jobId = globalThis.crypto.randomUUID();
    const receiver = await connectRealtimeChannel(baseUrlA, 'jobs', accessToken, jobId);
    let starter;
    try {
      await new Promise((r) => setTimeout(r, 200));
      // .NET: StartDemoJob only needs a hub connection to invoke it on,
      // not a subscription — the invocation runs on whichever instance
      // receives it. Python: starting is a message sent on the job's own
      // connection, so a second connection to instance B is opened for it.
      starter = await connectRealtimeChannel(baseUrlB, 'jobs', accessToken, jobId);
      starter.start();

      const frames = [];
      for (let i = 0; i < 4; i += 1) frames.push(await receiver.waitForMessage(5_000));
      const last = frames[frames.length - 1];
      const ok = frames.every((f) => f.type === 'job.progress' && f.jobId === jobId) && last.status === 'succeeded' && last.progress === 100;
      report('jobs fan-out (job.progress, started on B, received on A)', ok, ok ? `via ${receiver.transport}, ${frames.length} frames` : `frames: ${JSON.stringify(frames)}`);
    } finally {
      receiver.close();
      starter?.close();
    }
  }

  console.log(`\n${failures === 0 ? 'All fan-out checks passed.' : `${failures} fan-out check(s) FAILED.`}`);
  process.exit(failures === 0 ? 0 : 1);
}

main().catch((err) => {
  console.error('\nFan-out verification could not run to completion:', err.message);
  process.exit(1);
});
