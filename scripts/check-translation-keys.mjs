#!/usr/bin/env node
// Translation-completeness gate.
//
// Every feature ships its own translations, in every stack (see
// docs/STRUCTURE.md's "Five conventions") — but nothing before this script
// ever checked that a key added to one locale actually exists in the
// other. This walks every translation
// catalogue this repository ships (two backends, two web frontends, one
// mobile app) and fails if any key present in one shipped locale is
// missing from another, naming the exact file and key so the drift is a
// one-line fix, not a hunt.
//
// Deliberately zero runtime dependencies (Node built-ins only), matching
// the conformance suite's and the licence-audit gate's own rule: a tool
// that only ever runs against this repository's own files needs no
// third-party package, so it carries no licence to audit.
//
// Usage: node scripts/check-translation-keys.mjs

import { readFileSync, readdirSync, existsSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const rootDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const p = (...parts) => path.join(rootDir, ...parts);
const rel = (abs) => path.relative(rootDir, abs);

let failures = [];
let groupsChecked = 0;
let keysChecked = 0;

/**
 * One translation "group" is a single logical catalogue that ships more
 * than one locale — a JSON file pair (en.json/es.json) for the backends
 * and the two JSON-based frontends, or one Dart file holding an 'en' and
 * an 'es' map literal for Flutter. `locales` maps a locale code to the
 * Set of keys that locale's own text actually defines.
 */
function checkGroup(label, locales) {
  groupsChecked += 1;
  const localeNames = Object.keys(locales);
  const union = new Set();
  for (const keys of Object.values(locales)) {
    for (const key of keys) union.add(key);
  }
  keysChecked += union.size;

  for (const key of union) {
    const missingFrom = localeNames.filter((locale) => !locales[locale].has(key));
    if (missingFrom.length > 0) {
      failures.push(`${label}: key "${key}" is missing from locale(s) [${missingFrom.join(', ')}] (present in [${localeNames.filter((l) => !missingFrom.includes(l)).join(', ')}])`);
    }
  }
}

// --- Flatten a nested JSON messages object into dot-path keys ------------
function flattenKeys(obj, prefix = '') {
  const keys = [];
  for (const [key, value] of Object.entries(obj)) {
    const fullKey = prefix ? `${prefix}.${key}` : key;
    if (value !== null && typeof value === 'object' && !Array.isArray(value)) {
      keys.push(...flattenKeys(value, fullKey));
    } else {
      keys.push(fullKey);
    }
  }
  return keys;
}

function loadJsonKeySet(filePath) {
  const parsed = JSON.parse(readFileSync(filePath, 'utf8'));
  return new Set(flattenKeys(parsed));
}

// --- Group 1: flat dot-key JSON pairs (both backends' Shared localizer) --
const flatJsonGroups = [
  {
    label: '.NET backend (backends/dotnet/src/Shared/Localization/Resources)',
    dir: p('backends', 'dotnet', 'src', 'Shared', 'Localization', 'Resources'),
  },
  {
    label: 'Python backend (backends/python/src/app/shared/localization/resources)',
    dir: p('backends', 'python', 'src', 'app', 'shared', 'localization', 'resources'),
  },
];

for (const { label, dir } of flatJsonGroups) {
  const enPath = path.join(dir, 'en.json');
  const esPath = path.join(dir, 'es.json');
  if (!existsSync(enPath) || !existsSync(esPath)) {
    failures.push(`${label}: expected both en.json and es.json under ${rel(dir)}`);
    continue;
  }
  checkGroup(`${label} (${rel(dir)})`, {
    en: loadJsonKeySet(enPath),
    es: loadJsonKeySet(esPath),
  });
}

// --- Group 2: nested JSON "messages" pairs, one per feature, per web frontend
// Each frontend ships one en.json/es.json pair per feature-scoped `messages/`
// directory (`shared/i18n/messages`, `<area>/features/<feature>/presentation/messages`,
// `<area>/layout/messages`) — the "every feature ships its own translations"
// rule. Discovered by walking the frontend's `src/`, not hard-coded, so a
// newly added feature's messages are picked up automatically.
function findMessagesDirs(startDir) {
  const found = [];
  const walk = (dir) => {
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
      if (entry.name === 'node_modules' || entry.name.startsWith('.')) continue;
      const full = path.join(dir, entry.name);
      if (!entry.isDirectory()) continue;
      if (entry.name === 'messages') {
        found.push(full);
      } else {
        walk(full);
      }
    }
  };
  walk(startDir);
  return found;
}

