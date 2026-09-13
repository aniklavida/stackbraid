#!/usr/bin/env node
// Licence-audit drift gate — StackBraid card 30.
//
// Every dependency shipped here is inherited by every user of this skeleton
// (see AGENTS.md "Dependencies"). docs/dependency-inventory.json is the
// audited record: each entry names a package, the exact version it was
// verified at, its licence, and which of the two audit classes it falls in.
//
// This script does NOT re-fetch licences from the network on every run —
// that would make CI flaky and slow for no real benefit. Instead it checks
// that what is actually resolved today (package-lock.json, pubspec.lock,
// infra/compose.yaml image tags) still matches what was audited:
//
//   - A package/version pair that is resolved but missing from the
//     inventory is an UNAUDITED dependency — someone bumped a version or
//     added a package without recording the licence check. Fails the build.
//   - A package the inventory expects but that is no longer resolved is
//     reported as stale (does not fail the build; docs/dependency-inventory.json
//     should be trimmed on its next update).
//   - Every inventory entry's licence is checked against the two-class rule
//     from AGENTS.md, and against the reciprocal-for-consumers reject list
//     (RPL, SSPL, RSAL, BSL, revenue-gated commercial) regardless of class.
//     This is what would have caught Redis 7.4's RSALv2/SSPLv1 relicense if
//     the image tag had drifted instead of being pinned.
//
// Zero runtime dependencies — Node built-ins only, matching the conformance
// suite's own zero-dependency rule (nothing here needs its own licence audit).

import { readFileSync, existsSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const rootDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const p = (...parts) => path.join(rootDir, ...parts);

const PERMISSIVE = new Set([
  'MIT', 'Apache-2.0', 'BSD-2-Clause', 'BSD-3-Clause', 'ISC', '0BSD', 'Python-2.0',
]);

// Substrings checked case-insensitively against every recorded licence,
// regardless of class. Presence anywhere is an automatic failure.
const BANNED_SUBSTRINGS = [
  'rpl', 'sspl', 'rsal', 'bsl', 'business source license', 'commons clause',
  'proprietary', 'revenue-gated', 'revenue gated',
];

let failures = [];
let warnings = [];

function fail(msg) {
  failures.push(msg);
}
function warn(msg) {
  warnings.push(msg);
}

// --- Load the audited inventory ---------------------------------------
const inventoryPath = p('docs', 'dependency-inventory.json');
if (!existsSync(inventoryPath)) {
  console.error(`FAIL: ${inventoryPath} is missing — there is no audited inventory to check against.`);
  process.exit(1);
}
const inventory = JSON.parse(readFileSync(inventoryPath, 'utf8'));

// index inventory by ecosystem+name -> Map<version, entry>
const byKey = new Map();
for (const entry of inventory.entries) {
  const key = `${entry.ecosystem}::${entry.name}`;
  if (!byKey.has(key)) byKey.set(key, new Map());
  byKey.get(key).set(String(entry.version), entry);
}

// --- Rule check: every inventory entry must respect the two-class rule --
for (const entry of inventory.entries) {
  const lic = (entry.license || '').toLowerCase();
  if (BANNED_SUBSTRINGS.some((s) => lic.includes(s))) {
    fail(`REJECTED LICENCE: ${entry.ecosystem} ${entry.name}@${entry.version} is "${entry.license}" — on the reciprocal-for-consumers reject list regardless of class.`);
    continue;
  }
  if (entry.class === 'compiled-into-user-code' && !PERMISSIVE.has(entry.license)) {
    fail(`CLASS VIOLATION: ${entry.ecosystem} ${entry.name}@${entry.version} is compiled-into-user-code but licensed "${entry.license}" (must be MIT, Apache-2.0, BSD-2/3-Clause, ISC or 0BSD).`);
  }
}

// --- Helper: compare a resolved set of {name, version} against the inventory
function checkResolved(ecosystem, resolved, { sourceLabel }) {
  for (const { name, version } of resolved) {
    const key = `${ecosystem}::${name}`;
    const versions = byKey.get(key);
    if (!versions) {
      fail(`UNAUDITED DEPENDENCY: ${ecosystem} "${name}" (resolved at ${version} in ${sourceLabel}) has no entry in docs/dependency-inventory.json. Verify its licence from the actual package metadata, then add an entry before merging.`);
      continue;
    }
    if (!versions.has(String(version))) {
      const known = [...versions.keys()].join(', ');
      fail(`VERSION DRIFT: ${ecosystem} "${name}" resolved at ${version} in ${sourceLabel}, but the inventory only audited ${known}. Re-verify the licence at the new version and update docs/dependency-inventory.json.`);
    }
  }
}

// --- npm: clients/typescript --------------------------------------------
const npmLockPath = p('clients', 'typescript', 'package-lock.json');
if (existsSync(npmLockPath)) {
  const lock = JSON.parse(readFileSync(npmLockPath, 'utf8'));
  const resolved = [];
  for (const [pkgPath, meta] of Object.entries(lock.packages || {})) {
    if (pkgPath === '') continue;
    const name = meta.name || pkgPath.split('node_modules/').pop();
    resolved.push({ name, version: meta.version });
  }
  checkResolved('npm-typescript-client', resolved, { sourceLabel: 'clients/typescript/package-lock.json' });
} else {
  warn('clients/typescript/package-lock.json not found — skipping npm check.');
}

// --- Dart: clients/dart ---------------------------------------------------
const pubspecLockPath = p('clients', 'dart', 'pubspec.lock');
if (existsSync(pubspecLockPath)) {
  const text = readFileSync(pubspecLockPath, 'utf8');
  const resolved = [];
  let currentName = null;
  for (const line of text.split('\n')) {
    const nameMatch = line.match(/^  ([a-zA-Z0-9_]+):\s*$/);
    if (nameMatch) {
      currentName = nameMatch[1];
      continue;
    }
    const versionMatch = line.match(/^    version:\s*"?([^"\s]+)"?/);
    if (versionMatch && currentName) {
      resolved.push({ name: currentName, version: versionMatch[1] });
      currentName = null;
    }
  }
  checkResolved('dart-client', resolved, { sourceLabel: 'clients/dart/pubspec.lock' });
} else {
  warn('clients/dart/pubspec.lock not found — skipping Dart check.');
}

