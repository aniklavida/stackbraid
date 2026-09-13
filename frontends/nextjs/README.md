# frontends/nextjs — planned

**Status: not implemented.** This folder exists so the repository layout matches
[docs/STRUCTURE.md](../../docs/STRUCTURE.md); it holds no working code yet.

When the Next.js frontend is built (see [docs/ROADMAP.md](../../docs/ROADMAP.md),
step 4), it will mirror the Angular structure: `app/` holding thin route entry
points only, with the real code in `shared/`, `web/` and `admin/`, consuming a
generated, never-hand-edited `api/` built on [clients/typescript](../../clients/typescript).

Do not treat the presence of this folder as evidence of a working frontend.