const webFrontends = [
  { label: 'Angular', src: p('frontends', 'angular', 'src') },
  { label: 'Next.js', src: p('frontends', 'nextjs', 'src') },
];

for (const { label, src } of webFrontends) {
  if (!existsSync(src)) {
    failures.push(`${label}: expected source directory ${rel(src)}`);
    continue;
  }
  const messagesDirs = findMessagesDirs(src);
  if (messagesDirs.length === 0) {
    failures.push(`${label}: found no "messages" directories under ${rel(src)}`);
    continue;
  }
  for (const dir of messagesDirs) {
    const enPath = path.join(dir, 'en.json');
    const esPath = path.join(dir, 'es.json');
    if (!existsSync(enPath) || !existsSync(esPath)) {
      failures.push(`${label}: expected both en.json and es.json under ${rel(dir)}`);
      continue;
    }
    checkGroup(`${label} (${rel(dir)})`, {
      en: loadJsonKeySet(enPath),
      es: loadJsonKeySet(esPath),
    });
  }
}

// --- Group 3: Flutter's hand-rolled Dart translation maps -----------------
// `flutter gen-l10n` only supports one central `arb-dir`, incompatible with
// "every feature ships its own translations" (see clients/dart's own README
// for the parallel `swagger_parser` vs `openapi-generator` finding — a
// toolchain limitation, worked around rather than the rule being dropped).
// Each file instead defines a `Translations` map literal with one block per
// locale, in the same file — extracted here with brace-matching plus a
// regex over each block, not a full Dart parser (this repository has no
// Dart AST tooling, and one is not worth adding for a two-locale check).
function extractBracedBlock(text, needle, fromIndex) {
  const start = text.indexOf(needle, fromIndex);
  if (start === -1) return null;
  const braceStart = text.indexOf('{', start);
  if (braceStart === -1) return null;
  let depth = 0;
  for (let i = braceStart; i < text.length; i++) {
    if (text[i] === '{') depth += 1;
    else if (text[i] === '}') {
      depth -= 1;
      if (depth === 0) {
        return { block: text.slice(braceStart + 1, i), end: i };
      }
    }
  }
  return null;
}

function extractDartLocaleKeys(text, localeLabel) {
  const found = extractBracedBlock(text, `'${localeLabel}':`, 0);
  if (!found) return null;
  const keyPattern = /'([a-zA-Z0-9_.]+)':/g;
  const keys = new Set();
  let match;
  while ((match = keyPattern.exec(found.block)) !== null) {
    keys.add(match[1]);
  }
  return keys;
}

function findDartTranslationFiles(startDir) {
  const found = [];
  const walk = (dir) => {
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
      if (entry.name.startsWith('.')) continue;
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        walk(full);
      } else if (entry.name.endsWith('.dart')) {
        const text = readFileSync(full, 'utf8');
        if (/\bTranslations\b[^;]*=\s*{/.test(text) && text.includes("'en':") && text.includes("'es':")) {
          found.push(full);
        }
      }
    }
  };
  walk(startDir);
  return found;
}

const flutterLibDir = p('mobile', 'flutter', 'lib');
if (existsSync(flutterLibDir)) {
  const dartFiles = findDartTranslationFiles(flutterLibDir);
  if (dartFiles.length === 0) {
    failures.push(`Flutter: found no Dart translation maps (files defining 'en'/'es' blocks) under ${rel(flutterLibDir)}`);
  }
  for (const filePath of dartFiles) {
    const text = readFileSync(filePath, 'utf8');
    const en = extractDartLocaleKeys(text, 'en');
    const es = extractDartLocaleKeys(text, 'es');
    if (!en || !es) {
      failures.push(`Flutter (${rel(filePath)}): could not extract both 'en' and 'es' blocks`);
      continue;
    }
    checkGroup(`Flutter (${rel(filePath)})`, { en, es });
  }
} else {
  failures.push(`Flutter: expected ${rel(flutterLibDir)}`);
}

// --- Report ----------------------------------------------------------------
console.log(`Checked ${groupsChecked} translation catalogue(s), ${keysChecked} distinct key(s) across en/es.`);
if (failures.length > 0) {
  console.error(`\nFAILED — ${failures.length} translation-key mismatch(es) found:\n`);
  for (const f of failures) console.error(`  - ${f}`);
  console.error('\nEvery key shipped in one locale must exist in every other locale this stack ships.');
  process.exit(1);
}
console.log('OK — every translation key exists in every locale, in every stack.');
