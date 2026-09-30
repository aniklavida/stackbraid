// Freshly-written project documentation and config, generated from the
// user's choices rather than filtered from StackBraid's own multi-stack
// docs. StackBraid's own README/AGENTS.md are written for a repository
// that ships every stack side by side and cross-reference every sibling
// throughout — regenerating them here, scoped to only what this project
// actually contains, is far more reliable than trying to surgically
// remove every cross-reference from prose written for a different
// audience. See create/README.md for the reasoning.

import { blockedReason } from './discover.mjs';

const BACKEND_LABEL = { dotnet: '.NET', python: 'Python' };
const FRONTEND_LABEL = { angular: 'Angular', nextjs: 'Next.js', none: null };
const DATABASE_LABEL = { postgres: 'PostgreSQL', sqlserver: 'SQL Server', mysql: 'MySQL' };

// How each provider is pointed at a real database, and — honestly — which of
// them can be started on this machine without Docker. Only PostgreSQL ships a
// throwaway-cluster script; the other two need an instance the user already
// has, and the generated README says so rather than printing a Postgres
// command that cannot work.
const DATABASE_SETUP = {
  dotnet: {
    postgres: [
      '```bash',
      'cd backends/dotnet',
      './scripts/start-local-postgres.sh   # or point ConnectionStrings:Postgres at your own Postgres',
      'dotnet run --project src/Host --urls http://127.0.0.1:8080',
      '```',
    ].join('\n'),
    sqlserver: [
      '```bash',
      'cd backends/dotnet',
      '# Set Database:Provider to "sqlserver" and ConnectionStrings:SqlServer in',
      '# src/Host/appsettings.json (or the environment) to your own SQL Server',
      '# instance. No throwaway local script ships for this provider.',
      'dotnet run --project src/Host --urls http://127.0.0.1:8080',
      '```',
    ].join('\n'),
    mysql: [
      '```bash',
      'cd backends/dotnet',
      '# Set Database:Provider to "mysql" and ConnectionStrings:MySql in',
      '# src/Host/appsettings.json (or the environment) to your own MySQL',
      '# instance. No throwaway local script ships for this provider.',
      'dotnet run --project src/Host --urls http://127.0.0.1:8080',
      '```',
    ].join('\n'),
  },
  python: {
    postgres: [
      '```bash',
      'cd backends/python',
      './scripts/start-local-postgres.sh   # or point STACKBRAID_POSTGRES_DSN at your own Postgres',
      'python -m venv .venv && source .venv/bin/activate',
      'pip install -r requirements-lock.txt',
      'PYTHONPATH=src uvicorn app.host.main:app --port 8080',
      '```',
    ].join('\n'),
    sqlserver: [
      '```bash',
      'cd backends/python',
      '# Point STACKBRAID_DATABASE_PROVIDER=sqlserver and STACKBRAID_SQLSERVER_DSN at',
      '# your own SQL Server instance. No throwaway local script ships for',
      '# this provider.',
      'python -m venv .venv && source .venv/bin/activate',
      'pip install -r requirements-lock.txt',
      'PYTHONPATH=src uvicorn app.host.main:app --port 8080',
      '```',
    ].join('\n'),
    mysql: [
      '```bash',
      'cd backends/python',
      '# Point STACKBRAID_DATABASE_PROVIDER=mysql and STACKBRAID_MYSQL_DSN at your',
      '# own MySQL instance. No throwaway local script ships for this provider.',
      'python -m venv .venv && source .venv/bin/activate',
      'pip install -r requirements-lock.txt',
      'PYTHONPATH=src uvicorn app.host.main:app --port 8080',
      '```',
    ].join('\n'),
  },
};

function backendRunInstructions(choices) {
  const byBackend = DATABASE_SETUP[choices.backend] ?? {};
  return byBackend[choices.database] ?? byBackend.postgres;
}

