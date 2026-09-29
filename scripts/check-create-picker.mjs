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
//   4. Check the agent playbooks, which are filtered to the chosen stacks
//      (see checkPlaybooks): a playbook naming a stack the project does not
//      contain fails, a playbook written for stacks the project does not have
//      that was installed fails, a playbook that does apply and was not
//      installed fails, and the generated AGENTS.md may only link playbooks
//      that are actually there.
//   5. Check the honesty of a blocked combination (see
//      checkBlockedIsDeclared and checkPromptLabels): a backend x database
//      pair this repository documents as not working must be labelled as such
//      at the prompt and must say so in the generated README and AGENTS.md,
//      and a pair documented as working must claim neither.
//
// Zero runtime dependencies (Node built-ins only), and run as part of
// `scripts/check-client-drift.sh` — see that script's own comment for why
// this test lives there rather than in a new CI workflow file.
//
// Usage: node scripts/check-create-picker.mjs

import { mkdtempSync, rmSync, readdirSync, readFileSync, existsSync } from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { discoverStacks, blockedReason } from '../create/lib/discover.mjs';
import { databaseChoices, generate } from '../create/create.mjs';
import { ALL_STACKS, stackRegexes, stacksNamedIn } from '../create/lib/stacks.mjs';
import { listPlaybookNames, playbookScope, playbookStacks, planPlaybooks } from '../create/lib/playbooks.mjs';

const rootDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');

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
  const forbiddenStacks = ALL_STACKS.filter((s) => !chosenSet.has(s));
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
      for (const re of stackRegexes(stack)) {
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

const PLAYBOOKS_RELATIVE_DIR = path.join('.agent', 'playbooks');

/**
 * The agent playbooks in a generated project, checked three ways.
 *
 * 1. No installed playbook names a stack the project did not choose. This is
 *    the point of the whole exercise: an agent must never be handed a
 *    procedure for code that is not in the project.
 * 2. The install set is exactly the set of playbooks whose declared scope
 *    includes a stack the project has. Both directions fail: a playbook for
 *    absent stacks that is installed, and a playbook that applies to the
 *    project that is missing. Without the second direction, "install nothing
 *    at all" would satisfy check 1 forever.
 * 3. The generated AGENTS.md links exactly the playbooks that are installed —
 *    no link to a missing file, and no installed playbook left unmentioned.
 *
 * The expected install set is recomputed here from the same repository
 * sources the picker reads, not copied from the picker's own plan, so a
 * picker change that quietly stops filtering fails this check.
 */
function checkPlaybooks(targetDir, choices) {
  const chosen = new Set(
    [choices.backend, choices.frontend, choices.mobile].filter((c) => c && c !== 'none'),
  );
  const findings = [];
  const dir = path.join(targetDir, PLAYBOOKS_RELATIVE_DIR);

  const installed = existsSync(dir)
    ? readdirSync(dir)
        .filter((entry) => entry.endsWith('.md'))
        .sort()
    : [];
  const known = new Set(listPlaybookNames(rootDir));

  for (const name of installed) {
    if (!known.has(name)) {
      findings.push(`${PLAYBOOKS_RELATIVE_DIR}/${name}: not a playbook in this repository`);
      continue;
    }
    const text = readFileSync(path.join(dir, name), 'utf8');
    for (const stack of stacksNamedIn(text)) {
      if (!chosen.has(stack)) {
        findings.push(
          `${PLAYBOOKS_RELATIVE_DIR}/${name}: playbook for the unchosen stack "${stack}" was installed`,
        );
      }
    }
  }

  // Read the applicability of each source playbook from its own declared
  // scope, so a playbook for stacks the project does not have cannot be
  // installed, and one that does apply cannot be skipped.
  for (const name of listPlaybookNames(rootDir)) {
    let scope;
    try {
      scope = playbookScope(rootDir, name);
    } catch (err) {
      findings.push(err.message);
      continue;
    }
    const applies = scope.some((stack) => chosen.has(stack));
    if (applies && !installed.includes(name)) {
      findings.push(
        `${PLAYBOOKS_RELATIVE_DIR}/${name}: written for [${scope.join(', ')}], at least one of which this project has, but it was not installed`,
      );
    }
    if (!applies && installed.includes(name)) {
      findings.push(
        `${PLAYBOOKS_RELATIVE_DIR}/${name}: written for [${scope.join(', ')}], none of which this project has, yet it was installed`,
      );
    }
  }

  // What the picker itself planned, as a cross-check that the two views of
  // the same rule agree.
  const planned = planPlaybooks(rootDir, chosen).map((p) => p.name).sort();
  if (planned.join(',') !== installed.join(',')) {
    findings.push(
      `installed playbooks [${installed.join(', ') || 'none'}] do not match the filtered plan [${planned.join(', ') || 'none'}]`,
    );
  }

  const agents = readFileSync(path.join(targetDir, 'AGENTS.md'), 'utf8');
  for (const name of known) {
    const linked = agents.includes(`.agent/playbooks/${name}`);
    if (linked && !installed.includes(name)) {
      findings.push(`AGENTS.md links ${name}, which is not installed in this project`);
    }
    if (!linked && installed.includes(name)) {
      findings.push(`AGENTS.md does not link ${name}, which is installed in this project`);
    }
  }

  return findings;
}

/**
 * A combination this repository documents as not working must say so in the
 * project it generates; a combination documented as working must not claim
 * the opposite.
 */
function checkBlockedIsDeclared(targetDir, choices) {
  const findings = [];
  const reason = blockedReason(choices.backend, choices.database);
  const readme = readFileSync(path.join(targetDir, 'README.md'), 'utf8');
  const agents = readFileSync(path.join(targetDir, 'AGENTS.md'), 'utf8');

  // A fixed phrase, not the reason text: the check proves the warning is
  // there, and says nothing about how it is worded.
  const declaration = 'does not build';
  if (reason === null) {
    if (readme.includes(declaration) || agents.includes(declaration)) {
      findings.push(
        `README.md/AGENTS.md declares "${declaration}" for ${choices.backend} + ${choices.database}, which is not a blocked combination`,
      );
    }
    return findings;
  }

  if (!readme.includes(declaration)) {
    findings.push(
      `README.md does not declare that ${choices.backend} + ${choices.database} does not build, though it is a blocked combination`,
    );
  }
  if (!agents.includes(declaration)) {
    findings.push(
      `AGENTS.md does not declare that ${choices.backend} + ${choices.database} does not build, though it is a blocked combination`,
    );
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

/**
 * A provider this repository documents as not working must be labelled as such
 * in the prompt, and a provider that works must not be. Checked on the
 * labels the picker builds, because a notice in the generated README is no
 * use to a user who has not chosen that combination yet.
 */
function checkPromptLabels(stacks) {
  const findings = [];
  for (const backend of stacks.backends) {
    for (const { value, label } of databaseChoices(backend, stacks.databasesForBackend(backend))) {
      const blocked = blockedReason(backend, value) !== null;
      const labelled = label.toLowerCase().includes('blocked');
      if (blocked && !labelled) {
        findings.push(`database option "${value}" is offered for ${backend} without a "blocked" label`);
      }
      if (!blocked && labelled) {
        findings.push(`database option "${value}" is offered for ${backend} labelled "blocked", though it is not`);
      }
    }
  }
  return findings;
}

function main() {
  const stacks = discoverStacks(rootDir);
  const combos = allCombinations(stacks);

  // A playbook must declare the stacks it is for, and must not talk about a
  // stack it has not declared: an under-declared scope installs the playbook
  // for a project it was never meant to advise, and the block filter cannot
  // catch it, because the text of the offending block may name no stack at
  // all. Checked once, over the source playbooks, before any combination.
  const scopeProblems = [];
  for (const name of listPlaybookNames(rootDir)) {
    let scope;
    try {
      scope = playbookScope(rootDir, name);
    } catch (err) {
      // A playbook added without a declared scope: reported here rather than
      // thrown out of the picker half-way through the first combination.
      scopeProblems.push(err.message);
      continue;
    }
    for (const stack of playbookStacks(rootDir, name)) {
      if (!scope.includes(stack)) {
        scopeProblems.push(
          `${name}: text names "${stack}", which its declared scope [${scope.join(', ')}] does not include`,
        );
      }
    }
  }
  if (scopeProblems.length > 0) {
    console.log('FAIL  playbook scopes — declared scope and text disagree:');
    for (const problem of scopeProblems) console.log(`      ${problem}`);
    console.log();
    process.exitCode = 1;
    return;
  }

  console.log(`Testing ${combos.length} combination(s): backend x database x frontend x mobile.\n`);

  const labelProblems = checkPromptLabels(stacks);
  if (labelProblems.length > 0) {
    console.log('FAIL  database options — a blocked provider is not labelled, or a working one is:');
    for (const problem of labelProblems) console.log(`      ${problem}`);
    console.log();
    process.exitCode = 1;
    return;
  }

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

      const playbookFindings = checkPlaybooks(dirA, choices);
      if (playbookFindings.length > 0) {
        failures += 1;
        console.log(`FAIL  ${label} — ${playbookFindings.length} playbook problem(s):`);
        for (const finding of playbookFindings) console.log(`      ${finding}`);
        continue;
      }

      const blockedFindings = checkBlockedIsDeclared(dirA, choices);
      if (blockedFindings.length > 0) {
        failures += 1;
        console.log(`FAIL  ${label} — ${blockedFindings.length} undeclared limitation(s):`);
        for (const finding of blockedFindings) console.log(`      ${finding}`);
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

      console.log(
        `OK    ${label} — no leaks, playbooks filtered, identical on regeneration.`,
      );
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
