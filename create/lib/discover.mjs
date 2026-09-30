// Discovers which stacks and providers actually exist in this checkout.
//
// Never hard-codes "the four database providers" or "the two frontends" —
// it reads the filesystem, so create only ever offers what it can actually
// copy today. If a fifth stack or a second database provider is added
// later, this file needs no change.

import { existsSync, readdirSync, statSync } from 'node:fs';
import path from 'node:path';

function listDirs(dir) {
  if (!existsSync(dir)) return [];
  return readdirSync(dir, { withFileTypes: true })
    .filter((entry) => entry.isDirectory() && !entry.name.startsWith('.'))
    .map((entry) => entry.name)
    .sort();
}

/**
 * @param {string} repoRoot
 */
export function discoverStacks(repoRoot) {
  const backends = listDirs(path.join(repoRoot, 'backends'));

  const databasesByBackend = {};
  for (const backend of backends) {
    if (backend === 'dotnet') {
      databasesByBackend.dotnet = listDirs(
        path.join(repoRoot, 'backends/dotnet/src/Database'),
      ).map((name) => name.toLowerCase());
    } else if (backend === 'python') {
      databasesByBackend.python = listDirs(
        path.join(repoRoot, 'backends/python/src/app/database'),
      )
        .filter((name) => name !== '__pycache__')
        .map((name) => name.toLowerCase());
    } else {
      databasesByBackend[backend] = [];
    }
  }

  const frontends = listDirs(path.join(repoRoot, 'frontends'));
  const mobilePlatforms = listDirs(path.join(repoRoot, 'mobile'));

  return {
    backends,
    databasesByBackend,
    frontends,
    mobilePlatforms,
    // A database only ships if every existing backend can actually provide
    // it — create only ever offers a provider common to every backend it
    // knows how to build, so a later choice of a different backend for the
    // same project stays possible without silently losing the database.
    databasesForBackend(backend) {
      return databasesByBackend[backend] ?? [];
    },
  };
}

/**
 * Backend x provider combinations this repository ships but that are
 * documented as not working, and why.
 *
 * A folder existing is not the same as a combination working: `.NET + MySQL`
 * has its provider folder, its migrations and its CI job, and it still cannot
 * build, because Pomelo — the only MySQL EF Core provider with an accepted
 * licence — targets EF Core 9 while this backend's baseline is EF Core 10.
 * The picker still offers it (the code is there to read), but it says so out
 * loud at the prompt, in the generated README, and in the generated
 * AGENTS.md, rather than letting a user discover it at `dotnet build`.
 *
 * Every entry must point at the place in this repository's documentation that
 * says the same thing; the check in scripts/check-create-picker.mjs fails if a
 * combination is blocked in code but absent from the generated project's
 * README, which is what keeps this table and the docs from drifting apart.
 * The source of truth for "blocked" is docs/SPEC.md §8.
 */
export const BLOCKED_COMBINATIONS = {
  'dotnet+mysql': {
    reason:
      'the only MySQL EF Core provider with an accepted licence (Pomelo.EntityFrameworkCore.MySql 9.0.0, MIT) ' +
      'targets EF Core 9 and cannot build its model on this backend EF Core 10 baseline. The provider folder and ' +
      'its migrations are here, so you can read them, but `dotnet build` fails until Pomelo ships an EF Core 10 ' +
      'release, and this combination is not covered by the conformance suite.',
    reference: 'docs/SPEC.md §8 and README.md ("`.NET + MySQL` does not build")',
  },
};

/** The honest label for one combination, or null when it is expected to work. */
export function blockedReason(backend, database) {
  return BLOCKED_COMBINATIONS[`${backend}+${database}`]?.reason ?? null;
}

/**
 * Whether ContextPact can honestly be offered as an optional install.
 *
 * Deliberately NOT a filesystem probe against a sibling checkout: this
 * script ships inside a public repository and must give the same answer
 * on every machine, not depend on what else happens to be cloned next to
 * it on Anik's own disk. ContextPact has no npm release yet (see its own
 * README's "Project status") — nothing outside its own source checkout
 * can actually install or run it — so there is no honest way to offer it
 * as an install from here. Flip this the day it publishes a real package.
 */
export const CONTEXTPACT_OFFER_AVAILABLE = false;
