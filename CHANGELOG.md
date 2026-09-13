# Changelog

All notable changes to StackBraid are documented here, following [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Product specification, architecture, folder structure, roadmap and release checklist.
- Contributor and agent instructions.
- The `Identity` API contract (`contract/openapi.yaml`): auth, users and roles, validated against OpenAPI 3.1.
- A conformance suite (`contract/conformance/`), written against the contract and runnable against any backend on any database provider, with a stub fixture proving it catches contract violations.
- Generated TypeScript (`clients/typescript/`) and Dart (`clients/dart/`) clients, wired to regenerate from `contract/openapi.yaml` with one command (`scripts/generate-clients.sh`), plus a drift check and pre-commit hook that fail when the committed clients no longer match a fresh generation. Client generation is wired; no backend exists, so neither client has called a real server.

Nothing is implemented yet. There is no release.
