# Playbook: Add Web Page

This playbook describes how to add a public or user-facing web page to both Next.js and Angular frontends in StackBraid.

## Core Rules

1. **Place within `web/features/<feature>/`.** Public and customer-facing pages reside under `web/`, completely distinct from the back-office `admin/` features.
2. **Follow the four layers:**
   - `domain/`: Client-side validation schemas, types.
   - `data/`: Repositories invoking `@stackbraid/client-typescript`.
   - `application/`: State management and queries/mutations.
   - `presentation/`: Page components and localized strings.
3. **Respect authentication boundaries.**
   - Public pages (landing, terms, docs, auth entry) do not require authentication.
   - User pages (dashboard, settings, personal profile) must be wrapped in `RequireAuth` (Next.js) or guarded by `authGuard` (Angular).

---

## Step-by-Step Procedure

### Step 1: Implement Feature in Next.js

Directory: `frontends/nextjs/src/web/features/<feature>/`

1. **Domain & Data**:
   Define validation schemas and repositories in `domain/` and `data/`.
2. **Application (`application/use-<feature>.ts`)**:
   Wrap repository calls with TanStack Query hooks.
3. **Presentation (`presentation/<Feature>Page.tsx`)**:
   Implement page layout inside the `WebShell`:
   ```tsx
   'use client';

   import { useTranslations } from 'next-intl';
   import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';

   export function OverviewPage() {
     const t = useTranslations('overview');

     return (
       <div className="container mx-auto px-4 py-8 max-w-5xl">
         <h1 className="text-3xl font-bold tracking-tight mb-6">{t('title')}</h1>
         <div className="grid gap-6 md:grid-cols-2">
           <Card>
             <CardHeader>
               <CardTitle>{t('summaryCard')}</CardTitle>
             </CardHeader>
             <CardContent>
               <p className="text-muted-foreground">{t('summaryDescription')}</p>
             </CardContent>
           </Card>
         </div>
       </div>
     );
   }
   ```
4. **Route Entry Point (`src/app/(web)/<path>/page.tsx`)**:
   - For a public page:
     ```tsx
     import { OverviewPage } from '@/web/features/overview/presentation/OverviewPage';

     export default function Page() {
       return <OverviewPage />;
     }
     ```
   - For an authenticated page:
     ```tsx
     'use client';

     import { RequireAuth } from '@/shared/auth/RequireAuth';
     import { SettingsPage } from '@/web/features/settings/presentation/SettingsPage';

     export default function Page() {
       return (
         <RequireAuth>
           <SettingsPage />
         </RequireAuth>
       );
     }
     ```

### Step 2: Implement Feature in Angular

Directory: `frontends/angular/src/web/features/<feature>/`

1. **Presentation (`presentation/<feature>-page.ts` & `.html`)**:
   Implement the standalone Angular component.
2. **Route Registration (`src/app/app.routes.ts`)**:
   Register under the `WebShell` child routes:
   ```typescript
   {
     path: 'overview',
     loadComponent: () => import('../web/features/overview/presentation/overview-page').then(m => m.OverviewPage),
   }
   ```
   If authentication is required, add `canActivate: [authGuard]`.

### Step 3: Register in Web Navigation

Update header or navigation links:
- **Next.js:** In `frontends/nextjs/src/web/layout/WebShell.tsx`.
- **Angular:** In `frontends/angular/src/web/layout/web-shell.ts`.

### Step 4: Add Translations

Add messages to `presentation/messages/en.json` and `presentation/messages/es.json` in both frontends:
```json
{
  "overview": {
    "title": "Account Overview",
    "summaryCard": "Activity",
    "summaryDescription": "View your recent activity and usage."
  }
}
```

Verify translation parity:
```bash
node scripts/check-translation-keys.mjs
```

### Step 5: Verify Build

Confirm both frontends compile without type or bundle errors:
```bash
(cd frontends/nextjs && npm run build)
(cd frontends/angular && npm run build)
```
