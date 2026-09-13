// Orchestrates the conformance run: a connectivity preflight, then every
// registered check group, then the report.
//
// Deliberately empty of checks for now — this file is the runner's
// scaffolding. The checks themselves (auth, users, roles, the error
// envelope) land in a follow-up commit and register themselves here.

import { createHarness, printReport } from './report.mjs';

const CHECK_GROUPS = [];

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
