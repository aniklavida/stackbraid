// The agent playbooks, filtered to the stacks the user actually chose.
//
// A playbook in this repository is written for the whole multi-stack
// repository: `run-conformance.md` carries a `.NET` step and a `Python` step,
// `add-web-page.md` covers Next.js and Angular. Copying one of those verbatim
// into a single-stack generated project would hand a coding agent
// instructions for a stack that is not in the project at all — the exact
// failure this filtering exists to prevent (see docs/SPEC.md §9: "Only the
// playbooks matching the chosen stacks are installed").
//
// How the filter works, and why it is deliberately blunt:
//
//   * A markdown document is parsed into a heading tree — one node per
//     heading, holding its own lines and its sub-sections.
//   * A node whose text (heading included) names any stack the project did
//     NOT choose is dropped whole, with everything under it. A node that
//     names only chosen stacks, or no stack at all, is kept byte-for-byte.
//   * A heading whose body is empty and whose sub-sections were all dropped
//     goes too, so a kept playbook never shows a section with nothing under
//     it.
//   * Nothing is rewritten. The same conservative choice the comment
//     sanitizer already makes: blanking a word out of a sentence can leave
//     broken prose that dodges the next run's own grep check, whereas a
//     dropped block is unambiguously about something this project does not
//     contain.
//   * The document's own `# Title` is always kept, so an installed playbook
//     still reads as a playbook. When the introduction under it was about an
//     absent stack it is replaced with a note saying the file is a filtered
//     view.
//   * A playbook left with no `##` section at all is not installed: it had
//     nothing left to say about the chosen stacks.
//
// The cost is honest and worth stating: when one block holds instructions for
// two stacks at once (a single shell fence that runs both backends, say), the
// whole block goes and the agent is left with the per-stack sections around
// it. An agent that meets a gap is missing context it should not have had
// anyway, and nothing left in the file points it at code that does not exist.

import { existsSync, readdirSync, readFileSync } from 'node:fs';
import path from 'node:path';

import { stacksNamedIn } from './stacks.mjs';

const PLAYBOOKS_DIR = path.join('.agent', 'playbooks');

// Which stacks each playbook's instructions are FOR — i.e. the code an agent
// following it would write. A playbook is installed only when the project
// contains at least one of them; the sections are then filtered per block, so
// a project with one of two frontends gets that frontend's steps and not the
// other's.
//
// This is a judgement the text alone cannot make: `design-screen.md` never
// writes the word "Angular" in its layout section, but a `web/layout/`
// Tailwind shell is still a frontend instruction, and a backend-only project
// has no frontend to lay out. So the scope is declared here, per playbook,
// and the check in scripts/check-create-picker.mjs fails if a playbook in
// `.agent/playbooks/` has no entry — a new playbook cannot silently become
// advice for every generated project.
const PLAYBOOK_STACKS = {
  // Backend work. Present whenever a backend is, which is always.
  'add-endpoint.md': ['dotnet', 'python', 'angular', 'nextjs'],
  'add-entity.md': ['dotnet', 'python'],
  'add-background-job.md': ['dotnet', 'python'],
  'add-migration.md': ['dotnet', 'python'],
  'run-conformance.md': ['dotnet', 'python'],

  // Cross-cutting: a procedure for whichever stacks the project has.
  'add-feature.md': ['dotnet', 'python', 'angular', 'nextjs', 'flutter'],
  'add-localized-string.md': ['dotnet', 'python', 'angular', 'nextjs', 'flutter'],
  'review-architecture.md': ['dotnet', 'python', 'angular', 'nextjs', 'flutter'],
  'review-dependency.md': ['dotnet', 'python', 'angular', 'nextjs', 'flutter'],

  // Client regeneration only makes sense where a generated client exists.
  'regenerate-clients.md': ['angular', 'nextjs', 'flutter'],

  // UI work. A backend-only project has no screen to design, style, expose at
  // /admin, or make accessible.
  'design-screen.md': ['angular', 'nextjs', 'flutter'],
  'add-admin-screen.md': ['angular', 'nextjs'],
  'add-web-page.md': ['angular', 'nextjs'],
  'make-accessible.md': ['angular', 'nextjs'],
};

/** The stacks a playbook is for, or null when it has no declared scope. */
export function playbookScope(repoRoot, name) {
  if (!(name in PLAYBOOK_STACKS)) {
    throw new Error(
      `${name} has no declared scope in create/lib/playbooks.mjs (PLAYBOOK_STACKS). ` +
        'Every playbook must state which stacks it is for, so it is installed only for the projects ' +
        'that contain them. Add an entry naming the stacks its instructions are for.',
    );
  }
  return PLAYBOOK_STACKS[name];
}

// Replaces the introduction of an installed playbook whose own introduction
// named a stack this project does not have. Names no stack itself, so it can
// never reintroduce the leak this module exists to prevent.
const FILTER_NOTE = [
  '<!--',
  '  Filtered for this project by the StackBraid `create` picker: only the',
  '  sections covering the stacks this project actually contains are',
  '  included. See AGENTS.md for the full contributor and agent rules.',
  '-->',
].join('\n');