function frontendRunInstructions(choices) {
  if (choices.frontend === 'none') return null;
  const dir = choices.frontend === 'angular' ? 'frontends/angular' : 'frontends/nextjs';
  const startCmd = choices.frontend === 'angular' ? 'npm start' : 'npm run dev';
  return [
    '```bash',
    `cd ${dir}`,
    'npm install',
    startCmd,
    '```',
  ].join('\n');
}

function mobileRunInstructions(choices) {
  if (choices.mobile === 'none') return null;
  return [
    '```bash',
    'cd mobile/flutter',
    'flutter pub get',
    'flutter run --dart-define=API_BASE_URL=http://127.0.0.1:8080',
    '```',
  ].join('\n');
}

export function renderReadme(choices, playbooks = []) {
  const backendLabel = BACKEND_LABEL[choices.backend];
  const databaseLabel = DATABASE_LABEL[choices.database] ?? choices.database;
  const frontendLabel = FRONTEND_LABEL[choices.frontend];
  const mobileIncluded = choices.mobile !== 'none';
  const blocked = blockedReason(choices.backend, choices.database);

  const includedRows = [
    `| Backend | ${backendLabel} |`,
    `| Database | ${databaseLabel}${blocked ? ' — **not working yet**, see below' : ''} |`,
    `| Frontend | ${frontendLabel ?? 'none'} |`,
    `| Mobile | ${mobileIncluded ? 'Flutter' : 'none'} |`,
  ];

  const sections = [];
  sections.push(`# ${choices.name}\n`);
  sections.push(
    'Generated by the [StackBraid](https://github.com/aniklavida/stackbraid) `create` picker. ' +
      'This is not a code generator — every file below is exactly the code StackBraid ships for ' +
      'the pieces you chose; nothing here was invented for this project.\n',
  );
  sections.push('## What this project includes\n');
  sections.push('| Piece | Choice |\n|---|---|\n' + includedRows.join('\n') + '\n');

  if (blocked) {
    sections.push('## Read this first: this database combination does not build yet\n');
    sections.push(
      `**${backendLabel} + ${databaseLabel} was generated, and it is known not to work.** ${blocked}\n\n` +
        'The provider code is here so you can read it and follow it, but do not expect `dotnet build` ' +
        'to succeed, and do not treat this project as proven. Choose a different database provider if ' +
        'you need a backend that runs today.\n',
    );
  }

  sections.push('## Running the backend\n');
  sections.push(backendRunInstructions(choices) + '\n');

  const frontendInstr = frontendRunInstructions(choices);
  if (frontendInstr) {
    sections.push(`## Running the ${frontendLabel} frontend\n`);
    sections.push(frontendInstr + '\n');
  }

  const mobileInstr = mobileRunInstructions(choices);
  if (mobileInstr) {
    sections.push('## Running the Flutter mobile app\n');
    sections.push(mobileInstr + '\n');
  }

  sections.push('## The contract\n');
  sections.push(
    '`contract/openapi.yaml` is the single source of truth the backend implements. ' +
      (frontendInstr || mobileInstr
        ? 'The generated client(s) under `clients/` are built from it and are never hand-edited.\n'
        : 'No client is included because no frontend or mobile app was chosen.\n'),
  );

  sections.push('## Proving it works\n');
  sections.push(
    [
      'Run the shared conformance suite against your running backend:',
      '',
      '```bash',
      'cd contract/conformance',
      'npm install',
      'npm start -- http://127.0.0.1:8080',
      '```',
    ].join('\n') + '\n',
  );

  if (playbooks.length > 0) {
    sections.push('## Working with a coding agent\n');
    sections.push(
      'Read `AGENTS.md` first. The procedures that apply to this project live in ' +
        '`.agent/playbooks/`, filtered to the stacks chosen above — a playbook for a stack this project ' +
        'does not contain is not installed, so nothing there can send an agent after code that is not here: ' +
        `${playbooks.map((p) => '`' + p.name.replace(/\.md$/, '') + '`').join(' · ')}.\n`,
    );
  }

  sections.push('## Licence\n');
  sections.push('MIT. See [LICENSE](LICENSE).\n');

  return sections.join('\n');
}

