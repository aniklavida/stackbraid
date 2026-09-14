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
