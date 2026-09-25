# Playbook: Add Admin Screen

This playbook describes how to add a full-featured administrative screen to both Next.js and Angular frontends in StackBraid.

## Core Rules

1. **Place within `admin/features/<feature>/`.** Admin surfaces live separately from public web features, but under the same application build.
2. **Follow the four layers:**
   - `domain/`: View models, filter state types, sorting definitions.
   - `data/`: Repository calling `@stackbraid/client-typescript`.
   - `application/`: TanStack Query hooks/services managing cached server state and mutations.
   - `presentation/`: Visual components, data tables, dialogs, and localized message files.
3. **Protect with authorization.** Only users with administrative roles may access the route.

---

## Step-by-Step Procedure

### Step 1: Implement Feature Layers in Next.js

Directory: `frontends/nextjs/src/admin/features/<feature>/`

1. **Domain (`domain/<feature>-filters.ts`)**:
   ```typescript
   export interface ProjectFilters {
     search?: string;
     page: number;
     pageSize: number;
   }
   ```

2. **Data Access (`data/<feature>-repository.ts`)**:
   ```typescript
   import { getProjects, type ProjectPage, type CreateProjectRequest } from '@stackbraid/client-typescript';

   export const projectsRepository = {
     async listProjects(filters: ProjectFilters): Promise<ProjectPage> {
       const response = await getProjects({ query: filters });
       if (response.error) throw response.error;
       return response.data;
     },
   };
   ```

3. **Application (`application/use-<feature>.ts`)**:
   ```typescript
   import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
   import { projectsRepository } from '../data/projects-repository';
   import type { ProjectFilters } from '../domain/project-filters';

   export function useProjects(filters: ProjectFilters) {
     return useQuery({
       queryKey: ['admin', 'projects', filters],
       queryFn: () => projectsRepository.listProjects(filters),
     });
   }
   ```

4. **Presentation (`presentation/<Feature>Page.tsx`)**:
   Build the administrative page incorporating:
   - Header with title, description, and primary "Create" action button.
   - Filter bar with search input and refresh button.
   - Data table (`Table`, `TableHeader`, `TableRow`, `TableCell`) displaying rows, dates, and status badges.
   - Pagination toolbar displaying current page, total count, and Next/Previous page buttons.
   - Localized text loaded via `useTranslations('projects')`.

5. **Route Entry Point (`src/app/(admin)/admin/<feature>/page.tsx`)**:
   ```tsx
   import { ProjectsPage } from '@/admin/features/projects/presentation/ProjectsPage';

   export default function Page() {
     return <ProjectsPage />;
   }
   ```

### Step 2: Implement Feature Layers in Angular

Directory: `frontends/angular/src/admin/features/<feature>/`

1. **Domain & Data**:
   Create `projects-repository.ts` injecting `ApiClient` and invoking generated SDK methods.
2. **Application**:
   Create `use-projects.ts` using `@tanstack/angular-query-experimental` or signal-based state.
3. **Presentation**:
   Create `projects-page.ts` and `projects-page.html` using `mat-table`, `mat-paginator`, and `mat-form-field`.
4. **Route Registration (`src/app/app.routes.ts`)**:
   Register under the admin shell route children with `authGuard`:
   ```typescript
   {
     path: 'admin/projects',
     canActivate: [authGuard],
     loadComponent: () => import('../admin/features/projects/presentation/projects-page').then(m => m.ProjectsPage),
   }
   ```

### Step 3: Register in Admin Navigation

Update navigation links in the admin sidebar:
- **Next.js:** Add entry in `frontends/nextjs/src/admin/layout/AdminShell.tsx`.
- **Angular:** Add entry in `frontends/angular/src/admin/layout/admin-shell.ts`.

### Step 4: Add Localized Messages

Add `presentation/messages/en.json` and `presentation/messages/es.json` in both frontends and verify:
```bash
node scripts/check-translation-keys.mjs
```

### Step 5: Test Accessibility & E2E

1. Ensure interactive elements use accessible labels (`aria-label` on icon buttons, proper table headers).
2. Run frontend builds to verify clean compilation:
   ```bash
   (cd frontends/nextjs && npm run build)
   (cd frontends/angular && npm run build)
   ```
