# Playbook: Add Entity

This playbook describes how to model and configure a new domain entity in StackBraid while strictly preserving architectural layering and provider agnosticism.

## Core Rules

1. **Domain depends on nothing.** The `Domain/` (`domain/`) folder contains zero external framework imports. In .NET, it references only the BCL (no EF Core, no ASP.NET, no MediatR). In Python, it references only standard library modules (no SQLAlchemy, no Pydantic, no FastAPI).
2. **Persistence configuration is provider-agnostic.** Entity mappings in `Features/<Feature>/Persistence` must never import a specific database provider (Postgres, SQL Server, MySQL). Provider-specific drivers and migrations exist exclusively under `Database/<Provider>/`.
3. **Repository interfaces belong to Domain; implementations belong to Persistence.** The use cases in `Application/` depend on the domain repository interface/protocol, inverted at runtime by dependency injection.

---

## Step-by-Step Procedure

### Step 1: Model Entity in Domain (.NET)

1. Create entity file in `backends/dotnet/src/Features/<Feature>/Domain/Entities/<Entity>.cs`:
   ```csharp
   using StackBraid.Features.<Feature>.Domain.Common;

   namespace StackBraid.Features.<Feature>.Domain.Entities;

   public sealed class Project : Entity<Guid>, IAuditable
   {
       public string Name { get; private set; }
       public string Slug { get; private set; }
       public DateTime CreatedAt { get; set; }
       public DateTime? UpdatedAt { get; set; }

       private Project() { } // EF Core required constructor

       public Project(Guid id, string name, string slug)
       {
           if (string.IsNullOrWhiteSpace(name))
               throw new DomainException("Project name is required.");

           Id = id;
           Name = name.Trim();
           Slug = slug.Trim().ToLowerInvariant();
           CreatedAt = DateTime.UtcNow;
       }

       public void Rename(string newName)
       {
           if (string.IsNullOrWhiteSpace(newName))
               throw new DomainException("New project name is required.");

           Name = newName.Trim();
           UpdatedAt = DateTime.UtcNow;
       }
   }
   ```
2. Define repository interface in `backends/dotnet/src/Features/<Feature>/Domain/Repositories/IProjectRepository.cs`:
   ```csharp
   namespace StackBraid.Features.<Feature>.Domain.Repositories;

   public interface IProjectRepository
   {
       Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
       Task AddAsync(Project project, CancellationToken ct = default);
       Task UpdateAsync(Project project, CancellationToken ct = default);
       Task DeleteAsync(Project project, CancellationToken ct = default);
   }
   ```

### Step 2: Model Entity in Domain (Python)

1. Create entity in `backends/python/src/app/features/<feature>/domain/entities.py`:
   ```python
   from dataclasses import dataclass, field
   from datetime import datetime, timezone
   from uuid import UUID

   @dataclass
   class Project:
       id: UUID
       name: str
       slug: str
       created_at: datetime = field(default_factory=lambda: datetime.now(timezone.utc))
       updated_at: datetime | None = None

       def rename(self, new_name: str) -> None:
           trimmed = new_name.strip()
           if not trimmed:
               raise ValueError("New project name cannot be empty.")
           self.name = trimmed
           self.updated_at = datetime.now(timezone.utc)
   ```
2. Define repository protocol in `backends/python/src/app/features/<feature>/domain/repositories.py`:
   ```python
   from typing import Protocol
   from uuid import UUID
   from app.features.<feature>.domain.entities import Project

   class ProjectRepository(Protocol):
       async def get_by_id(self, id: UUID) -> Project | None: ...
       async def add(self, project: Project) -> None: ...
       async def update(self, project: Project) -> None: ...
       async def delete(self, project: Project) -> None: ...
   ```

### Step 3: Configure Persistence (.NET)

1. Create provider-agnostic entity configuration in `backends/dotnet/src/Features/<Feature>/Persistence/Configurations/<Entity>Configuration.cs`:
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using Microsoft.EntityFrameworkCore.Metadata.Builders;
   using StackBraid.Features.<Feature>.Domain.Entities;

   namespace StackBraid.Features.<Feature>.Persistence.Configurations;

   public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
   {
       public void Configure(EntityTypeBuilder<Project> builder)
       {
           builder.ToTable("projects");

           builder.HasKey(p => p.Id);
           builder.Property(p => p.Id).ValueGeneratedNever();

           builder.Property(p => p.Name)
               .IsRequired()
               .HasMaxLength(200);

           builder.Property(p => p.Slug)
               .IsRequired()
               .HasMaxLength(200);

           builder.HasIndex(p => p.Slug).IsUnique();

           builder.Property(p => p.CreatedAt).IsRequired();
           builder.Property(p => p.UpdatedAt);
       }
   }
   ```
2. In the feature DbContext (`IdentityDbContext.cs` or feature DbContext), ensure configurations are applied via:
   ```csharp
   modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProjectConfiguration).Assembly);
   ```
3. Implement repository in `backends/dotnet/src/Features/<Feature>/Persistence/Repositories/ProjectRepository.cs`:
   ```csharp
   public sealed class ProjectRepository(IdentityDbContext dbContext) : IProjectRepository
   {
       public async Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
           await dbContext.Set<Project>().FindAsync([id], ct);

       public async Task AddAsync(Project project, CancellationToken ct = default) =>
           await dbContext.Set<Project>().AddAsync(project, ct);

       public Task UpdateAsync(Project project, CancellationToken ct = default)
       {
           dbContext.Set<Project>().Update(project);
           return Task.CompletedTask;
       }

       public Task DeleteAsync(Project project, CancellationToken ct = default)
       {
           dbContext.Set<Project>().Remove(project);
           return Task.CompletedTask;
       }
   }
   ```

### Step 4: Configure Persistence (Python)

1. In `backends/python/src/app/features/<feature>/persistence/models.py`, declare the SQLAlchemy model:
   ```python
   from datetime import datetime
   from uuid import UUID
   from sqlalchemy import DateTime, String
   from sqlalchemy.orm import Mapped, mapped_column
   from app.shared.persistence.base import Base

   class ProjectModel(Base):
       __tablename__ = "projects"

       id: Mapped[UUID] = mapped_column(primary_key=True)
       name: Mapped[str] = mapped_column(String(200), nullable=False)
       slug: Mapped[str] = mapped_column(String(200), unique=True, nullable=False)
       created_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)
       updated_at: Mapped[datetime | None] = mapped_column(DateTime(timezone=True), nullable=True)
   ```
2. In `backends/python/src/app/features/<feature>/persistence/repositories.py`, implement the repository:
   ```python
   from uuid import UUID
   from sqlalchemy.ext.asyncio import AsyncSession
   from app.features.<feature>.domain.entities import Project
   from app.features.<feature>.persistence.models import ProjectModel

   class SqlAlchemyProjectRepository:
       def __init__(self, session: AsyncSession) -> None:
           self._session = session

       async def get_by_id(self, id: UUID) -> Project | None:
           model = await self._session.get(ProjectModel, id)
           if not model:
               return None
           return Project(id=model.id, name=model.name, slug=model.slug, created_at=model.created_at, updated_at=model.updated_at)
   ```

### Step 5: Verify Architecture

Execute the architecture test suites to guarantee no boundary leaks:
```bash
dotnet test backends/dotnet/tests/ArchitectureTests
(cd backends/python && uv run lint-imports)
```
Follow the `add-migration` playbook next to generate database migrations for all three database providers.
