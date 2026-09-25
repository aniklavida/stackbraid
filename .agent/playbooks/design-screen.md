# Playbook: Design Screen

This playbook outlines UI architecture, design token usage, and layout principles across Next.js, Angular, and Flutter in StackBraid.

## Core Rules

1. **Architecture consistency over visual uniformity.** Angular uses Angular Material; Next.js uses Tailwind CSS and shadcn/ui; Flutter uses Material 3. A user picks one stack and never sees the others. Consistency matters for directory structures, feature names, and layer separation — not identical CSS output.
2. **Two surfaces, one application.**
   - **`web/`**: Public landing, authentication flows (login, register), personal account profile. Focuses on content presentation, readability, and fast interaction.
   - **`admin/`**: Internal administrative operations (users, roles, system dashboard). Focuses on data density, filtering, sorting, pagination, and transactional dialogs.
   - Both share the same authentication session and generated API client.
3. **Presentation depends on Application, never on Data directly.** UI components in `presentation/` must never make direct fetch calls or import `@stackbraid/client-typescript` directly. They consume hooks/services exported from `application/`.

---

## Design System Foundations

### Next.js (Tailwind CSS + shadcn/ui)

- **Primitives:** Radix UI primitives wrapped in `@/components/ui/` (`button.tsx`, `dialog.tsx`, `table.tsx`, `input.tsx`, `card.tsx`).
- **Styling:** Tailwind utility classes configured with CSS variables in `src/app/globals.css`.
- **Theming:** Light/Dark theme switching supported via `next-themes`.
- **Icons:** `lucide-react` icons.

### Angular (Angular Material + Tailwind CSS)

- **Primitives:** Angular Material components (`MatTableModule`, `MatButtonModule`, `MatDialogModule`, `MatFormFieldModule`).
- **Styling:** Angular Material M3 theming configured in `src/styles.scss`, supplemented with utility classes.
- **Icons:** Material Icons / SVG symbols.

### Flutter Mobile (Material 3)

- **Theme:** Configured globally in `lib/shared/theme/app_theme.dart`.
- **Widgets:** Material 3 widgets (`Scaffold`, `FilledButton`, `Card`, `AppBar`).

---

## Screen Layout Guidelines

### 1. Web Shell Layout (`web/layout/`)
- Header / Top Navigation bar containing branding, navigation links, locale switcher (`LocaleSwitcher`), theme toggle, and auth button (Sign In / User Avatar Menu).
- Main content container: centered with standard max-width (`max-w-6xl` or `container mx-auto px-4 py-8`).
- Footer with repository/copyright links.

### 2. Admin Shell Layout (`admin/layout/`)
- Persistent collapsible sidebar navigation with active route highlights.
- Top action bar showing active tenant/organization, breadcrumbs, search, and user profile menu.
- Main work area: full-width data tables, filter toolbars, summary cards, and pagination footers.
- Protected by authentication and role-checking guards (`RequireAuth` / `auth.guard.ts`).

---

## Component Checklist

When designing any screen:
- [ ] **Loading states:** Render skeleton loaders (`Skeleton` in Next.js, `mat-spinner` or skeleton cards in Angular) rather than empty space during data fetching.
- [ ] **Empty states:** Provide clear empty-state illustrations, informative messages, and primary calls to action when a list has zero items.
- [ ] **Error boundaries:** Handle API errors gracefully; render localized user-friendly messages from Problem Details `detail` and `code`.
- [ ] **Form validation:** Validate inputs using domain validation rules; show field-specific error messages directly beneath the invalid input.
- [ ] **Responsive behavior:** Test layouts at mobile (`< 640px`), tablet (`640px - 1024px`), and desktop (`> 1024px`) breakpoints.
