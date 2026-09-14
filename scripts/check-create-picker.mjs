#!/usr/bin/env node
// Proves the `create` picker (create/create.mjs) for every combination of
// backend x database x frontend x mobile that exists in this checkout:
//
//   1. Generate into a fresh temp directory.
//   2. Grep the generated tree for every OTHER stack's names and paths —
//      fail on any hit. `contract/openapi.yaml` is exempt: it is the one
//      hand-written, canonical, shared source of truth every backend
//      implements and every client generates from, and it necessarily
//      documents both backends' realtime wiring (see its own
//      `x-realtime-channels` extension) so that whichever backend a user
//      later adds to the same contract stays truthful. It is never a
//      config file, README, playbook or lockfile — the artifact classes
//      the picker's own leak guarantee is about.
//   3. Regenerate the same choices again into a second temp directory and
//      diff the two byte-for-byte — the same choices must produce the
//      same tree.
//
// Zero runtime dependencies (Node built-ins only), and run as part of
// `scripts/check-client-drift.sh` — see that script's own comment for why
// this test lives there rather than in a new CI workflow file.
//
// Usage: node scripts/check-create-picker.mjs

import { execFileSync } from 'node:child_process';
import { mkdtempSync, rmSync, readdirSync, statSync, readFileSync } from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { discoverStacks } from '../create/lib/discover.mjs';
import { generate } from '../create/create.mjs';

const rootDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');

const STACK_TOKENS = {
  dotnet: [/\.NET\b/gi, /\bdotnet\b/gi],
  python: [/\bpython\b/gi],
  angular: [/\bangular\b/gi],
  nextjs: [/\bnext\.js\b/gi, /\bnextjs\b/gi],
  flutter: [/\bflutter\b/gi],
};

// Files that are allowed to name every stack because they are the single
// shared, canonical source every stack is generated from or measured
// against — see the module comment above.
const EXEMPT_RELATIVE_PATHS = new Set(['contract/openapi.yaml']);

// Machine-generated dependency manifests. An entry here naming a stack is
// not a StackBraid stack leak — it is an unrelated third-party package's
// own metadata (e.g. npm's `argparse` reports its licence as the SPDX id
// "Python-2.0"; that is a licence identifier, not a reference to this
// project's Python backend). These files must also never be hand-edited:
// doing so would break their integrity hashes. A real leak here — this
// project's own dependency on a folder outside what was chosen — would
// show up as a path (e.g. "frontends/nextjs") and is covered separately.
const EXEMPT_LOCKFILE_NAMES = new Set([
  'package-lock.json',
  'pubspec.lock',
  'packages.lock.json',
  'requirements-lock.txt',
]);

const STACK_PATHS = {
  dotnet: ['backends/dotnet'],
  python: ['backends/python'],
  angular: ['frontends/angular'],
  nextjs: ['frontends/nextjs'],
  flutter: ['mobile/flutter', 'clients/dart'],
};

function listFilesRecursive(dir) {
  const out = [];
  function walk(rel) {
    const abs = path.join(dir, rel);
    for (const entry of readdirSync(abs, { withFileTypes: true })) {
      const childRel = rel ? `${rel}/${entry.name}` : entry.name;
      if (entry.isSymbolicLink()) continue;
      if (entry.isDirectory()) {
        walk(childRel);
      } else {
        out.push(childRel);
      }
    }
  }
  walk('');
  return out;
}

