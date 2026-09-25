# Playbook: Add Localized String

This playbook describes the procedure for adding translated strings to all catalogues across StackBraid and verifying completeness.

## Core Rules

1. **Every feature ships its own translations.** There is no monolithic global file that turns into an unmaintained graveyard. Features own their localized messages.
2. **Every key exists in all supported locales.** Today StackBraid ships English (`en`) and Spanish (`es`). Adding a key to `en.json` without the matching key in `es.json` breaks CI.
3. **Automated gate enforcement.** `node scripts/check-translation-keys.mjs` runs on every CI build and inspects all catalogues across backends, web frontends, and mobile.

---

## Catalogue Locations

| Stack | Path | Format |
|---|---|---|
| **.NET Backend** | `backends/dotnet/src/Shared/Localization/Resources/` | Flat JSON (`en.json`, `es.json`) |
| **Python Backend** | `backends/python/src/app/shared/localization/resources/` | Flat JSON (`en.json`, `es.json`) |
| **Next.js Frontend** | `frontends/nextjs/src/[web\|admin]/features/<feature>/presentation/messages/` | Nested JSON (`en.json`, `es.json`) |
| **Angular Frontend** | `frontends/angular/src/[web\|admin]/features/<feature>/presentation/messages/` | Nested JSON (`en.json`, `es.json`) |
| **Flutter Mobile** | `mobile/flutter/lib/features/<feature>/presentation/i18n/` | Dart `Translations` map (`'en'`, `'es'`) |

---

## Step-by-Step Procedure

### Step 1: Add Server-Side Error & Message Strings

Both backends localize RFC 9457 Problem Details (`title` and `detail`) based on the request's negotiated `Accept-Language` header.

1. **.NET Backend** (`backends/dotnet/src/Shared/Localization/Resources/`):
   - In `en.json`:
     ```json
     "errors.projectNotFound": "Project not found",
     "errors.projectNotFoundDetail": "No project exists with the specified identifier."
     ```
   - In `es.json`:
     ```json
     "errors.projectNotFound": "Proyecto no encontrado",
     "errors.projectNotFoundDetail": "No existe ningún proyecto con el identificador especificado."
     ```
2. **Python Backend** (`backends/python/src/app/shared/localization/resources/`):
   - Add the identical keys and translations to `en.json` and `es.json`.

### Step 2: Add Frontend Feature Messages

Each web frontend feature maintains its own `messages/` folder under `presentation/`:

#### Next.js & Angular (`features/<feature>/presentation/messages/`)

1. **`en.json`**:
   ```json
   {
     "projects": {
       "title": "Projects",
       "create": "Create project",
       "empty": "No projects found.",
       "table": {
         "name": "Project name",
         "slug": "Slug",
         "createdAt": "Created"
       }
     }
   }
   ```
2. **`es.json`**:
   ```json
   {
     "projects": {
       "title": "Proyectos",
       "create": "Crear proyecto",
       "empty": "No se encontraron proyectos.",
       "table": {
         "name": "Nombre del proyecto",
         "slug": "Identificador",
         "createdAt": "Creado"
       }
     }
   }
   ```

### Step 3: Add Mobile Strings (Flutter)

In `mobile/flutter/lib/features/<feature>/presentation/i18n/<feature>_strings.dart`:
```dart
class ProjectStrings {
  static const Map<String, Map<String, String>> values = {
    'en': {
      'projects.title': 'Projects',
      'projects.create': 'Create project',
      'projects.empty': 'No projects found.',
    },
    'es': {
      'projects.title': 'Proyectos',
      'projects.create': 'Crear proyecto',
      'projects.empty': 'No se encontraron proyectos.',
    },
  };
}
```

### Step 4: Run the Translation Completeness Check

Run the repository verification script from the root:
```bash
node scripts/check-translation-keys.mjs
```

**Expected output:**
```
PASS: Checked N translation groups and M keys across all stacks; zero missing keys found.
```

If any key is missing from one locale, the script fails and names the exact file, missing key, and locales involved.