export function renderAgents(choices, playbooks = []) {
  const backendLabel = BACKEND_LABEL[choices.backend];
  const frontendLabel = FRONTEND_LABEL[choices.frontend];
  const mobileIncluded = choices.mobile !== 'none';
  const blocked = blockedReason(choices.backend, choices.database);

  const pieces = [backendLabel, frontendLabel, mobileIncluded ? 'Flutter' : null].filter(Boolean);

  const clientLines = [];
  if (choices.frontend !== 'none') clientLines.push('- Regenerate `clients/typescript` (TypeScript).');
  if (mobileIncluded) clientLines.push('- Regenerate `clients/dart` (Dart).');

  // The integration tier runs against the provider this project actually
  // chose, not a hard-coded Postgres: a SQL Server project is not covered by
  // a table that says "real Postgres".
  const providerLabel = DATABASE_LABEL[choices.database] ?? choices.database;
  const integrationTier = blocked
    ? `real ${providerLabel}, once the limitation above is resolved`
    : `real ${providerLabel}`;
  const testLines = [];
  if (choices.backend === 'dotnet') {
    testLines.push(
      `| Backend (.NET) | xUnit + Shouldly + NSubstitute | Testcontainers-free, ${integrationTier} | NetArchTest |`,
    );
  } else {
    testLines.push(`| Backend (Python) | pytest + pytest-asyncio | ${integrationTier} | import-linter |`);
  }
  if (choices.frontend === 'angular') {
    testLines.push('| Frontend (Angular) | Vitest | Playwright | dependency-cruiser |');
  } else if (choices.frontend === 'nextjs') {
    testLines.push('| Frontend (Next.js) | Vitest + Testing Library | Playwright | dependency-cruiser |');
  }
  if (mobileIncluded) {
    testLines.push('| Mobile (Flutter) | flutter_test | integration_test | hand-written boundary test |');
  }
  testLines.push('| Contract | — | — | conformance suite (`contract/conformance`) |');

  return `# ${choices.name} — contributor and agent instructions

The canonical guide for humans and coding agents working in this repository.
Tool-neutral: Claude, Codex, Cursor, Gemini CLI and others can read this file.

## What this repository is

A product skeleton generated from [StackBraid](https://github.com/aniklavida/stackbraid),
containing exactly the pieces chosen at generation time: **${pieces.join(', ')}**, on
**${DATABASE_LABEL[choices.database] ?? choices.database}**. Nothing else is present — do not write instructions or
code assuming a stack this project does not include.${
    blocked
      ? `\n\n**Known limitation — this project does not build today.** ${blocked} Label it *unsupported* in anything you write about it, and do not spend time debugging it as if it were a mistake in this repository's code.`
      : ''
  }

## The one rule that matters

**The contract comes first.**

