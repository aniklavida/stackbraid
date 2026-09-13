using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackBraid.Features.Identity.Domain.Entities;

namespace StackBraid.Features.Identity.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(r => r.Name).IsUnique();
        builder.Property(r => r.Description).HasMaxLength(500);

        // A JSON-text column rather than a Postgres `text[]` array — array
        // columns are Postgres-specific and this entity configuration must
        // stay provider-agnostic (see docs/STRUCTURE.md: no provider name
        // outside Database/). The same column type works unchanged on
        // SQL Server or MySQL if either ships later.
        builder.Property(r => r.Permissions)
            .HasConversion(
                permissions => JsonSerializer.Serialize(permissions, JsonOptions),
                json => JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>(),
                new ValueComparer<IReadOnlyCollection<string>>(
                    (a, b) => a!.SequenceEqual(b!),
                    c => c.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    c => c.ToList()))
            .HasColumnName("permissions")
            .IsRequired();

        builder.Property(r => r.CreatedAtUtc).IsRequired();
        builder.Property(r => r.UpdatedAtUtc).IsRequired();
    }
}
