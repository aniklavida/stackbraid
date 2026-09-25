# Folder structure

## The naming rule

**Use the words engineers already know.** Invented vocabulary makes a reader learn a dialect before they can work, and a structure nobody recognises is a structure nobody adopts.

Four words, in every stack:

| Word | Meaning |
|---|---|
| `Shared` | Cross-cutting plumbing. **No business meaning.** |
| `Features/<name>` | One folder per domain area. **The same name in every stack.** |
| `Host` | Composition root — startup, dependency injection, configuration |
| `api` | The generated client. **Never hand-edited.** (frontend and mobile) |

So *"where is the identity code?"* has one answer in all five stacks.

**`Shared`, not `Core`** — in .NET, `Core` already means the domain layer in widely used templates. Reusing it for plumbing would confuse the engineers most likely to read this.

**`Features`, not `Modules`** — "module" is more precise in .NET but exists only there. `features` exists in all five, and the shared word matters more than local precision.

## One architecture, all five stacks

Every feature carries the same four layers. Two words are universal; two are named for what the layer actually talks to, because calling a frontend's API gateway "persistence" would be a lie.

| Layer | .NET · Python | Angular · Next.js | Flutter |
|---|---|---|---|
| Models and rules | `Domain/` | `domain/` | `domain/` |
| Use cases | `Application/` → Commands, Queries | `application/` | `application/` |
| Data access | `Persistence/` → the database | `data/` → the generated client | `data/` |
| Delivery | `Endpoints/` → HTTP in | `presentation/` → screens | `presentation/` |

The dependency direction is identical everywhere: **Domain knows nothing. Application knows Domain. Data and Delivery know Application. Nothing points inward from the edge.**

For a simple feature, `application/` may hold a single file. That is fine — a consistent shape across five stacks is worth one thin folder, and the alternative is business logic leaking into components.

## Backends

```
backends/dotnet/
├── src/
│   ├── Shared/                  cross-cutting — no business meaning
│   │   ├── Persistence/         provider-agnostic base, interceptors, conventions
│   │   ├── Messaging/  Storage/  Mailing/  Jobs/
│   │   ├── Documents/           Excel and PDF behind interfaces
│   │   ├── Caching/  Localization/
│   │   └── Web/                 errors, correlation ID, rate limiting, results
│   ├── Database/                provider-specific — one ships
│   │   ├── Postgres/            driver + its own migrations
│   │   ├── SqlServer/
│   │   └── MySql/
│   ├── Features/
│   │   └── Identity/            auth, users, roles — the only feature shipped
│   │       ├── Domain/
│   │       ├── Application/     Commands/ · Queries/
│   │       ├── Persistence/     entity configuration and repositories
│   │       ├── Contracts/       what other features may see
│   │       └── Endpoints/
│   └── Host/                    startup, DI, migrations, observability
└── tests/
    Shared · Features.Identity · Integration · ArchitectureTests
```

Python mirrors this exactly — `shared/`, `database/`, `features/identity/`, `host/` — using `Protocol` ports where .NET uses interfaces.

**The only difference between the two backends is dispatch.** .NET routes commands through a source-generated mediator; Python resolves handlers directly from the container. Same folders, same names, same flow, each idiomatic.

### Why the database has its own folder

Migrations are provider-specific — the generated SQL differs per database. One migrations folder cannot serve three providers, so each gets its own, while shared persistence and every feature's persistence stay agnostic.

```
Shared/Persistence/              knows no provider
Features/Identity/Persistence/   knows no provider
Database/<Provider>/             the only place a provider name appears
```

### Enforced, not documented

- **A feature never imports another feature's internals** — only its `Contracts`. This is what keeps features removable.
- **`Shared` never imports a feature.** The moment it does, it stops being shared and becomes a hidden dependency.
- **No provider name appears outside `Database/<Provider>/`.** The .NET architecture test
  `No_provider_name_appears_outside_Database` fails if any non-Database, non-Host assembly references a provider's driver,
  and Python's `Features and shared never import a database provider` import-linter contract does the same for
  `app.database`. The composition root is the one place a provider is named and chosen — .NET's `Host/Program.cs`
  (`Database:Provider`) and Python's `app/host/main.py` (`STACKBRAID_DATABASE_PROVIDER`).

All three are CI tests. A rule nobody enforces is a rule everybody breaks by month three.

## Frontends — web and admin side by side

Two surfaces, one application. Different layout, different navigation, same auth session, same generated client.

```
frontends/angular/src/app/
├── shared/          auth · http · i18n · ui · config
├── web/             THE PUBLIC SITE
│   ├── layout/
│   └── features/
│       ├── auth/    domain/ application/ data/ presentation/
│       ├── home/  profile/
├── admin/           THE ADMIN AREA
│   ├── layout/
│   └── features/
│       ├── users/   domain/ application/ data/ presentation/
│       └── dashboard/  roles/  audit/
└── api/             generated — never hand-edited
```

Next.js mirrors this: `app/` holds thin route entry points only, and the real code lives in `web/` and `admin/`.

**One build, one deployment, one auth session.** The admin area is what a user with the right role can reach — not a second application to deploy and secure separately.

**The two frontends deliberately do not look alike.** Angular uses Material, Next.js uses shadcn/ui, because a user installs one and never sees the other. Consistency matters for feature names and architecture, which developers move between — not for the styling of an alternative nobody runs.

## Mobile

```
mobile/flutter/lib/
├── shared/          auth · http · config · routing · theme · i18n · widgets
├── features/
│   └── auth/        domain/ · application/ · data/ · presentation/
└── api/             generated
```

There is no web/admin split on mobile — administration is a desktop job.

## The repository

```
stackbraid/
├── contract/        openapi.yaml · conformance/
├── backends/        dotnet/ · python/
├── frontends/       angular/ · nextjs/
├── mobile/          flutter/
├── clients/         typescript/ · dart/      generated, committed, never edited
├── infra/           compose.yaml · grafana/ · prometheus/ · loki/
├── .agent/          playbooks/               tool-neutral procedures
├── .claude/  .cursor/                        thin pointers
├── create/          the picker
└── docs/
```

## Five conventions

1. **Feature names are identical across every stack.** Adding `invoices` means a folder called `invoices` everywhere — not `Invoice`, not `billing`. Casing follows each language; the word does not change.
2. **`api/` is generated, committed and never hand-edited.** A hook regenerates it when the contract changes and fails the commit if it drifted.
3. **`Shared` holds nothing with business meaning** and never imports a feature.
4. **A feature never reaches another feature's internals** — only its `Contracts`.
5. **Every feature ships its own translations.** No central file nobody updates. `node scripts/check-translation-keys.mjs` walks every catalogue this repository ships — both backends, both web frontends, and the mobile app — and fails if a key exists in one shipped locale but not another, naming the exact file and key.
