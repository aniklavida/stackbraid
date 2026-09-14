// Strips comment-only prose that names a stack the user did not choose.
//
// Why this exists: several files this script copies verbatim are working,
// tested source code that also carries doc comments comparing one stack to
// its sibling ("the same rule the .NET backend's own X follows") — true
// and useful when both siblings ship side by side in this repository, but
// a leaked, unfollowable cross-reference in a single-stack project. This
// module removes only the offending comment TEXT, never a line of real
// code, so a file's behaviour is provably unchanged — only what a human
// reads in passing changes.
//
// Deliberately conservative: it blanks a comment's content down to its own
// marker rather than trying to rewrite a grammatical sentence around the
// missing word. "// the same split the Next.js/Angular AuthProvider makes"
// becomes "//" — a little of the surrounding explanation is lost, but zero
// risk of leaving a broken sentence or, worse, a token this pass missed
// wrapped in new words that dodge the next run's own grep check.

const LINE_COMMENT_STYLES = {
  '.py': '#',
  '.yaml': '#',
  '.yml': '#',
  '.sh': '#',
  '.cs': '//',
  '.dart': '//',
  '.ts': '//',
  '.tsx': '//',
  '.js': '//',
  '.mjs': '//',
};

const PY_TRIPLE_QUOTES = ['"""', "'''"];

function countOccurrences(haystack, needle) {
  let count = 0;
  let index = 0;
  while (true) {
    index = haystack.indexOf(needle, index);
    if (index === -1) break;
    count += 1;
    index += needle.length;
  }
  return count;
}

/**
 * @param {string} content
 * @param {string} ext file extension including the leading dot
 * @param {RegExp} forbidden a single case-insensitive, global regex matching
 *   every token that names an unchosen stack
 * @returns {{ content: string, redactions: number }}
 */
