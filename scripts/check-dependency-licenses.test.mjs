#!/usr/bin/env node
// Tests for the AGENTS.md class rule enforced by
// scripts/check-dependency-licenses.mjs.
//
// Issue #18 found that the licence gate only checked the inventory, not the
// policy: QuestPDF's "Community" licence — free only under USD 1M annual gross
// revenue, and unavailable to public companies and governments — passed CI
// because the script's own allow-list carried its licence string and the
// inventory recorded the entry as audited. The gate now decides on the licence
// and the class alone, and these tests hold it to that.
//
// Run with: node --test scripts/
// Zero runtime dependencies (node:test, node:assert) — the script under test
// has none either, and a test harness that needed its own licence audit would
// be a poor advertisement for this file.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(scriptDir, '..');
const script = path.join(scriptDir, 'check-dependency-licenses.mjs');

function runGate(fixtureName) {
  const result = spawnSync(
    process.execPath,
    [script, '--policy-only', '--inventory', path.join(scriptDir, 'fixtures', fixtureName)],
    { cwd: repoRoot, encoding: 'utf8' },
  );
  return { status: result.status, output: `${result.stdout}${result.stderr}` };
}

test('rejects a revenue-gated licence compiled into user code', () => {
  const { status, output } = runGate('dependency-inventory.revenue-gated.json');

  assert.equal(status, 1, `expected the gate to fail; it said:\n${output}`);

  // It must name the dependency...
  assert.match(output, /RevenuePDF@2026\.1\.0/, `the failure did not name the entry:\n${output}`);
  // ...state the rule that rejected it, not merely "bad licence"...
  assert.match(output, /REVENUE-GATED LICENCE/, `the failure did not classify the licence:\n${output}`);
  assert.match(output, /AGENTS\.md/, `the failure did not cite the policy:\n${output}`);
  // ...and be explicit that an inventory entry cannot record the problem away,
  //    which is the exact loophole #18 described.
  assert.match(output, /cannot record it away/, `the failure did not address the loophole:\n${output}`);
});

test('does not report the permissive or separate-process entries in the same fixture', () => {
  const { output } = runGate('dependency-inventory.revenue-gated.json');

  assert.doesNotMatch(output, /Acme\.Reporting/, 'a plain MIT compiled entry was reported');
  assert.doesNotMatch(output, /Acme\.Pdf/, 'a copyleft separate-process entry was reported');
  assert.equal(output.match(/REVENUE-GATED LICENCE/g)?.length, 1, 'expected exactly one violation');
});

test('passes an inventory whose entries all comply with the policy', () => {
  const { status, output } = runGate('dependency-inventory.compliant.json');

  assert.equal(status, 0, `expected the gate to pass; it said:\n${output}`);
});

test('the real inventory satisfies the policy rule', () => {
  const result = spawnSync(process.execPath, [script, '--policy-only'], { cwd: repoRoot, encoding: 'utf8' });

  assert.equal(result.status, 0, `the shipped inventory violates the policy:\n${result.stdout}${result.stderr}`);
});

test('QuestPDF cannot be reintroduced by recording it as audited', () => {
  // The regression this whole change exists for: the exact entry that shipped
  // until #18 was fixed — QuestPDF Community, audited, compiled into the
  // running backend, behind IPdfGenerator so consumers could swap it. The
  // package reference is gone from backends/dotnet; this asserts the gate
  // rejects the licence on its own terms if anyone puts it back. Note it is
  // rejected for what the licence says, not because "QuestPDF" is spelled out
  // anywhere in the script — a blocklist of package names would not have
  // generalised to the fake entry in the fixture above.
  const { status, output } = runGate('dependency-inventory.questpdf-readmitted.json');

  assert.equal(status, 1, `expected the gate to fail; it said:\n${output}`);
  assert.match(output, /REVENUE-GATED LICENCE/, `the failure did not classify the licence:\n${output}`);
  assert.match(output, /QuestPDF@2024\.12\.3/, `the failure did not name the entry:\n${output}`);
});
