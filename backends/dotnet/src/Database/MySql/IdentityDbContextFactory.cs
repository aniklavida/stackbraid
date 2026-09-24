using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using StackBraid.Features.Identity.Persistence;

namespace StackBraid.Database.MySql;

/// <summary>
/// Lets <c>dotnet ef migrations add</c> construct <see cref="IdentityDbContext"/>
/// at design time, without needing the whole <c>Host</c> composition root or
/// a real database — only used by the EF Core CLI, never at runtime (see
/// <see cref="ServiceCollectionExtensions.AddMySqlPersistence"/> for the
/// real, configuration-driven connection).
/// </summary>
public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseMySql(
            "Server=localhost;Port=3306;Database=stackbraid_design;User=root;Password=ChangeMe!123",
            ServiceCollectionExtensions.DefaultServerVersion,
            mySql => mySql.MigrationsAssembly(typeof(IdentityDbContextFactory).Assembly.FullName));

        return new IdentityDbContext(optionsBuilder.Options);
    }
}
