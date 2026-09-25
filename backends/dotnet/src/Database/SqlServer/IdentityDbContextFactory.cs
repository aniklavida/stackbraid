using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using StackBraid.Features.Identity.Persistence;

namespace StackBraid.Database.SqlServer;

/// <summary>
/// Lets <c>dotnet ef migrations add</c> construct <see cref="IdentityDbContext"/>
/// at design time, without needing the whole <c>Host</c> composition root or
/// a real database — only used by the EF Core CLI, never at runtime (see
/// <see cref="ServiceCollectionExtensions.AddSqlServerPersistence"/> for the
/// real, configuration-driven connection).
/// </summary>
public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=stackbraid_design;User Id=sa;Password=ChangeMe!123;TrustServerCertificate=True",
            sqlServer => sqlServer.MigrationsAssembly(typeof(IdentityDbContextFactory).Assembly.FullName));

        return new IdentityDbContext(optionsBuilder.Options);
    }
}