function grepForLeaks(targetDir, chosenStacks) {
  const chosenSet = new Set(chosenStacks);
  const forbiddenStacks = Object.keys(STACK_TOKENS).filter((s) => !chosenSet.has(s));
  const findings = [];

  for (const relPath of listFilesRecursive(targetDir)) {
    if (EXEMPT_RELATIVE_PATHS.has(relPath)) continue;
    const abs = path.join(targetDir, relPath);
    const baseName = path.basename(relPath);
    const isLockfile = EXEMPT_LOCKFILE_NAMES.has(baseName);
    let text;
    try {
      text = readFileSync(abs, 'utf8');
    } catch {
      continue; // binary file — nothing this test can usefully grep
    }
    for (const stack of forbiddenStacks) {
      if (isLockfile) {
        // Only a literal path leak matters in a lockfile — see
        // EXEMPT_LOCKFILE_NAMES above for why a bare word does not.
        for (const p of STACK_PATHS[stack] ?? []) {
          if (text.includes(p)) {
            findings.push(`${relPath}: leaked path reference to "${stack}" (found "${p}")`);
          }
        }
        continue;
      }
      for (const re of STACK_TOKENS[stack]) {
        re.lastIndex = 0;
        const match = re.exec(text);
        if (match) {
          const line = text.slice(0, match.index).split('\n').length;
          findings.push(`${relPath}:${line}: leaked reference to "${stack}" (matched "${match[0]}")`);
        }
      }
    }
  }
  return findings;
}

function diffTrees(dirA, dirB) {
  const filesA = new Set(listFilesRecursive(dirA));
  const filesB = new Set(listFilesRecursive(dirB));
  const diffs = [];

  for (const f of filesA) if (!filesB.has(f)) diffs.push(`only in first generation: ${f}`);
  for (const f of filesB) if (!filesA.has(f)) diffs.push(`only in second generation: ${f}`);

  for (const f of filesA) {
    if (!filesB.has(f)) continue;
    const a = readFileSync(path.join(dirA, f));
    const b = readFileSync(path.join(dirB, f));
    if (!a.equals(b)) diffs.push(`content differs: ${f}`);
  }
  return diffs;
}

function allCombinations(stacks) {
  const combos = [];
  for (const backend of stacks.backends) {
    for (const database of stacks.databasesForBackend(backend)) {
      for (const frontend of [...stacks.frontends, 'none']) {
        for (const mobile of ['flutter', 'none']) {
          if (mobile === 'flutter' && !stacks.mobilePlatforms.includes('flutter')) continue;
          combos.push({ name: 'e2e-test-app', backend, database, frontend, mobile });
        }
      }
    }
  }
  return combos;
}

function main() {
  const stacks = discoverStacks(rootDir);
  const combos = allCombinations(stacks);
  console.log(`Testing ${combos.length} combination(s): backend x database x frontend x mobile.\n`);

  let failures = 0;
  const tmpBase = mkdtempSync(path.join(os.tmpdir(), 'stackbraid-create-test-'));

  try {
    for (const choices of combos) {
      const label = `${choices.backend}/${choices.database}/${choices.frontend}/${choices.mobile}`;
      const dirA = path.join(tmpBase, `${label.replace(/\//g, '-')}-a`);
      const dirB = path.join(tmpBase, `${label.replace(/\//g, '-')}-b`);

      generate(rootDir, choices, dirA);

      const chosenStacks = [choices.backend];
      if (choices.frontend !== 'none') chosenStacks.push(choices.frontend);
      if (choices.mobile !== 'none') chosenStacks.push(choices.mobile);

      const leaks = grepForLeaks(dirA, chosenStacks);
      if (leaks.length > 0) {
        failures += 1;
        console.log(`FAIL  ${label} — ${leaks.length} leaked reference(s):`);
        for (const finding of leaks.slice(0, 20)) console.log(`      ${finding}`);
        if (leaks.length > 20) console.log(`      ... and ${leaks.length - 20} more`);
        continue;
      }

      generate(rootDir, choices, dirB);
      const diffs = diffTrees(dirA, dirB);
      if (diffs.length > 0) {
        failures += 1;
        console.log(`FAIL  ${label} — regeneration is not identical (${diffs.length} difference(s)):`);
        for (const d of diffs.slice(0, 20)) console.log(`      ${d}`);
        continue;
      }

      console.log(`OK    ${label} — no leaks, identical on regeneration.`);
    }
  } finally {
    rmSync(tmpBase, { recursive: true, force: true });
  }

  console.log();
  if (failures > 0) {
    console.log(`create picker check FAILED: ${failures}/${combos.length} combination(s) failed.`);
    process.exitCode = 1;
  } else {
    console.log(`create picker check passed: all ${combos.length} combination(s) clean and reproducible.`);
  }
}

const isMain = process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) main();
