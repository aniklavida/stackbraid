# backends/dotnet — planned

**Status: not implemented.** This folder exists so the repository layout matches
[docs/STRUCTURE.md](../../docs/STRUCTURE.md); it holds no working code yet.

When the .NET backend is built (see [docs/ROADMAP.md](../../docs/ROADMAP.md), step 2
or 3), it will follow the structure documented there:

```
backends/dotnet/
├── src/
│   ├── Shared/       cross-cutting plumbing — no business meaning
│   ├── Database/     provider-specific (Postgres, SQL Server, MySQL)
│   ├── Features/
│   │   └── Identity/ auth, users, roles — the only feature shipped
│   └── Host/         composition root
├── tests/
└── StackBraid.sln
```

Do not treat the presence of this folder as evidence of a working backend.
