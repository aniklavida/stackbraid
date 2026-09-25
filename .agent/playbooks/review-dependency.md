# Playbook: Review Dependency

This playbook describes the mandatory procedure for auditing and admitting new third-party dependencies into StackBraid.

## Core Rules

1. **Every dependency shipped is inherited by every user of this skeleton.** A licence that binds downstream consumers is a defect, not a detail.
2. **Strict classification criteria:**
   - **Compiled into user code** (libraries compiled into backend binaries, frontend bundles, or mobile packages): **MIT, Apache-2.0, or BSD only** (including compatible permissive licences: PostgreSQL Licence, ISC, 0BSD, Python-2.0).
   - **Run as a separate process or build-only CLI tooling** (e.g. Postgres, RabbitMQ, Redis, Prometheus, shellcheck, yamllint): **Copyleft acceptable** (MPL-2.0, AGPL-3.0, GPL-3.0), because it is never compiled into, linked with, or redistributed alongside user code.
   - **Strictly rejected in ANY class:** Any licence with reciprocal-for-consumer obligations, source-available restrictions, or commercial revenue thresholds: **RPL, SSPL, RSAL, BSL, Commons Clause, or revenue-gated commercial licences**.
3. **Audit the transitive tree, not just the direct package.** A compliant direct package that brings in a non-compliant transitive dependency is rejected.
4. **Never audit from memory.** Verify the actual licence file and metadata on the registry on the day of adoption.

---

## Real Precedents in StackBraid

StackBraid has rejected widely used packages for licence reasons. These precedents establish binding standards for future reviews:

| Candidate Package | Licence Encountered | Decision & StackBraid Precedent |
|---|---|---|
| **MediatR** | RPL-1.5 (requires paid commercial licence above free tier) | **REJECTED.** StackBraid adopted `martinothamar/Mediator` (MIT), a compile-time source-generated mediator with zero runtime dependencies. |
| **AutoMapper** | RPL-1.5 (commercial licence required for business use) | **REJECTED.** StackBraid uses hand-written, type-safe mapping extension methods (`EntityMappingExtensions.cs`) with zero dependency overhead. |
| **FluentAssertions** (v8+) | Commercial licence required for commercial use | **REJECTED.** StackBraid standardizes on `Shouldly` (MIT) and xUnit. |
| **EPPlus** | Polyform Noncommercial 1.0.0 (paid licence required) | **REJECTED.** StackBraid adopted `ClosedXML` (MIT) and `DocumentFormat.OpenXml` (Apache-2.0) for document export. |
| **`MySql.Data`** | GPL-2.0-only (with proprietary exception sold by vendor) | **REJECTED.** StackBraid standardizes on `Pomelo.EntityFrameworkCore.MySql` (MIT) and `MySqlConnector` (MIT). |
| **Redis 7.4+** | RSALv2 / SSPLv1 (reciprocal / source-available) | **REJECTED.** StackBraid pins `redis:7.2-alpine` (the last BSD-3-Clause release) and architected realtime backplanes to be swappable with Linux Foundation's Valkey (BSD-3-Clause). |
| **Hangfire Core** | LGPL-3.0-only | **REJECTED for compiled backend scheduling.** StackBraid built a native Postgres/SQLServer/MySQL-backed `PersistentJobScheduler` using existing database connections, requiring zero external packages. |
| **QuestPDF Community** | Revenue-gated Community tier | **ACCEPTED AS BOUNDARY CASE BEHIND ABSTRACTION.** Admitted under open-source qualification, but placed strictly behind `IPdfGenerator` so users outside the tier can swap to `MinimalPdfGenerator` without code changes. |

---

## Step-by-Step Procedure

### Step 1: Verify the Licence at the Source

1. Check the official package registry metadata:
   - NuGet: Inspect `.nuspec` and declared `license` field.
   - npm: Run `npm view <package> license`.
   - PyPI: Inspect `pyproject.toml` or `PKG-INFO`.
   - pub.dev: Inspect `pubspec.yaml` on pub.dev.
2. Open the package repository's `LICENSE` file on GitHub/GitLab to verify that the registry metadata accurately reflects the actual source licence.

### Step 2: Audit the Transitive Dependency Tree

Examine the entire dependency graph resolved by package managers:
- .NET: Inspect `packages.lock.json`.
- Node: Inspect `package-lock.json`.
- Python: Check resolved dependencies in lockfile or virtual environment.
- Flutter: Inspect `pubspec.lock`.

Verify that every single transitive dependency satisfies the compiled-into-user-code rule (MIT / Apache / BSD).

### Step 3: Record in `docs/dependency-inventory.json`

StackBraid maintains a machine-readable source of truth in `docs/dependency-inventory.json`. Add the entry with:
- Ecosystem name (`nuget-dotnet-backend`, `npm-nextjs-frontend`, `python-backend`, etc.)
- Package name and exact pinned version
- Licence identifier (SPDX)
- Classification (`compiled-into-user-code`, `separate-process`, or `build-tooling`)
- Date verified (YYYY-MM-DD)

### Step 4: Document in `docs/DEPENDENCIES.md`

Add a narrative summary in `docs/DEPENDENCIES.md` explaining:
- Package name, version, and verified licence
- Why the dependency is needed
- Classification rationale and why it clears the StackBraid licence bar

### Step 5: Run the Automated Licence Gate

Run the repository dependency licence audit script:
```bash
node scripts/check-dependency-licenses.mjs
```

**Expected output:**
```
PASS: All dependencies verified against allowed licence policies.
```

If a package is unrecorded, carries an unapproved licence, or has drifted from the inventory, this check fails and names the exact offender.
