// The test harness: runs named checks, catches their failures without
// aborting the run, and prints a report a human can act on at 2am.

import { ConformanceFailure } from './assert.mjs';

/** Thrown by a check to mark it skipped (not passed, not failed) with a reason. */
export class Skip extends Error {
  constructor(message) {
    super(message);
    this.skip = true;
  }
}

export function createHarness() {
  const results = [];
  return {
    results,
    /**
     * @param {string} name
     * @param {() => Promise<void> | void} fn
     */
    async run(name, fn) {
      const start = Date.now();
      try {
        await fn();
        results.push({ name, status: 'pass', ms: Date.now() - start });
      } catch (err) {
        const ms = Date.now() - start;
        if (err instanceof ConformanceFailure) {
          results.push({ name, status: 'fail', ms, message: err.message });
        } else if (err && err.skip) {
          results.push({ name, status: 'skip', ms, message: err.message });
        } else {
          const detail = err && err.stack ? err.stack : String(err);
          results.push({ name, status: 'fail', ms, message: `unexpected error (not a contract assertion): ${detail}` });
        }
      }
    },
  };
}

export function printReport(baseUrl, results) {
  const pass = results.filter((r) => r.status === 'pass');
  const failed = results.filter((r) => r.status === 'fail');
  const skipped = results.filter((r) => r.status === 'skip');

  console.log(`\nStackBraid Identity conformance suite — target: ${baseUrl}\n`);
  for (const r of results) {
    const label = r.status === 'pass' ? 'PASS' : r.status === 'fail' ? 'FAIL' : 'SKIP';
    console.log(`[${label}] ${r.name} (${r.ms}ms)`);
    if (r.status !== 'pass' && r.message) {
      for (const line of r.message.split('\n')) console.log(`       ${line}`);
    }
  }

  console.log(`\n${pass.length} passed, ${failed.length} failed, ${skipped.length} skipped, ${results.length} total.\n`);

  return { passCount: pass.length, failureCount: failed.length, skipCount: skipped.length };
}
