// The copy engine: walks a source folder and reproduces it under a
// destination, skipping build artifacts and stack-crossing comment text.
// This is the only place `create` touches file bytes — everything else in
// this tool is choice-handling and templating.

import { existsSync, mkdirSync, readdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { sanitizeComments } from './sanitize.mjs';

// Build artifacts and editor/OS junk — never part of "the code in the
// repository", so never part of what create copies. Matches the spirit of
// the repository's own .gitignore.
const EXCLUDED_DIR_NAMES = new Set([
  'node_modules',
  'bin',
  'obj',
  '.venv',
  'venv',
  '__pycache__',
  '.pytest_cache',
  '.mypy_cache',
  '.ruff_cache',
  '.import_linter_cache',
  '.next',
  '.angular',
  '.dart_tool',
  '.vite-temp',
  '.vite',
  'dist',
  'build',
  'TestResults',
  '.idea',
  '.vs',
  '.git',
  'test-results',
  '.turbo',
  // Flutter/Xcode regenerate these on the next `flutter pub get` / `pod
  // install` — never part of "the code in the repository", and on macOS
  // `ephemeral/` holds a symlink farm that a naive copy can't follow safely.
  'ephemeral',
  'Pods',
  '.symlinks',
]);

const EXCLUDED_FILE_NAMES = new Set(['.DS_Store']);

function isTextFileExt(ext) {
  return [
    '.cs', '.py', '.dart', '.ts', '.tsx', '.js', '.mjs', '.cjs',
    '.yaml', '.yml', '.sh', '.json', '.md', '.txt', '.xml', '.csproj',
    '.sln', '.props', '.targets', '.gitignore', '.toml', '.cfg', '.ini',
    '.html', '.scss', '.css',
  ].includes(ext);
}

/**
 * Recursively copies `srcDir` into `destDir`.
 *
 * @param {string} srcDir
 * @param {string} destDir
 * @param {object} opts
 * @param {(relPath: string) => boolean} [opts.skip] return true to omit a
 *   file or directory entirely, given its path relative to srcDir
 * @param {RegExp} [opts.forbidden] when set, every copied text file has its
 *   comments scanned and redacted for these tokens
 * @param {{ files: number, redactedFiles: number, redactions: number }} [opts.stats]
 */
export function copyTree(srcDir, destDir, opts = {}) {
  const skip = opts.skip ?? (() => false);
  const stats = opts.stats ?? { files: 0, redactedFiles: 0, redactions: 0 };

  function walk(relDir) {
    const absSrc = path.join(srcDir, relDir);
    const entries = readdirSync(absSrc, { withFileTypes: true }).sort((a, b) =>
      a.name.localeCompare(b.name),
    );

    for (const entry of entries) {
      const relPath = relDir ? `${relDir}/${entry.name}` : entry.name;

      // Never follow a symlink — this repository's own tree owns exactly
      // one (a Flutter plugin symlink under the now-excluded `ephemeral/`),
      // and a generated project should never depend on a link that may not
      // resolve the same way on the machine it lands on.
      if (entry.isSymbolicLink()) continue;

      if (entry.isDirectory()) {
        if (EXCLUDED_DIR_NAMES.has(entry.name)) continue;
        if (skip(relPath)) continue;
        walk(relPath);
        continue;
      }

      if (EXCLUDED_FILE_NAMES.has(entry.name)) continue;
      if (skip(relPath)) continue;

      const absDestDir = path.join(destDir, relDir);
      mkdirSync(absDestDir, { recursive: true });
      const absSrcFile = path.join(absSrc, entry.name);
      const absDestFile = path.join(absDestDir, entry.name);
      const ext = path.extname(entry.name);

      stats.files += 1;

      if (opts.forbidden && isTextFileExt(ext)) {
        const original = readFileSync(absSrcFile, 'utf8');
        const { content, redactions } = sanitizeComments(original, ext, opts.forbidden);
        if (redactions > 0) {
          stats.redactedFiles += 1;
          stats.redactions += redactions;
        }
        writeFileSync(absDestFile, content);
      } else {
        writeFileSync(absDestFile, readFileSync(absSrcFile));
      }
    }
  }

  walk('');
  return stats;
}

export function ensureEmptyDir(dir) {
  if (existsSync(dir)) {
    const entries = readdirSync(dir);
    if (entries.length > 0) {
      throw new Error(`Target directory is not empty: ${dir}`);
    }
  } else {
    mkdirSync(dir, { recursive: true });
  }
}

export function writeFile(destDir, relPath, content) {
  const abs = path.join(destDir, relPath);
  mkdirSync(path.dirname(abs), { recursive: true });
  writeFileSync(abs, content);
}

export function copyFile(srcFile, destDir, relPath) {
  const abs = path.join(destDir, relPath);
  mkdirSync(path.dirname(abs), { recursive: true });
  writeFileSync(abs, readFileSync(srcFile));
}
