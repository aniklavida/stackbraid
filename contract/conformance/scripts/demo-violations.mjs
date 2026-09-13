// Proves the conformance suite actually fails, and fails for the right
// reasons. "A suite that has never failed is a suite nobody should trust."
//
// For a baseline (no injected violations) and each documented violation
// class, this spawns the stub fixture with that class injected, runs the
// real suite against it, and checks the exit code matches what should
// happen — 0 for the baseline, non-zero for every injected violation. It
// also prints the specific [FAIL] lines so a human can see the suite named
// the actual field and expected value, not just "assertion failed".
//
// This IS the CI gate for the conformance suite itself: it exercises only
// the stub fixture, never a real backend, and its passing is not evidence
// any real backend conforms to anything.

import { spawn } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const stubPath = path.join(__dirname, '..', 'fixtures', 'stub-server', 'server.mjs');
const runnerPath = path.join(__dirname, '..', 'cli', 'run.mjs');

const SCENARIOS = [
  { name: 'baseline (no injected violations)', violations: '', expectExit: 0 },
  { name: 'wrong type', violations: 'wrong-type', expectExit: 1 },
  { name: 'missing field', violations: 'missing-field', expectExit: 1 },
  { name: 'wrong timestamp format (+00:00 instead of Z)', violations: 'bad-timestamp', expectExit: 1 },
  { name: 'wrong error shape (not RFC 9457)', violations: 'bad-error-shape', expectExit: 1 },
  { name: 'wrong pagination (cursor instead of offset)', violations: 'bad-pagination', expectExit: 1 },
];

function runNode(script, args, env) {
  return new Promise((resolve) => {
    const child = spawn(process.execPath, [script, ...args], { stdio: ['ignore', 'pipe', 'pipe'], env });
    let stdout = '';
    let stderr = '';
    child.stdout.on('data', (d) => (stdout += d));
    child.stderr.on('data', (d) => (stderr += d));
    child.on('close', (code) => resolve({ exitCode: code, stdout, stderr }));
  });
}

async function waitForServer(baseUrl, timeoutMs = 5000) {
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    try {
      await fetch(`${baseUrl}/v1/auth/me`);
      return;
    } catch {
      await new Promise((r) => setTimeout(r, 100));
    }
  }
  throw new Error(`stub server did not become ready at ${baseUrl} within ${timeoutMs}ms`);
}

async function stopServer(child) {
  if (child.exitCode !== null) return;
  child.kill();
  await new Promise((resolve) => {
    if (child.exitCode !== null) return resolve();
    child.once('exit', resolve);
  });
}

let port = 4300;
let overallOk = true;
const summaries = [];

for (const scenario of SCENARIOS) {
  port += 1;
  const baseUrl = `http://127.0.0.1:${port}`;
  const stub = spawn(process.execPath, [stubPath], {
    env: { ...process.env, PORT: String(port), STUB_VIOLATIONS: scenario.violations, STUB_ACCESS_TTL_MS: '5000' },
    stdio: ['ignore', 'pipe', 'pipe'],
  });

  await waitForServer(baseUrl);
  const { exitCode, stdout } = await runNode(runnerPath, [baseUrl]);
  await stopServer(stub);

  const ok = exitCode === scenario.expectExit;
  overallOk = overallOk && ok;

  console.log(`\n${'='.repeat(78)}`);
  console.log(`Scenario: ${scenario.name}`);
  console.log(`STUB_VIOLATIONS=${scenario.violations || '(none)'}  expected exit ${scenario.expectExit}, got ${exitCode}  ${ok ? 'OK' : 'MISMATCH'}`);
  console.log('='.repeat(78));

  const failLines = stdout
    .split('\n')
    .filter((line) => line.includes('[FAIL]') || /^\s+(field:|expected:|actual:|-\s)/.test(line));
  if (failLines.length > 0) {
    console.log(failLines.join('\n'));
  } else {
    console.log(stdout.trim().split('\n').slice(-3).join('\n'));
  }

  summaries.push({ ...scenario, exitCode, ok, failureCount: (stdout.match(/\[FAIL\]/g) || []).length });
}

console.log(`\n${'='.repeat(78)}`);
console.log('Summary');
console.log('='.repeat(78));
for (const s of summaries) {
  console.log(`${s.ok ? 'OK  ' : 'FAIL'}  ${s.name.padEnd(48)} exit=${s.exitCode} failures-reported=${s.failureCount}`);
}

console.log(`\n${overallOk ? 'Every scenario behaved as expected.' : 'Some scenarios did NOT behave as expected — see above.'}`);
process.exit(overallOk ? 0 : 1);
