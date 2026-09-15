#!/usr/bin/env node
// StackBraid `create` — the picker.
//
// Copies exactly the backend, database, frontend and mobile pieces the
// user chose into a new project folder and writes their configuration.
// This is NOT a code generator: every file it produces is either copied
// unchanged from this repository, or a small, freshly-written config/doc
// file (README, AGENTS.md, .gitignore) scoped to what was actually chosen.
//
// Zero runtime dependencies — Node built-ins only, matching every other
// script in this repository (see scripts/check-dependency-licenses.mjs
// and friends): a tool that only ever reads this repository's own files
// needs no third-party package, so it carries no licence to audit.
//
// Usage (interactive):
//   node create/create.mjs
//
// Usage (non-interactive, e.g. for scripting or tests):
//   node create/create.mjs --name my-app --backend dotnet --database postgres \
//     --frontend nextjs --mobile flutter --out /path/to/target --yes

import { existsSync, mkdirSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import readline from 'node:readline/promises';

import { discoverStacks, CONTEXTPACT_OFFER_AVAILABLE } from './lib/discover.mjs';
import { copyTree, ensureEmptyDir, writeFile, copyFile } from './lib/copy.mjs';
import { buildForbiddenRegex } from './lib/sanitize.mjs';
import { renderReadme, renderAgents, renderGitignore } from './lib/templates.mjs';

const REPO_ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');

function parseArgs(argv) {
  const args = {};
  for (let i = 0; i < argv.length; i += 1) {
    const raw = argv[i];
    const match = raw.match(/^--([^=]+)(?:=(.*))?$/);
    if (!match) continue;
    const [, key, inlineValue] = match;
    if (inlineValue !== undefined) {
      args[key] = inlineValue;
    } else if (i + 1 < argv.length && !/^--/.test(argv[i + 1])) {
      args[key] = argv[i + 1];
      i += 1;
    } else {
      args[key] = true;
    }
  }
  return args;
}

async function prompt(rl, question, choices) {
  while (true) {
    const answer = (await rl.question(`${question} [${choices.join('/')}]: `)).trim().toLowerCase();
    if (choices.includes(answer)) return answer;
    console.log(`  Please choose one of: ${choices.join(', ')}`);
  }
}

async function resolveChoices(stacks, args) {
  const nonInteractive =
    args.backend !== undefined ||
    args.frontend !== undefined ||
    args.mobile !== undefined ||
    args.database !== undefined ||
    args.yes === true;

  let backend = typeof args.backend === 'string' ? args.backend.toLowerCase() : undefined;
  let database = typeof args.database === 'string' ? args.database.toLowerCase() : undefined;
  let frontend = typeof args.frontend === 'string' ? args.frontend.toLowerCase() : undefined;
  let mobile = typeof args.mobile === 'string' ? args.mobile.toLowerCase() : undefined;
  let name = typeof args.name === 'string' ? args.name : undefined;

  const rl = nonInteractive ? null : readline.createInterface({ input: process.stdin, output: process.stdout });

  try {
    if (!name) {
      name = rl ? (await rl.question('Project name: ')).trim() : 'my-app';
    }
    if (!name) name = 'my-app';

    if (!backend) {
      if (!rl) throw new Error('--backend is required in non-interactive mode');
      backend = await prompt(rl, `Backend`, stacks.backends);
    }
    if (!stacks.backends.includes(backend)) {
      throw new Error(
        `Unknown backend "${backend}". Available today: ${stacks.backends.join(', ') || '(none)'}`,
      );
    }

    const availableDatabases = stacks.databasesForBackend(backend);
    if (!database) {
      database = availableDatabases.length === 1 && !rl ? availableDatabases[0] : undefined;
      if (!database && rl) database = await prompt(rl, 'Database', availableDatabases);
      if (!database) database = availableDatabases[0];
    }
    if (!availableDatabases.includes(database)) {
      throw new Error(
        `"${database}" is not a database provider that exists for ${backend} today. ` +
          `Available: ${availableDatabases.join(', ') || '(none)'}`,
      );
    }

    const frontendChoices = [...stacks.frontends, 'none'];
    if (!frontend) {
      frontend = rl ? await prompt(rl, 'Frontend', frontendChoices) : 'none';
    }
    if (!frontendChoices.includes(frontend)) {
      throw new Error(`Unknown frontend "${frontend}". Available: ${frontendChoices.join(', ')}`);
    }

    const mobileChoices = [...stacks.mobilePlatforms.map(() => 'flutter').filter((v, i, a) => a.indexOf(v) === i), 'none'];
    if (!mobile) {
      mobile = rl ? await prompt(rl, 'Mobile', mobileChoices) : 'none';
    }
    if (!mobileChoices.includes(mobile)) {
      throw new Error(`Unknown mobile choice "${mobile}". Available: ${mobileChoices.join(', ')}`);
    }

    if (CONTEXTPACT_OFFER_AVAILABLE && rl) {
      // Intentionally unreachable until ContextPact ships a real, installable
      // release — see create/lib/discover.mjs. When it does, prompt here.
    }

    return { name, backend, database, frontend, mobile };
  } finally {
    rl?.close();
  }
}

// Any README.md, at any depth, is skipped from every copied tree — each
// carries prose written for the full multi-stack repository (cross-links
// to sibling stacks throughout) rather than for a single generated
// project. Fresh, stack-scoped documentation is written instead; see
// create/lib/templates.mjs.
function isReadme(rel) {
  return rel === 'README.md' || rel.endsWith('/README.md');
}

function planCopies(stacks, choices) {
  const chosenTokens = [choices.backend, choices.database];
  if (choices.frontend !== 'none') chosenTokens.push(choices.frontend);
  if (choices.mobile !== 'none') chosenTokens.push(choices.mobile);
  const forbidden = buildForbiddenRegex(chosenTokens);

  return { forbidden };
}

export function generate(repoRoot, choices, targetDir) {
  const stacks = discoverStacks(repoRoot);
  if (!stacks.backends.includes(choices.backend)) {
    throw new Error(`Unknown backend "${choices.backend}"`);
  }
  const { forbidden } = planCopies(stacks, choices);
  const stats = { files: 0, redactedFiles: 0, redactions: 0 };

  ensureEmptyDir(targetDir);

  // --- Backend -------------------------------------------------------
  copyTree(path.join(repoRoot, 'backends', choices.backend), path.join(targetDir, 'backends', choices.backend), {
    skip: isReadme,
    forbidden,
    stats,
  });

  // --- Frontend --------------------------------------------------------
  if (choices.frontend !== 'none') {
    copyTree(
      path.join(repoRoot, 'frontends', choices.frontend),
      path.join(targetDir, 'frontends', choices.frontend),
      {
        skip: isReadme,
        forbidden,
        stats,
      },
    );
    copyTree(path.join(repoRoot, 'clients/typescript'), path.join(targetDir, 'clients/typescript'), {
      skip: isReadme,
      forbidden,
      stats,
    });
    // The shared end-to-end identity spec both frontends' own
    // `e2e/identity-flow.spec.ts` import via a relative path
    // (`../../../e2e/identity-flow`) — copying it at the same relative
    // depth keeps that import working unchanged in the generated project.
    copyTree(path.join(repoRoot, 'e2e'), path.join(targetDir, 'e2e'), {
      skip: isReadme,
      forbidden,
      stats,
    });
  }

  // --- Mobile ----------------------------------------------------------
  if (choices.mobile === 'flutter') {
    copyTree(path.join(repoRoot, 'mobile/flutter'), path.join(targetDir, 'mobile/flutter'), {
      skip: isReadme,
      forbidden,
      stats,
    });
    copyTree(path.join(repoRoot, 'clients/dart'), path.join(targetDir, 'clients/dart'), {
      skip: isReadme,
      forbidden,
      stats,
    });
  }

  // --- Contract (always) -------------------------------------------------
  copyFile(path.join(repoRoot, 'contract/openapi.yaml'), targetDir, 'contract/openapi.yaml');
  copyTree(path.join(repoRoot, 'contract/conformance'), path.join(targetDir, 'contract/conformance'), {
    skip: isReadme,
    forbidden,
    stats,
  });
  // The vendored Swagger UI copy both backends serve at /docs/assets/.
  //
  // Deliberately copied WITHOUT `forbidden`: these files are a byte-identical
  // copy of a published package whose SHA-256s are recorded in PROVENANCE.json
  // and re-checked by the licence gate. Running them through the comment
  // sanitizer was measured to modify both of them — the minified bundle carries
  // syntax-highlighting definitions naming half the stacks in this repository —
  // which would leave every generated project failing its own integrity check
  // on the first CI run. The sanitizer exists to strip *our* cross-stack
  // comments; those cannot appear in someone else's published package.
  copyTree(path.join(repoRoot, 'contract/docs-assets'), path.join(targetDir, 'contract/docs-assets'), {
    stats,
  });
  writeFile(
    targetDir,
    'contract/README.md',
    '# Contract\n\n`openapi.yaml` is hand-written and authoritative — the backend implements it, ' +
      'and any generated client in `clients/` is produced from it, never hand-edited.\n\n' +
      'Validate it before committing a change:\n\n```bash\nnpx @redocly/cli@2.52.1 lint contract/openapi.yaml\n```\n',
  );
  writeFile(
    targetDir,
    'contract/conformance/README.md',
    '# Conformance suite\n\nA zero-dependency Node.js suite that validates a running backend against ' +
      '`contract/openapi.yaml` — full shape and type checks, not just status codes.\n\n' +
      '```bash\ncd contract/conformance\nnpm install\nnpm start -- http://127.0.0.1:8080\n```\n',
  );

  // --- Infra (always; generic, but sanitized just in case) ----------------
  copyTree(path.join(repoRoot, 'infra'), path.join(targetDir, 'infra'), { forbidden, stats });

  // --- Generic top-level files ---------------------------------------------
  for (const file of ['LICENSE', 'THIRD_PARTY_NOTICES.md', 'CODE_OF_CONDUCT.md', 'CONTRIBUTING.md', 'SECURITY.md', 'CLAUDE.md', 'GEMINI.md', '.env.example']) {
    const src = path.join(repoRoot, file);
    if (existsSync(src)) copyFile(src, targetDir, file);
  }

  // --- Freshly generated, stack-scoped config and docs ---------------------
  writeFile(targetDir, 'README.md', renderReadme(choices));
  writeFile(targetDir, 'AGENTS.md', renderAgents(choices));
  writeFile(targetDir, '.gitignore', renderGitignore(choices));

  return stats;
}

async function main() {
  const args = parseArgs(process.argv.slice(2));
  const stacks = discoverStacks(REPO_ROOT);
  const choices = await resolveChoices(stacks, args);

  const targetDir = args.out
    ? path.resolve(String(args.out))
    : path.resolve(process.cwd(), choices.name);

  console.log(`\nGenerating "${choices.name}" into ${targetDir}`);
  console.log(`  backend:  ${choices.backend}`);
  console.log(`  database: ${choices.database}`);
  console.log(`  frontend: ${choices.frontend}`);
  console.log(`  mobile:   ${choices.mobile}\n`);

  const stats = generate(REPO_ROOT, choices, targetDir);

  console.log(`Copied ${stats.files} file(s). Redacted ${stats.redactions} cross-stack comment(s) in ${stats.redactedFiles} file(s).`);
  console.log(`\nDone. See ${path.join(targetDir, 'README.md')} for how to run it.`);
}

const isMain = process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  main().catch((err) => {
    console.error(`create: ${err.message}`);
    process.exitCode = 1;
  });
}