export function sanitizeComments(content, ext, forbidden) {
  const lineCommentMarker = LINE_COMMENT_STYLES[ext];
  if (!lineCommentMarker) {
    return { content, redactions: 0 };
  }

  const lines = content.split('\n');
  let redactions = 0;
  let inBlockComment = false; // /* ... */ (C#, Dart, TS, JS)
  let inDocstring = false; // Python triple-quoted string
  let docstringDelim = null;

  const hasForbidden = (text) => {
    forbidden.lastIndex = 0;
    return forbidden.test(text);
  };

  const out = lines.map((line) => {
    const trimmed = line.trimStart();
    const indent = line.slice(0, line.length - trimmed.length);

    // --- Python triple-quoted docstrings -------------------------------
    if (ext === '.py') {
      if (inDocstring) {
        if (trimmed.includes(docstringDelim)) {
          // Closing line. If the closing line also carries a leaked token
          // before the delimiter, blank that prefix; keep the delimiter.
          const delimIndex = trimmed.indexOf(docstringDelim);
          const before = trimmed.slice(0, delimIndex);
          const after = trimmed.slice(delimIndex);
          inDocstring = false;
          if (hasForbidden(before)) {
            redactions += 1;
            return indent + after;
          }
          return line;
        }
        if (hasForbidden(trimmed)) {
          redactions += 1;
          return ''; // blank prose line inside an open string — always valid
        }
        return line;
      }
      for (const delim of PY_TRIPLE_QUOTES) {
        if (trimmed.startsWith(delim)) {
          const rest = trimmed.slice(delim.length);
          const closesHere = rest.includes(delim);
          if (!closesHere) {
            inDocstring = true;
            docstringDelim = delim;
          }
          if (hasForbidden(rest)) {
            redactions += 1;
            if (closesHere) {
              const closeIndex = rest.indexOf(delim);
              return indent + delim + rest.slice(closeIndex);
            }
            return indent + delim;
          }
          return line;
        }
      }
    }

    // --- C-style block comments -----------------------------------------
    if (lineCommentMarker === '//') {
      if (inBlockComment) {
        const closeIndex = line.indexOf('*/');
        if (closeIndex !== -1) {
          inBlockComment = false;
          const before = line.slice(0, closeIndex);
          const after = line.slice(closeIndex);
          if (hasForbidden(before)) {
            redactions += 1;
            return after;
          }
          return line;
        }
        if (hasForbidden(line)) {
          redactions += 1;
          return '';
        }
        return line;
      }
      const openIndex = line.indexOf('/*');
      if (openIndex !== -1 && !line.slice(0, openIndex).includes('//')) {
        const closeIndex = line.indexOf('*/', openIndex);
        if (closeIndex === -1) {
          inBlockComment = true;
        }
        const commentPortion =
          closeIndex === -1 ? line.slice(openIndex) : line.slice(openIndex, closeIndex + 2);
        if (hasForbidden(commentPortion)) {
          redactions += 1;
          const before = line.slice(0, openIndex);
          const marker = closeIndex === -1 ? '/*' : '/* */';
          const tail = closeIndex === -1 ? '' : line.slice(closeIndex + 2);
          return before + marker + tail;
        }
        return line;
      }
    }

    // --- Whole-line comment ("#" or "//" or "///") ----------------------
    if (trimmed.startsWith(lineCommentMarker)) {
      if (hasForbidden(trimmed)) {
        redactions += 1;
        const markerMatch = trimmed.match(/^\/{2,3}|^#+/);
        const marker = markerMatch ? markerMatch[0] : lineCommentMarker;
        return indent + marker;
      }
      return line;
    }

    // --- Trailing comment after real code --------------------------------
    if (lineCommentMarker === '//' || lineCommentMarker === '#') {
      const markerIndex = line.indexOf(lineCommentMarker);
      if (markerIndex !== -1) {
        const codePart = line.slice(0, markerIndex);
        const commentPart = line.slice(markerIndex);
        // Ignore a marker that only appears inside a string literal on this
        // line (heuristic: an odd number of unescaped quotes before it
        // means we are inside a string) — extremely rare in this codebase's
        // comment style, and worth erring toward leaving the line alone
        // rather than risking a code edit.
        const quoteCount = countOccurrences(codePart, '"') + countOccurrences(codePart, "'");
        if (quoteCount % 2 === 0 && hasForbidden(commentPart)) {
          redactions += 1;
          const markerMatch = commentPart.match(/^\/{2,3}|^#+/);
          const marker = markerMatch ? markerMatch[0] : lineCommentMarker;
          return codePart + marker;
        }
      }
    }

    return line;
  });

  return { content: out.join('\n'), redactions };
}

/**
 * Builds the combined, case-insensitive, whole-word regex for every token
 * that names a stack NOT in `chosen`. `chosen` lists every stack folder
 * name actually included (e.g. ['dotnet', 'postgres', 'nextjs']).
 */
export function buildForbiddenRegex(chosen) {
  const chosenSet = new Set(chosen.map((s) => s.toLowerCase()));
  // Each entry is a fully-formed regex fragment, not a bare word — ".NET"
  // starts with a non-word character, so wrapping it in a leading `\b`
  // (as a bare-word token would need) can never match: `\b` only fires at
  // a transition between a word and non-word character, and ".NET" is
  // almost always preceded by a space, i.e. two non-word characters in a
  // row. Each fragment below supplies exactly the boundary it needs.
  const ALL_STACK_TOKENS = {
    dotnet: ['\\.NET\\b', '\\bdotnet\\b'],
    python: ['\\bPython\\b'],
    angular: ['\\bAngular\\b'],
    nextjs: ['\\bNext\\.js\\b', '\\bNextjs\\b'],
    flutter: ['\\bFlutter\\b'],
  };
  const parts = [];
  for (const [stack, tokens] of Object.entries(ALL_STACK_TOKENS)) {
    if (chosenSet.has(stack)) continue;
    parts.push(...tokens);
  }
  if (parts.length === 0) return /$^/; // matches nothing
  return new RegExp(parts.join('|'), 'gi');
}
