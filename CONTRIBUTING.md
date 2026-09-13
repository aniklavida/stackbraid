# Contributing

StackBraid is pre-implementation. The specification, architecture and structure exist; working code does not yet.

**Implementation contributions are not being accepted until the foundation is complete.** Issues and discussion about the specification are welcome now.

## When contributions open

1. Read [`AGENTS.md`](AGENTS.md) first — it is the contract for humans and agents alike.
2. The API contract comes first. Never change an API by editing a backend.
3. A change that cannot be made end-to-end in every shipped stack will not be merged.
4. Every new dependency needs a licence audit in the pull request. See the dependency rules in `AGENTS.md`.
5. Run the conformance suite against every backend before opening a pull request.
6. Generated clients are never hand-edited. Regenerate them.

## Generated clients

`clients/typescript` and `clients/dart` are generated from `contract/openapi.yaml` —
see `clients/typescript/README.md` and `clients/dart/README.md`. Regenerate
both with `./scripts/generate-clients.sh`; never hand-edit the output.

Enable the drift-checking pre-commit hook once per clone:

```bash
git config core.hooksPath .githooks
```

It regenerates both clients and fails the commit if `clients/` doesn't match
a fresh generation — see `scripts/check-client-drift.sh`.

The same check also runs in CI (`.github/workflows/ci.yml`, job
`client-drift`) on every push and pull request, so drift is caught even from
a clone where the hook was never enabled, or a commit made with
`--no-verify`.

## Commit messages

Describe what changed and why. Reference the contract change if there was one.
