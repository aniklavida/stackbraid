// The single definition of "what names a StackBraid stack" — one table, three
// consumers: the copy sanitizer (create/lib/sanitize.mjs), the playbook
// filter (create/lib/playbooks.mjs) and the picker's own check
// (scripts/check-create-picker.mjs). Three hand-kept copies of these tokens
// would eventually disagree, and a disagreement is a silent leak guarantee in
// one place and a false failure in another.
//
// Each entry is a list of fully-formed, case-insensitive regex *sources*, not
// bare words: ".NET" starts with a non-word character, so wrapping it in a
// leading `\b` (as a bare-word token would need) can never match — `\b` only
// fires at a transition between a word and a non-word character, and ".NET"
// is almost always preceded by a space, i.e. two non-word characters in a
// row. Each source below supplies exactly the boundary it needs.

export const STACK_TOKEN_SOURCES = {
  dotnet: ['\\.NET\\b', '\\bdotnet\\b'],
  python: ['\\bpython\\b'],
  angular: ['\\bangular\\b'],
  nextjs: ['\\bnext\\.js\\b', '\\bnextjs\\b'],
  flutter: ['\\bflutter\\b'],
};

/** Every stack name in this repository, in a stable order. */
export const ALL_STACKS = Object.keys(STACK_TOKEN_SOURCES);

/**
 * @param {string} stack
 * @returns {RegExp[]} fresh global, case-insensitive regexes for that stack
 */
export function stackRegexes(stack) {
  return (STACK_TOKEN_SOURCES[stack] ?? []).map((source) => new RegExp(source, 'gi'));
}

/**
 * Which stacks a piece of text names.
 *
 * @param {string} text
 * @returns {string[]} stack names, in `ALL_STACKS` order
 */
export function stacksNamedIn(text) {
  return ALL_STACKS.filter((stack) => stackRegexes(stack).some((re) => re.test(text)));
}
