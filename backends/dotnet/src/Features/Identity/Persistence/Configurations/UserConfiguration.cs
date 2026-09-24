using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.ValueObjects;

namespace StackBraid.Features.Identity.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasConversion(email => email.Value, value => Email.Create(value))
            .HasMaxLength(320)
            .IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(u => u.DeletedAtUtc);
        builder.Property(u => u.CreatedAtUtc).IsRequired();
        builder.Property(u => u.UpdatedAtUtc).IsRequired();
        builder.Property(u => u.LastLoginAtUtc);

        builder.HasMany(u => u.UserRoles)
            .WithOne()
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // UserRoles/Roles/DomainEvents are read-only projections over private fields —
        // EF writes through the backing field directly rather than a settable property.
        builder.Navigation(u => u.UserRoles).HasField("_userRoles").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(u => u.Roles);
        builder.Ignore(u => u.DomainEvents);
    }
}
