// Orchestrates the conformance run: a connectivity preflight, then every
// registered check group, then the report.

import { createHarness, printReport } from './report.mjs';
import { registerAuthChecks } from './checks/auth.mjs';
import { registerErrorEnvelopeChecks } from './checks/errors.mjs';
import { registerUserChecks } from './checks/users.mjs';
import { registerRoleChecks } from './checks/roles.mjs';
import { registerRealtimeChecks } from './checks/realtime.mjs';

// Order matters: auth registers the primary test user and hands off a live
// session (`ctx.primaryUser.activeTokens`) that users/roles/realtime checks
// reuse. Realtime runs last — its own checks deactivate and reassign roles
// on the primary user, which nothing after it depends on being untouched.
const CHECK_GROUPS = [registerAuthChecks, registerErrorEnvelopeChecks, registerUserChecks, registerRoleChecks, registerRealtimeChecks];

export async function runConformanceSuite(baseUrl) {
  try {
    await fetch(new URL('/v1/auth/me', baseUrl));
  } catch (err) {
    console.error(`\nCould not reach ${baseUrl}: ${err && err.message ? err.message : err}`);
    console.error('Nothing else can run without a live server at the given base URL.\n');
    return { passCount: 0, failureCount: 1, skipCount: 0 };
  }

  const harness = createHarness();
  const ctx = { baseUrl };

  for (const group of CHECK_GROUPS) {
    await group(harness, ctx);
  }

  return printReport(baseUrl, harness.results);
}
