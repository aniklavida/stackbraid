# create — the picker

Copies exactly the backend, database, frontend and mobile pieces you choose
into a new project folder and writes their configuration. **Not a code
generator** — every file you get is exactly the code this repository ships
for the pieces you picked, idiomatic and readable, nothing invented.

```bash
node create/create.mjs
```

or non-interactively:

```bash
node create/create.mjs --name my-app --backend dotnet --database postgres \
  --frontend nextjs --mobile flutter --out ../my-app
```

Ships first as this in-repo script, needing no `npm install` beyond what
each generated piece needs for itself; a published `npx stackbraid create`
wrapper around the same logic is planned for v1.0 (see
[docs/ROADMAP.md](../docs/ROADMAP.md), step 7).

## What it guarantees today

- **Only providers that actually exist ship as choices.** The picker reads
  `backends/*/src/Database` (and the Python equivalent) at run time rather
  than hard-coding a list — today that means Postgres only; SQL Server and
  MySQL are not offered because their folders don't exist yet.
- **No reference to an unchosen stack anywhere in the output.** Config
  files, `.env.example`, `.gitignore`, READMEs, lockfiles and every copied
  source comment are checked. `contract/openapi.yaml` is the one documented
  exception — it is the single hand-written contract every backend
  implements and every client is generated from, and it necessarily
  documents both backends' realtime wiring so a project can add the other
  backend later without the contract lying about it.
- **The same choices regenerate an identical tree**, byte for byte.
- `scripts/check-create-picker.mjs` proves both of the above for every
  backend x database x frontend x mobile combination that exists today, and
  is wired into `scripts/check-client-drift.sh` so it runs in CI on every
  push (see that script's own comment for why it lives there rather than in
  a new workflow file).
- **ContextPact is not offered.** It has no npm release yet — see
  `create/lib/discover.mjs`'s `CONTEXTPACT_OFFER_AVAILABLE` — so there is no
  honest way to offer it as an install from a generated project.

## What is not yet proven

- `docker compose up` has not been run for any combination — the picker's
  output has only been verified by starting the backend natively against a
  throwaway local Postgres cluster, for one combination per backend.
- The shared Playwright end-to-end suite (`e2e/`) is not copied into
  generated projects yet; a frontend's own unit and architecture tests are.
- Dependency lockfiles are copied as committed, not re-audited per project;
  a generated project inherits this repository's own licence audit as of
  the commit it was generated from.

Do not describe `create` as feature-complete until the compose run above
has actually happened for every combination.
