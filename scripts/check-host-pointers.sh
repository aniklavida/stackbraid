#!/usr/bin/env bash
# check-host-pointers.sh — verify thin multi-host pointers and duplicate guards.
#
# Checks load-bearing multi-host properties:
#
#   1. Resolution / Broken pointer guard:
#      Every supported host pointer file resolves to the canonical source
#      (AGENTS.md). If a pointer references a missing file or broken symlink,
#      the check fails and explicitly names the affected host.
#
#   2. Single source / No duplication guard:
#      Actual instruction text exists in exactly one source file (AGENTS.md).
#      Host pointer files carry only thin discovery/import metadata (under 500 bytes).
#      If any instruction text is duplicated into a host file or across files,
#      the check fails and explicitly names both files. Where two hosts want
#      identical pointer content, symlinks are permitted and not flagged.
#
#   3. Self-test mode (--self-test):
#      Verifies that:
#        a) One source produces correct pointers for every supported host.
#        b) Duplicating an instruction fails and names both files.
#        c) Breaking a pointer fails and names the host.
#        d) Symlinked host pointers pass without duplicate penalties.
#        e) Broken symlink fails and names the host.
#
# Usage:
#   scripts/check-host-pointers.sh [TARGET_DIR]
#   scripts/check-host-pointers.sh --self-test
#
# Exit status:
#   0  all checks pass
#   1  broken pointer, duplicated instruction, or invalid configuration

set -euo pipefail

# ── Host resolution helper ───────────────────────────────────────────────────

# identify_host <filepath>
identify_host() {
  local f="$1"
  local base
  base="$(basename "$f")"
  case "$base" in
    CLAUDE.md|.claude*) echo "Claude Code" ;;
    GEMINI.md|.gemini*) echo "Gemini CLI" ;;
    AGENTS.md|CODEX.md|.codex*) echo "Codex" ;;
    .cursorrules|*.mdc) echo "Cursor" ;;
    .windsurfrules) echo "Windsurf" ;;
    *copilot*) echo "GitHub Copilot" ;;
    *)
      # Fallback: check first heading in file if regular file
      if [ -f "$f" ]; then
        local heading
        heading="$(grep -E '^#[[:space:]]+' "$f" 2>/dev/null | head -n 1 | sed -E 's/^#[[:space:]]+//' || true)"
        if [ -n "$heading" ]; then
          echo "$heading"
          return
        fi
      fi
      echo "$base"
      ;;
  esac
}

# ── Self-test mode ───────────────────────────────────────────────────────────

run_self_tests() {
  echo "Running check-host-pointers.sh self-tests..."
  local tmp
  tmp="$(mktemp -d)"
  trap 'rm -rf "$tmp"' RETURN

  local script_path
  script_path="$(cd "$(dirname "$0")" && pwd)/check-host-pointers.sh"
  local gen_script_path
  gen_script_path="$(cd "$(dirname "$0")" && pwd)/generate-host-pointers.sh"

  # --- Self-test 1: Generator produces correct pointers from one source ---
  local t1="$tmp/t1"
  mkdir -p "$t1"
  cat <<'EOF' > "$t1/AGENTS.md"
# Contributor and agent instructions

The canonical guide for humans and coding agents working in this repository.

## The one rule that matters
The contract comes first and backends implement it faithfully.
EOF
  bash "$gen_script_path" "$t1" >/dev/null
  if ! bash "$script_path" "$t1" >/dev/null 2>&1; then
    echo "FAIL: Self-test 1 — generated pointers failed validation." >&2
    exit 1
  fi
  echo "PASS: self-test 1 — one source produces correct pointers for every supported host."

  # --- Self-test 2: Duplicating an instruction fails and names both files ---
  local t2="$tmp/t2"
  mkdir -p "$t2"
  cat <<'EOF' > "$t2/AGENTS.md"
# Contributor Guidelines
## Rules
The contract comes first and backends implement it faithfully.
EOF
  cat <<'EOF' > "$t2/CLAUDE.md"
# Claude Code
@AGENTS.md
The contract comes first and backends implement it faithfully.
EOF
  local out2
  out2="$(bash "$script_path" "$t2" 2>&1 || true)"
  if bash "$script_path" "$t2" >/dev/null 2>&1; then
    echo "FAIL: Self-test 2 — duplicated instruction was accepted." >&2
    exit 1
  fi
  if ! echo "$out2" | grep -q "AGENTS.md" || ! echo "$out2" | grep -q "CLAUDE.md"; then
    echo "FAIL: Self-test 2 — failure did not name both files. Output was:" >&2
    echo "$out2" >&2
    exit 1
  fi
  echo "PASS: self-test 2 — duplicate instruction fails check and names both files."

  # --- Self-test 3: Breaking a pointer fails and names the host ---
  local t3="$tmp/t3"
  mkdir -p "$t3"
  cat <<'EOF' > "$t3/AGENTS.md"
# Contributor Guidelines
EOF
  cat <<'EOF' > "$t3/CLAUDE.md"
# Claude Code
@NONEXISTENT.md
EOF
  local out3
  out3="$(bash "$script_path" "$t3" 2>&1 || true)"
  if bash "$script_path" "$t3" >/dev/null 2>&1; then
    echo "FAIL: Self-test 3 — broken pointer was accepted." >&2
    exit 1
  fi
  if ! echo "$out3" | grep -qi "Claude Code"; then
    echo "FAIL: Self-test 3 — failure did not name host 'Claude Code'. Output was:" >&2
    echo "$out3" >&2
    exit 1
  fi
  echo "PASS: self-test 3 — broken pointer fails check and names the host."

  # --- Self-test 4: Symlinked pointer passes without duplicate penalty ---
  local t4="$tmp/t4"
  mkdir -p "$t4"
  cat <<'EOF' > "$t4/AGENTS.md"
# Contributor Guidelines
EOF
  cat <<'EOF' > "$t4/GEMINI.md"
# Gemini
The canonical guide is **`AGENTS.md`** (tool-neutral). Read it before making changes.
EOF
  # Cursor symlinked to GEMINI.md
  ln -s "GEMINI.md" "$t4/.cursorrules"
  if ! bash "$script_path" "$t4" >/dev/null 2>&1; then
    echo "FAIL: Self-test 4 — symlinked host pointer failed validation." >&2
    exit 1
  fi
  echo "PASS: self-test 4 — symlinked host pointer resolves and avoids duplicate penalty."

  # --- Self-test 5: Broken symlink fails and names the host ---
  local t5="$tmp/t5"
  mkdir -p "$t5"
  cat <<'EOF' > "$t5/AGENTS.md"
# Contributor Guidelines
EOF
  ln -s "MISSING.md" "$t5/CODEX.md"
  local out5
  out5="$(bash "$script_path" "$t5" 2>&1 || true)"
  if bash "$script_path" "$t5" >/dev/null 2>&1; then
    echo "FAIL: Self-test 5 — broken symlink was accepted." >&2
    exit 1
  fi
  if ! echo "$out5" | grep -qi "Codex"; then
    echo "FAIL: Self-test 5 — failure did not name host 'Codex'. Output was:" >&2
    echo "$out5" >&2
    exit 1
  fi
  echo "PASS: self-test 5 — broken symlink fails check and names the host."

  echo "PASS: all check-host-pointers.sh self-tests passed successfully."
  exit 0
}

