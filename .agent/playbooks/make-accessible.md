# Playbook: Make Accessible

This playbook describes accessibility (a11y) standards, implementation patterns, and automated verification procedures in StackBraid.

## Core Rules

1. **Target WCAG 2.1 Level AA compliance.** All web pages and admin surfaces in Next.js and Angular must meet WCAG 2.1 Level AA criteria.
2. **Accessible locators drive test parity.** The repository's shared end-to-end tests (`e2e/identity-flow.ts`) drive pages exclusively using accessible roles, labels, and visible text (`getByRole`, `getByLabel`, `getByText`). Maintaining accessible HTML is what allows one test to verify both frontends without branching.
3. **Prefer semantic HTML over ARIA.** Use native HTML elements (`<button>`, `<main>`, `<nav>`, `<table>`, `<dialog>`) before adding custom ARIA roles. ARIA supplements HTML semantics; it does not replace them.

---

## Accessibility Checklist & Patterns

### 1. Form Controls and Labels

- **Explicit Labeling:** Every `<input>`, `<select>`, and `<textarea>` must be associated with a `<label>` element via matching `id` and `htmlFor` (React) or `for` (Angular):
  ```tsx
  <label htmlFor="email" className="text-sm font-medium">Email Address</label>
  <input id="email" type="email" name="email" required />
  ```
- **Icon-Only Buttons:** Any button that renders only an icon (e.g., delete, edit, close) must include an explicit `aria-label`:
  ```tsx
  <button aria-label="Delete project" onClick={handleDelete}>
    <Trash2Icon className="h-4 w-4" />
  </button>
  ```
- **Error States and Help Text:**
  When a field fails validation, link the error description to the input:
  ```tsx
  <input
    id="email"
    aria-invalid={hasError ? "true" : "false"}
    aria-describedby={hasError ? "email-error" : "email-hint"}
  />
  {hasError && <p id="email-error" role="alert" className="text-destructive text-sm">{errorMessage}</p>}
  ```

### 2. Semantic Document Structure

- **Landmarks:** Every page must contain one `<main>` landmark. Headers live in `<header>`, navigation links live in `<nav>`, and footers live in `<footer>`.
- **Heading Hierarchy:** Use logical, unskipped heading levels (`<h1>` -> `<h2>` -> `<h3>`). There must be exactly one `<h1>` per page representing the page title.
- **Data Tables:** Admin tables must use `<thead>`, `<th>` with `scope="col"`, and `<tbody>` so screen readers can announce column associations.

### 3. Keyboard Navigation & Focus Management

- **Visible Focus Indicators:** Never remove focus styles with `outline: none` without providing a high-contrast replacement. In Tailwind, ensure `focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2` is active.
- **Tab Order:** Ensure natural DOM tab order. Never use positive `tabindex` values (`tabindex="1"`). Use `tabindex="0"` only to make non-interactive custom elements focusable, and `tabindex="-1"` for programmatically focused containers.
- **Modal Focus Trapping:** Dialogs (`<dialog>` or Radix UI / Angular Material dialog primitives) must trap keyboard focus while open and return focus to the triggering element when dismissed.

### 4. Color Contrast and Motion

- **Contrast Ratios:** Text must satisfy a minimum contrast ratio of 4.5:1 against its background (3:1 for large text >= 18pt or bold >= 14pt).
- **Non-Color Indicators:** Status must never be conveyed by color alone. A badge must include readable text or an accompanying icon alongside the color tint.
- **Reduced Motion:** Respect user preferences for reduced motion by using Tailwind's `motion-reduce:` utilities.

---

## Automated Verification

### 1. Playwright Accessible Locators
Run the shared end-to-end test suite:
```bash
# In Next.js:
(cd frontends/nextjs && npm run test:e2e)

# In Angular:
(cd frontends/angular && npm run test:e2e)
```
If a locator like `page.getByRole('button', { name: 'Sign in' })` fails to resolve, check that the element has the correct accessible role and accessible name.

### 2. Automated Axe Auditing
Inject axe-core into Playwright tests to scan rendered DOM trees for WCAG violations:
```typescript
import AxeBuilder from '@axe-core/playwright';

test('page should have no automatically detectable accessibility violations', async ({ page }) => {
  await page.goto('/login');
  const accessibilityScanResults = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa'])
    .analyze();

  expect(accessibilityScanResults.violations).toEqual([]);
});
```