\`\`\`
contract/openapi.yaml   →   the backend implements it
                        →   any included client is generated from it
                        →   the conformance suite proves it
\`\`\`

Never change an API by editing the backend directly. Change \`contract/openapi.yaml\` first${
    clientLines.length ? ', then regenerate the client(s), then implement:' : ', then implement:'
  }
${clientLines.length ? clientLines.join('\n') + '\n' : ''}
## Adding anything

1. Edit \`contract/openapi.yaml\`.
${clientLines.length ? '2. Regenerate every included generated client.\n' : ''}${
    clientLines.length ? '3' : '2'
  }. Implement in the backend.
${clientLines.length ? '4' : '3'}. Implement in the frontend or mobile app if the feature has a surface.
${clientLines.length ? '5' : '4'}. Run the conformance suite against the running backend.
${clientLines.length ? '6' : '5'}. Add or update localized strings for every locale this project ships.

## Structure

\`\`\`
src/
├── Shared/       cross-cutting plumbing — no business meaning
├── Database/     provider-specific; the only place a provider name appears
├── Features/     one folder per domain area
└── Host/         composition root
\`\`\`

Every feature carries four layers, in every piece of this project:

| Layer | Backend | Frontend / mobile |
|---|---|---|
| Models and rules | \`Domain/\` | \`domain/\` |
| Use cases | \`Application/\` | \`application/\` |
| Data access | \`Persistence/\` | \`data/\` |
| Delivery | \`Endpoints/\` | \`presentation/\` |

Two rules, both enforced by tests:

- **A feature never reaches into another feature's internals** — only its \`Contracts\`.
- **\`Shared\` never imports a feature.** The moment it does, it stops being shared.

## Naming

Feature names are identical across every piece this project includes. Adding
\`invoices\` means a folder called \`invoices\` everywhere — not \`Invoice\`, not
\`billing\`. Casing follows each language's convention; the word does not change.

## Dependencies

Every dependency shipped here is inherited by whoever uses this project.
Audit the licence before adding one: code compiled into what you ship needs
an MIT, Apache or BSD licence; something that runs as its own process
(a database, a broker) may be copyleft, since it never links against your code.

## Truthfulness

Every public claim about this project should be exactly one of: **implemented
and tested**, **experimental**, **planned**, or **unsupported**. Don't describe
a planned capability as already working.

## Generated code

${
  clientLines.length
    ? '`clients/` and any `api/` folder are generated and committed. **Never hand-edit them.** Regenerate from `contract/openapi.yaml` instead.'
    : 'No client is generated in this project because no frontend or mobile app was chosen — the backend is consumed directly over HTTP.'
}

## Playbooks

${
  playbooks.length
    ? [
        'Procedures live in `.agent/playbooks/`, one file each, already filtered to the stacks this ' +
          'project contains — a playbook for a stack that is not here is not installed, so nothing in ' +
          'there can send an agent after code that does not exist:',
        '',
        playbooks
          .map((p) => `- [\`${p.name.replace(/\.md$/, '')}\`](.agent/playbooks/${p.name})`)
          .join('\n'),
      ].join('\n')
    : 'No playbook applies to the stacks in this project, so `.agent/playbooks/` is empty. Write down the procedure you follow as you add the first one.'
}

## Tests

| Piece | Unit | Integration / E2E | Architecture |
|---|---|---|---|
${testLines.join('\n')}
`;
}

export function renderGitignore(choices) {
  const lines = [
    '# Secrets',
    '.env',
    '.env.*',
    '!.env.example',
    '*.pem',
    '*.key',
    '',
  ];

  if (choices.backend === 'dotnet') {
    lines.push(
      '# .NET',
      'bin/',
      'obj/',
      '*.user',
      'TestResults/',
      'backends/dotnet/.local-postgres-test-state',
      '',
    );
  }
  if (choices.backend === 'python') {
    lines.push(
      '# Python',
      '__pycache__/',
      '*.py[cod]',
      '.venv/',
      'venv/',
      '.pytest_cache/',
      '.mypy_cache/',
      '.ruff_cache/',
      '*.egg-info/',
      'backends/python/.local-postgres-test-state',
      '',
    );
  }
  if (choices.frontend !== 'none' || choices.mobile === 'flutter') {
    lines.push(
      '# Node',
      'node_modules/',
      'dist/',
      'coverage/',
      '*.tsbuildinfo',
      '',
    );
  }
  if (choices.frontend === 'nextjs') {
    lines.push('.next/', '');
  }
  if (choices.frontend === 'angular') {
    lines.push('.angular/', '');
  }
  if (choices.mobile === 'flutter') {
    lines.push(
      '# Flutter / Dart',
      '.dart_tool/',
      '.packages',
      'build/',
      '*.iml',
      '',
    );
  }

  lines.push('# Editors and OS', '.idea/', '.vs/', '.DS_Store', 'Thumbs.db', '');

  return lines.join('\n');
}