const HEADING_RE = /^(#{1,6})\s+(.*)$/;
const FENCE_RE = /^\s*(```|~~~)/;

/**
 * Parses markdown into a heading tree.
 *
 * Each node is `{ level, heading, lines, children }`, where `lines` holds the
 * heading line itself plus its own body, and `children` are the sections
 * nested under it. Text before the first heading becomes a node with
 * `heading === null`.
 *
 * Lines inside a fenced code block are body text whatever they look like: a
 * shell comment (`# For Python:`) inside a fence is not a heading, and treating
 * it as one would split the command in half — keeping the lines above it
 * because their block reads clean, and dropping the lines below it with the
 * other backend's heading.
 *
 * @param {string} markdown
 */
function parseBlocks(markdown) {
  const root = { level: 0, heading: null, lines: [], children: [] };
  const stack = [root];
  let inFence = false;

  for (const line of markdown.split('\n')) {
    if (FENCE_RE.test(line)) inFence = !inFence;
    const match = inFence ? null : line.match(HEADING_RE);
    if (!match) {
      stack[stack.length - 1].lines.push(line);
      continue;
    }
    const node = { level: match[1].length, heading: match[2].trim(), lines: [line], children: [] };
    while (stack.length > 1 && stack[stack.length - 1].level >= node.level) stack.pop();
    stack[stack.length - 1].children.push(node);
    stack.push(node);
  }
  return root;
}

function mentionsUnchosen(lines, chosen) {
  return stacksNamedIn(lines.join('\n')).some((stack) => !chosen.has(stack));
}

function hasContent(lines) {
  return lines.some((line) => line.trim() !== '');
}

/**
 * Filters one playbook's markdown to the chosen stacks.
 *
 * @param {string} markdown
 * @param {Set<string>} chosen chosen stack names, e.g. {'dotnet', 'angular'}
 * @returns {{ content: string, kept: string[], dropped: string[] }|null} null
 *   when nothing worth installing is left
 */
export function filterPlaybook(markdown, chosen) {
  const root = parseBlocks(markdown);
  const state = { kept: [], dropped: [], sections: 0 };

  const filterNode = (node, isTitle) => {
    if (isTitle) {
      // The document title is never about a stack, so it is kept even when
      // the introduction under it is not. That introduction is filtered as
      // its own block, with the filter note standing in for it.
      const body = node.lines.slice(1);
      const parts = [];
      if (hasContent(body)) {
        parts.push(mentionsUnchosen(body, chosen) ? FILTER_NOTE : body.join('\n'));
      }
      for (const child of node.children) {
        const filtered = filterNode(child, false);
        if (filtered !== null) parts.push(filtered);
      }
      if (parts.length === 0) {
        state.dropped.push(node.heading ?? '(introduction)');
        return null;
      }
      state.kept.push(node.heading);
      return [node.lines[0], ...parts].join('\n\n');
    }

    if (mentionsUnchosen(node.lines, chosen)) {
      state.dropped.push(node.heading ?? '(introduction)');
      return null;
    }

    // A heading's own line is kept whenever anything survives under it, so a
    // step never loses its title just because the prose under it was dropped
    // — and a heading with neither its own prose nor a surviving section is
    // dropped, so an installed playbook never shows a section with nothing
    // in it.
    const ownBody = hasContent(node.lines.slice(1));
    const parts = [];
    if (ownBody) parts.push(node.lines.join('\n'));
    for (const child of node.children) {
      const filtered = filterNode(child, false);
      if (filtered !== null) parts.push(filtered);
    }
    if (parts.length === 0) {
      state.dropped.push(node.heading ?? '(introduction)');
      return null;
    }
    if (!ownBody) parts.unshift(node.lines[0]);
    if (node.level >= 2) state.sections += 1;
    return parts.join('\n\n');
  };

  const blocks = [];
  if (hasContent(root.lines)) {
    if (mentionsUnchosen(root.lines, chosen)) {
      state.dropped.push('(introduction)');
    } else {
      blocks.push(root.lines.join('\n'));
    }
  }
  for (const child of root.children) {
    const filtered = filterNode(child, child.level === 1);
    if (filtered !== null) blocks.push(filtered);
  }

  // Nothing of substance survived: this playbook was entirely about stacks
  // this project does not have, so it is not installed at all.
  if (state.sections === 0) return null;

  const content = blocks
    .join('\n\n')
    .replace(/\n{3,}/g, '\n\n')
    .replace(/\s*$/, '\n');
  return { content, kept: state.kept, dropped: state.dropped };
}

/**
 * Reads every playbook in this repository and filters it to the chosen
 * stacks.
 *
 * @param {string} repoRoot
 * @param {Set<string>} chosen
 * @returns {{ name: string, content: string, kept: string[], dropped: string[] }[]}
 */
export function planPlaybooks(repoRoot, chosen) {
  const dir = path.join(repoRoot, PLAYBOOKS_DIR);
  if (!existsSync(dir)) return [];

  const plan = [];
  for (const entry of readdirSync(dir).sort()) {
    if (!entry.endsWith('.md')) continue;
    const scope = playbookScope(repoRoot, entry);
    if (!scope.some((stack) => chosen.has(stack))) continue;
    const filtered = filterPlaybook(readFileSync(path.join(dir, entry), 'utf8'), chosen);
    if (!filtered) continue;
    plan.push({ name: entry, ...filtered });
  }
  return plan;
}

/** Every playbook filename this repository ships, filtered or not. */
export function listPlaybookNames(repoRoot) {
  const dir = path.join(repoRoot, PLAYBOOKS_DIR);
  if (!existsSync(dir)) return [];
  return readdirSync(dir)
    .filter((entry) => entry.endsWith('.md'))
    .sort();
}

/**
 * The stack names a playbook mentions, read from the playbook itself rather
 * than from a hand-kept table beside it — so nothing here can drift from the
 * text it claims to describe.
 *
 * @param {string} repoRoot
 * @param {string} name playbook filename
 * @returns {string[]}
 */
export function playbookStacks(repoRoot, name) {
  return stacksNamedIn(readFileSync(path.join(repoRoot, PLAYBOOKS_DIR, name), 'utf8'));
}