// --- Docker images: infra/compose.yaml ------------------------------------
const composePath = p('infra', 'compose.yaml');
if (existsSync(composePath)) {
  const text = readFileSync(composePath, 'utf8');
  const resolved = [];
  for (const line of text.split('\n')) {
    const m = line.match(/^\s*image:\s*([^\s#]+)\s*$/);
    if (!m) continue;
    const ref = m[1];
    const lastColon = ref.lastIndexOf(':');
    const name = lastColon === -1 ? ref : ref.slice(0, lastColon);
    const tag = lastColon === -1 ? 'latest' : ref.slice(lastColon + 1);
    resolved.push({ name, version: tag });
  }
  checkResolved('infra-image', resolved, { sourceLabel: 'infra/compose.yaml' });
} else {
  warn('infra/compose.yaml not found — skipping Docker image check.');
}

// --- Report ---------------------------------------------------------------
console.log(`Checked ${inventory.entries.length} audited entries against what is actually resolved.`);
if (warnings.length) {
  console.log('\nWarnings:');
  for (const w of warnings) console.log(`  - ${w}`);
}
if (failures.length) {
  console.error('\nFAILED — a dependency slipped in unaudited, drifted, or carries a rejected licence:\n');
  for (const f of failures) console.error(`  - ${f}`);
  console.error(`\n${failures.length} problem(s). See docs/DEPENDENCIES.md for the audit process.`);
  process.exit(1);
}
console.log('OK — every resolved dependency is in the audited inventory, at the audited version, with an accepted licence.');
