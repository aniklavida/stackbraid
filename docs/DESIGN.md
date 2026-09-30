# StackBraid design system

StackBraid is developer infrastructure — a full-stack starter connecting backends, databases, web frontends, and mobile clients through a unified OpenAPI contract.

Its user interface follows its own design system: calm, dense, accessible, and structured for fast daily engineering workflows.

---

## Brand decision

- **Industry & role:** Developer infrastructure, tools, and runtime platforms.
- **Direction:** Technical Dense.
- **Vibe words:** `dense, exact, fast`. Every screen is evaluated against these three qualities.

### Five core brand values

| Property | Value | Rationale |
|---|---|---|
| **Hue (`--brand-h`)** | `215` | Technical cobalt blue; authoritative, clear, and calm for developer consoles. |
| **Chroma (`--brand-c`)** | `0.14` | One vivid accent to focus attention without visual noise. |
| **Warmth (`--warmth`)** | `0.005` | Near-zero warmth; pure slate neutrals with high legibility on dark and light surfaces. |
| **Radius scale (`--radius-scale`)** | `0.7` | Compact engineered curve (buttons/inputs: 7px, cards: 10px, badges: 3px). Fits within the 0.5–0.8 developer infrastructure standard. |
| **Type pairing** | Technical | `IBM Plex Sans` (UI & display) + `IBM Plex Mono` (code, IDs, metrics, tabular data). |

### Theme defaults

- **Default theme:** Dark (`data-theme="dark"`).
- **Secondary theme:** Light (`data-theme="light"`).
- **Control:** Persistent theme toggle available in both web and admin shells.

---

## Typography & self-hosted fonts

All typography is self-hosted with zero third-party font requests at runtime.

| Face | Role | Weight | Licence |
|---|---|---|---|
| `IBM Plex Sans` | Display headers, UI copy, navigation, buttons, form labels | 400 (regular), 500 (medium), 600 (semi-bold) | OFL-1.1 |
| `IBM Plex Mono` | Code blocks, job IDs, logs, metrics, tabular digits | 400 (regular), 500 (medium) | OFL-1.1 |

- Headings and body copy use sentence case.
- Monospace numerals are tabular and right-aligned in tables and metric cards.
- The product logo is set as a pure text wordmark in `IBM Plex Sans` semi-bold.

---

## System non-negotiables

1. **One primary action per screen:** Only the primary workflow action receives the primary accent button. Secondary actions use subtle or ghost styling.
2. **Accent budget under 5%:** The brand accent is reserved for focus rings, primary action buttons, active navigation markers, and determinate progress.
3. **Labels above inputs:** Form fields place clear labels above inputs with inline validation messages.
4. **No prohibited chrome:** No gradients, no coloured left borders, no emoji in chrome, no zebra striping in tables, and no modals for complex forms (modals only for non-reversible confirmation decisions).
5. **Standard icons:** All icons come exclusively from Lucide (MIT licence), sized to adjacent typography (16px / 18px / 20px).
6. **State coverage:** Every screen explicitly models empty, loading, ideal, error, and no-access states.

---

## Frontend implementation notes

### Angular (`frontends/angular`)
- **Tokens layer:** `src/tokens.css` with identical brand values and tokens mirrored from the Next.js shell.
- **Self-hosted typography:** IBM Plex Sans and IBM Plex Mono served from `public/fonts/` with `@font-face` definitions in `src/styles.scss`. Google Fonts links removed from `src/index.html`.
- **Theme management:** `ThemeService` and `ThemeToggleComponent` persisted in `localStorage` under `stackbraid.theme`, controlling the `data-theme` attribute on the root element (dark default, light secondary).
- **Component styling:** Component styles implemented adhering strictly to design rules (one primary action per screen, accent budget under 5%, labels above inputs, no zebra tables, no gradients, no colored left borders).
