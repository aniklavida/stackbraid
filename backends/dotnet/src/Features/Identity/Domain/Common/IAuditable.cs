namespace StackBraid.Features.Identity.Domain.Common;

/// <summary>Marks an entity that tracks when it was created and last changed. <c>IdentityDbContext</c> stamps both on every save.</summary>
public interface IAuditable
{
    DateTime CreatedAtUtc { get; set; }
    DateTime UpdatedAtUtc { get; set; }
}
