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
  than hard-coding a list — today that means PostgreSQL, SQL Server and MySQL
  on each backend. Add a fourth provider folder and it is offered, with no
  change here.
- **A combination documented as not working is never offered silently.**
  `.NET + MySQL` is offered — the provider folder, its migrations and its CI
  job are all in the repository to read — but it is labelled *blocked* at the
  prompt, and the generated README and AGENTS.md both say plainly that the
  project does not build, before anything else. The reason and its source are
  recorded in `create/lib/discover.mjs`'s `BLOCKED_COMBINATIONS`; the check
  below fails if a blocked combination is generated without that notice.
- **No reference to an unchosen stack anywhere in the output.** Config
  files, `.env.example`, `.gitignore`, READMEs, playbooks, lockfiles and every
  copied source comment are checked. `contract/openapi.yaml` is the one
  documented exception — it is the single hand-written contract every backend
  implements and every client is generated from, and it necessarily
  documents both backends' realtime wiring so a project can add the other
  backend later without the contract lying about it.
- **The agent playbooks are filtered to the chosen stacks.** A playbook is
  installed only when the project contains at least one stack it is written
  for (`PLAYBOOK_STACKS` in `create/lib/playbooks.mjs`), and then its sections
  are filtered: a block naming an absent stack is dropped whole rather than
  edited, so a single-stack project gets a short, complete `.NET` procedure
  instead of half of a two-backend one. A backend-only project gets the nine
  backend playbooks and none of the five UI ones. The generated AGENTS.md
  links exactly the playbooks that were installed.
- **The same choices regenerate an identical tree**, byte for byte.
- `scripts/check-create-picker.mjs` proves all of the above for every
  backend x database x frontend x mobile combination that exists today — 36 of
  them — and is wired into `scripts/check-client-drift.sh` so it runs in CI on
  every push (see that script's own comment for why it lives there rather than
  in a new workflow file).
- **ContextPact is not offered.** It has no npm release yet — see
  `create/lib/discover.mjs`'s `CONTEXTPACT_OFFER_AVAILABLE` — so there is no
  honest way to offer it as an install from a generated project.

## What is not yet proven

- `docker compose up` has not been run for any combination — the picker's
  output has only been verified by starting the backend natively against a
  throwaway local Postgres cluster, for one combination per backend.
- No combination other than those two has had its backend started at all.
  SQL Server and MySQL need an instance the user already has: only
  PostgreSQL ships a no-Docker local script, and the generated README says so
  for the other two rather than printing a command that cannot work.
- One Angular and one Next.js combination have had the frontend's own unit,
  lint and architecture suites run from inside the generated project. That is
  a sample of the 36, not all of them.
- Dependency lockfiles are copied as committed, not re-audited per project;
  a generated project inherits this repository's own licence audit as of
  the commit it was generated from.
- A playbook whose sections span stacks loses the blocks that covered the
  stacks this project does not have, so a kept step can refer to a step that
  is no longer in the file. The consequence is stated in the file itself: an
  agent that meets one is missing context it should not have had, and nothing
  left in the file points it at code that does not exist.
- A generated project does **not** include this repository's own `scripts/`
  tooling, so a playbook step that names `scripts/check-client-drift.sh`,
  `scripts/generate-clients.sh` or the licence gate points at a file that is
  not in the project. The step is still the right instruction; the command has
  to be adapted. Copying the scripts that apply is not done.

Do not describe `create` as feature-complete until the compose run above
has actually happened for every combination.
