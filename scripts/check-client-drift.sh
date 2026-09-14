#!/usr/bin/env bash
# Fails if the committed clients differ from a fresh generation, and also
# proves the `create` picker (create/create.mjs) leaks no unchosen stack
# into any generated project and reproduces byte-for-byte.
#
# Regenerates both clients in place (via generate-clients.sh) and compares
# the result against what git already has tracked (the index if something
# is staged, HEAD otherwise). This is what makes "never hand-edited" a
# checked fact instead of a house rule: an edit made directly to generated
# output is overwritten by regeneration and shows up as drift here.
#
# The `create` picker check is appended to this same script, rather than
# added as its own CI job, because this environment cannot push a change
# to .github/workflows/ (no `workflow` OAuth scope on the token available
# here — the same constraint already documented for the held-back
# `dotnet`/`dotnet-integration` CI jobs). This job already runs
# unconditionally on every push and pull request with no path filter, so
# it is the natural place for another zero-dependency Node check to live
# until a future session can give `create` its own workflow job.
#
# Usage:
#   ./scripts/check-client-drift.sh
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root_dir"

echo "== Regenerating both clients to check for drift =="
"$root_dir/scripts/generate-clients.sh"

echo
echo "== Diffing clients/ against git =="
# Compares the working tree (which generate-clients.sh just overwrote with a
# fresh generation) against the index, so this also catches drift in files
# that were staged but never regenerated.
if ! git diff --exit-code --stat -- clients/ > /tmp/stackbraid-client-drift.stat 2>&1; then
  echo
  echo "DRIFT DETECTED — committed clients do not match a fresh generation:"
  echo
  cat /tmp/stackbraid-client-drift.stat
  echo
  git --no-pager diff -- clients/ | head -200
  echo
  echo "The working tree now holds the correct, regenerated output above."
  echo "Review it, 'git add clients/', and commit again."
  rm -f /tmp/stackbraid-client-drift.stat
  exit 1
fi
rm -f /tmp/stackbraid-client-drift.stat

untracked="$(git ls-files --others --exclude-standard -- clients/)"
if [ -n "$untracked" ]; then
  echo
  echo "DRIFT DETECTED — fresh generation produced files that are not tracked:"
  echo "$untracked"
  echo
  echo "Review them, 'git add clients/', and commit again."
  exit 1
fi

echo "No drift: clients/ matches a fresh generation."

echo
echo "== Checking the create picker: no leaked stacks, reproducible output =="
node "$root_dir/scripts/check-create-picker.mjs"