if [ "${1:-}" = "--self-test" ]; then
  run_self_tests
fi

TARGET_DIR="${1:-.}"
if [ ! -d "$TARGET_DIR" ]; then
  echo "FAIL: Target directory \"$TARGET_DIR\" does not exist." >&2
  exit 1
fi

SOURCE_FILE="$TARGET_DIR/AGENTS.md"
if [ ! -f "$SOURCE_FILE" ]; then
  echo "FAIL: Canonical source file AGENTS.md missing in \"$TARGET_DIR\"." >&2
  exit 1
fi

FAILED=0

# ── 1. Check Pointer Resolution & Thinness ───────────────────────────────────

# Find potential host pointer files in TARGET_DIR root and .cursor/rules
POINTER_FILES=()
for f in "$TARGET_DIR"/CLAUDE.md "$TARGET_DIR"/GEMINI.md "$TARGET_DIR"/CODEX.md "$TARGET_DIR"/.cursorrules; do
  if [ -e "$f" ] || [ -L "$f" ]; then
    POINTER_FILES+=("$f")
  fi
done

if [ -d "$TARGET_DIR/.cursor/rules" ]; then
  for mdc in "$TARGET_DIR/.cursor/rules"/*.mdc; do
    if [ -e "$mdc" ] || [ -L "$mdc" ]; then
      POINTER_FILES+=("$mdc")
    fi
  done
fi

if [ "${#POINTER_FILES[@]}" -eq 0 ]; then
  echo "FAIL: No host pointer files found in \"$TARGET_DIR\"." >&2
  exit 1
fi

for ptr in "${POINTER_FILES[@]}"; do
  host_name="$(identify_host "$ptr")"
  rel_ptr="$(basename "$ptr")"
  ptr_dir="$(dirname "$ptr")"

  # Check if symlink
  if [ -L "$ptr" ]; then
    target="$(readlink "$ptr")"
    if [ ! -e "$ptr_dir/$target" ] && [ ! -e "$TARGET_DIR/$target" ]; then
      echo "FAIL: Broken pointer for host \"$host_name\": $rel_ptr points to non-existent target \"$target\"." >&2
      FAILED=1
    fi
    continue
  fi

  # Regular pointer file
  if [ -f "$ptr" ]; then
    # Thinness check: must be a thin pointer under 500 bytes
    bytes="$(wc -c < "$ptr" | tr -d '[:space:]')"
    if [ "$bytes" -gt 500 ]; then
      echo "FAIL: Host pointer $rel_ptr for \"$host_name\" is not thin ($bytes bytes, ceiling: 500 bytes)." >&2
      FAILED=1
    fi

    # Pointer resolution check: inspect referenced files
    # 1. Claude import @path
    while IFS= read -r line; do
      case "$line" in
        "@"*)
          target_ref="$(printf '%s' "$line" | sed 's/^@//' | tr -d '[:space:]')"
          if [ -n "$target_ref" ] && [ ! -e "$ptr_dir/$target_ref" ] && [ ! -e "$TARGET_DIR/$target_ref" ]; then
            echo "FAIL: Broken pointer for host \"$host_name\": $rel_ptr references \"$target_ref\", which does not exist." >&2
            FAILED=1
          fi
          ;;
      esac
    done < "$ptr"

    # 2. General markdown / prose references like `AGENTS.md`
    # shellcheck disable=SC2016
    refs="$(grep -oE '`[A-Za-z0-9_-]+\.md`|\([A-Za-z0-9_.-]+\.md\)' "$ptr" 2>/dev/null | tr -d '`()' || true)"
    for ref in $refs; do
      if [ ! -e "$ptr_dir/$ref" ] && [ ! -e "$TARGET_DIR/$ref" ]; then
        echo "FAIL: Broken pointer for host \"$host_name\": $rel_ptr references \"$ref\", which does not exist." >&2
        FAILED=1
      fi
    done
  fi
done

# ── 2. Check for Duplicate Instruction Text ──────────────────────────────────

TMP_WORK="$(mktemp -d)"
trap 'rm -rf "$TMP_WORK"' EXIT

extract_sentences() {
  local src="$1"
  local out="$2"
  : > "$out"

  while IFS= read -r line || [ -n "$line" ]; do
    trimmed="$(printf '%s' "$line" | sed 's/^[[:space:]]*//')"
    [ -z "$trimmed" ] && continue

    case "$trimmed" in
      "#"*) continue ;;
      "---"*) continue ;;
      "\`\`\`"*) continue ;;
      "@"*) continue ;;
      "| "*) continue ;;
      "**Status:"*) continue ;;
      "Inherits"*) continue ;;
      "description:"*|"globs:"*|"alwaysApply:"*) continue ;;
    esac

    normalized="$(printf '%s' "$trimmed" | sed -E 's/^[0-9]+\.[[:space:]]*//' | sed -E 's/^[-*][[:space:]]*//')"
    [ -z "$normalized" ] && continue

    len="${#normalized}"
    if [ "$len" -ge 20 ]; then
      printf '%s\n' "$normalized" >> "$out"
    fi
  done < "$src"
}

