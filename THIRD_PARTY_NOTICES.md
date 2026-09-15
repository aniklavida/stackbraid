# Third-party notices

StackBraid is MIT licensed (see [`LICENSE`](LICENSE)). It also **redistributes** the third-party
software listed here: files copied into this repository and shipped as part of it, rather than
fetched by a user's own package manager from the original publisher.

This file exists because of that distinction. A package declared in `package.json` or a Docker
image referenced by tag arrives from its publisher carrying its own licence, and creates no
obligation here. A file copied into the tree travels with every clone, release and deployment, and
its licence travels with it. `docs/DEPENDENCIES.md` records the determination in full.

---

## Swagger UI (`swagger-ui-dist`)

**Version 5.32.15 · Apache License 2.0 · Copyright 2020-2021 SmartBear Software Inc.**

Location in this repository: [`contract/docs-assets/swagger-ui/`](contract/docs-assets/swagger-ui/)

Files: `swagger-ui.css`, `swagger-ui-bundle.js` — copied byte-identical from the published npm
package and served by both backends at `/docs/assets/`, so the API documentation renders without
contacting any third party. See that directory's `README.md` for why it is a copy rather than a
CDN reference, and `PROVENANCE.json` for the integrity record.

Licence and notice texts, redistributed alongside the code as Apache-2.0 §4 requires:

- [`contract/docs-assets/swagger-ui/LICENSE`](contract/docs-assets/swagger-ui/LICENSE) — the full
  Apache License 2.0 text.
- [`contract/docs-assets/swagger-ui/NOTICE`](contract/docs-assets/swagger-ui/NOTICE) — the
  attribution notice.
- [`contract/docs-assets/swagger-ui/swagger-ui-bundle.js.LICENSE.txt`](contract/docs-assets/swagger-ui/swagger-ui-bundle.js.LICENSE.txt)
  — notices for the third-party code bundled inside `swagger-ui-bundle.js` by its own build, carried
  forward unchanged.

Upstream: <https://github.com/swagger-api/swagger-ui>

---

## Nothing else

Every other dependency is declared in a manifest (`package.json`, `pubspec.yaml`, `*.csproj`,
`requirements*.txt`) or referenced as a container image tag, and is fetched by the user's own
tooling from its original registry. The generated API clients under `clients/` are new code written
by a code generator from `contract/openapi.yaml`, not copies of the generator's source. None of
these is redistribution, and none adds an entry here.

**Adding one does.** If a file is ever copied into this tree from a third-party project, it is
recorded in this file, its licence and notice files are copied with it, and
`docs/dependency-inventory.json` gains an entry in the same commit.
