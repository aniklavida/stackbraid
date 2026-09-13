using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using StackBraid.Features.Identity.Persistence;

namespace StackBraid.Database.Postgres;

/// <summary>
/// Lets <c>dotnet ef migrations add</c> construct <see cref="IdentityDbContext"/>
/// at design time, without needing the whole <c>Host</c> composition root or
/// a real database — only used by the EF Core CLI, never at runtime (see
/// <see cref="ServiceCollectionExtensions.AddPostgresPersistence"/> for the
/// real, configuration-driven connection).
/// </summary>
public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=stackbraid_design;Username=postgres;Password=postgres",
            npgsql => npgsql.MigrationsAssembly(typeof(IdentityDbContextFactory).Assembly.FullName));

        return new IdentityDbContext(optionsBuilder.Options);
    }
}
