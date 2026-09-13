# backends/python — planned

**Status: not implemented.** This folder exists so the repository layout matches
[docs/STRUCTURE.md](../../docs/STRUCTURE.md); it holds no working code yet.

When the Python backend is built (see [docs/ROADMAP.md](../../docs/ROADMAP.md), step 2
or 3), it will follow the structure documented there:

```
backends/python/
├── src/app/
│   ├── shared/       cross-cutting plumbing — no business meaning
│   ├── database/     provider-specific (postgres, sqlserver, mysql)
│   ├── features/
│   │   └── identity/ the same feature, the same name as the .NET backend
│   └── host/         composition root
├── tests/
└── pyproject.toml
```

Do not treat the presence of this folder as evidence of a working backend.
