# v1.0 release checklist

Status as of this pass. **Ticked means verified in this pass, with the evidence
named.** Unticked means not verified, or verified as failing. A box is never
ticked on the strength of a claim in a document — only on a check that ran.

## Truth

- [x] Every public claim has working evidence or is clearly labelled planned.
      Audited `README.md`, `docs/SPEC.md`, `docs/ARCHITECTURE.md`,
      `CONTRIBUTING.md` and `docs/ROADMAP.md` against the source tree rather
      than against other documents. `docs/SPEC.md` §6 now carries a per-capability
      status table (implemented / partial / planned) with the specific gap named
      for each partial; the blanket "everything is planned" and
      "nothing in this document is implemented yet" claims are gone; the
      `.NET + MySQL` block and the never-run `docker compose up` are stated in
      the README's opening note rather than left to be discovered.
- [x] The specification, the documentation and the implementation agree.
      `docs/SPEC.md` §8 no longer marks `.NET + MySQL` as passing; §15 no longer
      names Testcontainers, which neither backend uses; the `docs/SPEC.md` §11
      dependency table and `AGENTS.md` were cross-checked against the actual
      `package.json`, `pyproject.toml`, `*.csproj` and `pubspec.yaml` files.

**Closed truth item, #18:** `AGENTS.md` rejects "a revenue-gated commercial
licence" outright, yet `QuestPDF` Community — free under USD 1M annual revenue,
and excluded for public companies and governments — was shipped compiled into
the .NET backend, with the contradiction defended at length in
`docs/DEPENDENCIES.md` rather than resolved. It is resolved: QuestPDF was
removed and `IPdfGenerator` resolves to the dependency-free
`MinimalPdfGenerator`. The licence gate now enforces the policy itself — a
`compiled-into-user-code` entry outside MIT/Apache-2.0/BSD fails CI whatever
the inventory records — so the contradiction cannot be reintroduced by an entry
marked accepted.

## Product

- [ ] `create` produces a working project for every combination, containing only the chosen pieces.
- [ ] `docker compose up` brings up the full system with no manual steps.
- [ ] Register, log in and manage users and roles work in web, admin and mobile against both backends.
- [ ] A queued export produces a file and its duration appears on a dashboard.
- [ ] A realtime notification reaches every surface from both backends.
- [ ] Every string is localized in at least two locales, backend errors included.

## Engineering

- [ ] Conformance passes for every backend on every shipped provider, in CI.
- [ ] Generated clients are byte-identical to a fresh regeneration.
- [ ] Architecture tests pass in both backends.
- [ ] All five test suites run green from a clean checkout.
- [ ] Every route and generated client carries `/v1/`.

## Repository

- [x] Every shipped dependency is listed with its licence and passes the audit.
      `node scripts/check-dependency-licenses.mjs` → `Checked 2137 audited
      entries and what is actually resolved. OK — every resolved dependency
      is in the audited inventory, at the audited version, with an accepted
      licence.` The inventory covers 2,137 entries across 10 ecosystems
      (npm ×3, Dart ×2, NuGet, PyPI, Docker images, CI tooling, vendored
      assets). Spot-checked the direct dependencies actually declared in
      `backends/dotnet/src/**.csproj` and `backends/python/pyproject.toml`
      against the inventory — all present, none stale. **The one policy
      conflict, QuestPDF's revenue-gated Community licence, is now resolved —
      see the Truth section and #18.**
- [x] README, specification, architecture, structure and troubleshooting are complete.
      README, `docs/SPEC.md`, `docs/ARCHITECTURE.md` and `docs/STRUCTURE.md`
      all existed. **Troubleshooting did not exist at all** and is named in
      this checklist while `grep -ri troubleshoot` matched nothing but the
      checklist line itself — added as `docs/TROUBLESHOOTING.md`, 14
      documented failures, every environment variable in it read out of the
      source rather than written from memory.