SOURCE_SENTENCES="$TMP_WORK/source_sentences.txt"
extract_sentences "$SOURCE_FILE" "$SOURCE_SENTENCES"

for ptr in "${POINTER_FILES[@]}"; do
  [ -L "$ptr" ] && continue
  [ ! -f "$ptr" ] && continue

  PTR_SENTENCES="$TMP_WORK/ptr_sentences_$(basename "$ptr").txt"
  extract_sentences "$ptr" "$PTR_SENTENCES"

  while IFS= read -r sentence; do
    [ -z "$sentence" ] && continue
    if grep -Fqx "$sentence" "$SOURCE_SENTENCES"; then
      echo "FAIL: Instruction text duplicated across files:" >&2
      echo "  \"$sentence\"" >&2
      echo "    in: $(basename "$SOURCE_FILE")" >&2
      echo "    in: $(basename "$ptr")" >&2
      FAILED=1
    fi
  done < "$PTR_SENTENCES"
done

# Compare host pointer files against each other (excluding symlinks)
num_ptrs="${#POINTER_FILES[@]}"
if [ "$num_ptrs" -gt 1 ]; then
  for ((i=0; i<num_ptrs; i++)); do
    f1="${POINTER_FILES[$i]}"
    { [ -L "$f1" ] || [ ! -f "$f1" ]; } && continue
    for ((j=i+1; j<num_ptrs; j++)); do
      f2="${POINTER_FILES[$j]}"
      { [ -L "$f2" ] || [ ! -f "$f2" ]; } && continue
      if [ "$f1" -ef "$f2" ]; then
        continue
      fi

      s1="$TMP_WORK/ptr_sentences_$(basename "$f1").txt"
      s2="$TMP_WORK/ptr_sentences_$(basename "$f2").txt"
      if [ -f "$s1" ] && [ -f "$s2" ]; then
        while IFS= read -r sentence; do
          [ -z "$sentence" ] && continue
          if grep -Fqx "$sentence" "$s2"; then
            echo "FAIL: Instruction text duplicated across files:" >&2
            echo "  \"$sentence\"" >&2
            echo "    in: $(basename "$f1")" >&2
            echo "    in: $(basename "$f2")" >&2
            FAILED=1
          fi
        done < "$s1"
      fi
    done
  done
fi

if [ "$FAILED" -ne 0 ]; then
  exit 1
fi

echo "PASS: All host pointers resolve, and no instruction text is duplicated."
exit 0
