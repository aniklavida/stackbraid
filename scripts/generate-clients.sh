#!/usr/bin/env bash
# Regenerates both StackBraid clients from contract/openapi.yaml.
# This is the one command card 3 requires: it is never split into two,
# because two commands means one of them stops being run.
#
# Requires on PATH:
#   - Node.js >= 18.17 and npm (for the TypeScript client)
#   - Dart SDK >= 3.11 (for the Dart client — build_runner and
#     json_serializable need it; see clients/dart/README.md "Toolchain")
#
# Usage:
#   ./scripts/generate-clients.sh
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "== Checking toolchain =="

if ! command -v dart >/dev/null 2>&1; then
  echo "error: 'dart' not found on PATH. Install Dart SDK >= 3.11." >&2
  exit 1
fi

dart_version="$(dart --version 2>&1 | sed -nE 's/.*Dart SDK version: ([0-9]+)\.([0-9]+).*/\1.\2/p')"
dart_major="${dart_version%%.*}"
dart_minor="${dart_version##*.}"
if [ "$dart_major" -lt 3 ] || { [ "$dart_major" -eq 3 ] && [ "$dart_minor" -lt 11 ]; }; then
  echo "error: Dart SDK $dart_version found, but >= 3.11 is required." >&2
  echo "       build_runner >= 2.16 and json_serializable >= 6.14 both need it." >&2
  echo "       Upgrade the toolchain rather than pinning an older generator." >&2
  exit 1
fi
echo "  dart $dart_version OK"

if ! command -v npm >/dev/null 2>&1; then
  echo "error: 'npm' not found on PATH. Install Node.js >= 18.17." >&2
  exit 1
fi
echo "  npm $(npm --version) OK"

echo
echo "== Regenerating TypeScript client (clients/typescript) =="
(
  cd "$root_dir/clients/typescript"
  npm install --no-audit --no-fund
  npm run generate
)

echo
echo "== Regenerating Dart client (clients/dart) =="
(
  cd "$root_dir/clients/dart"
  npx --yes @openapitools/openapi-generator-cli generate -c openapi-generator-config.yaml
  dart pub get
  dart run build_runner build
)

echo
echo "Both clients regenerated. Review with 'git status' / 'git diff -- clients/'."