- [x] Security policy, code of conduct and contributor instructions are complete.
      `SECURITY.md` and `CODE_OF_CONDUCT.md` were complete and accurate.
      `CONTRIBUTING.md` was the outlier: it opened with "StackBraid is
      pre-implementation… working code does not yet" and "Implementation
      contributions are not being accepted" — both false, and actively
      discouraging. Corrected, and given the missing how-to-run and
      before-you-open-a-PR sections.
- [ ] Repository description, topics and homepage are set. — **Partly done,
      one item blocked.** `gh repo view` confirms description and 10 topics are
      set and correct. **Homepage is genuinely empty** and was not fixed:
      `gh repo edit` is denied by a local permission rule in this environment,
      and routing around a permission rule is not a decision this pass should
      make. One command remains for the maintainer:
      `gh repo edit aniklavida/stackbraid --homepage https://github.com/aniklavida/stackbraid#readme`
- [ ] CI and release workflows pass. — **Was red; two real defects fixed, one
      remains, reported rather than fixed.** See below.
- [ ] Working tree is clean and local HEAD matches the remote. — Checked at the
      end of this pass; see the commit for the recorded result.

### CI: what was actually broken

`gh run list` showed the last four `develop` runs **failing**, and the newest
one (run `36120869921`, 2026-09-25) failing in four jobs. The failures were
**not** documentation problems and were **not** a flaky database:

| Job | Real cause | Status |
|---|---|---|
| `dotnet-conformance`, `python-conformance`, `conformance-self-check` | `contract/conformance/src/checks/users.mjs` had a **syntax error** — the exported `registerUserChecks` function was never closed. `git show 1d4f0ea` shows commit `1d4f0ea` deleted the file's final `}` when it added the soft-delete and audit checks. Every job that loads the suite died with `SyntaxError: Unexpected end of input` at load time, before any HTTP call. | **Fixed** — restored the closing brace |
| `flutter` | `mobile/flutter/test/features/auth/application/use_cases_test.dart` builds a `User` without the `deletedAt` argument that `1d4f0ea` added as a **required** named parameter to the generated Dart model. `flutter analyze` → `missing_required_argument`. | **Fixed** — added `deletedAt: null` |
| `conformance-self-check` (baseline scenario) | **Still failing, deliberately not fixed here.** With the syntax error gone the suite runs, and the baseline scenario reports 9 failures: the stub server never learned `deletedAt`, and has no `restore`, `includeDeleted` or `/v1/audit` handling. The stub drifted from the checks that `1d4f0ea` extended. | **Open — see below** |

Verified locally, no container engine: `node --check` clean on the repaired
file; `flutter analyze` → `No issues found!`; `npm run demo:violations` → the
suite now loads and executes.

**The remaining conformance baseline failure is out of scope for this pass and
is reported, not fixed.** Closing it means teaching the stub server
`deletedAt`, `POST /v1/users/{id}/restore`, `?includeDeleted=` and `GET
/v1/audit` — which is editing a test fixture and a stub server, and the
instructions for this pass explicitly exclude both. It is also the exact work a
prior attempt on this card drifted into. It needs the two
`contract/conformance` jobs green before the box above can honestly be ticked.

Two related findings noted **and deliberately not acted on**, because both are
real bugs outside this pass's scope:

- The .NET backend exposes `POST /v1/documents/export/excel` and
  `.../pdf`, and `POST /v1/jobs/*` — none of which appear in
  `contract/openapi.yaml`, and the `/v1/documents/*` routes carry no
  `RequireAuthorization`. By the repo's own contract-first rule these are
  violations, and an unauthenticated one.
- `AuthEndpoints.cs` calls `.RequireRateLimiting("auth")` three times, but
  there is no `AddRateLimiter`/`UseRateLimiter` anywhere in the backend, so
  those three calls are inert. Only the custom middleware rate-limits.

## Launch

- [ ] A 90-second demo proves pick, run, and add a feature.
- [ ] Release notes and changelog are accurate.
- [ ] The tag is created only after every box above is ticked.
