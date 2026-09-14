namespace StackBraid.Shared.Realtime;

/// <summary>
/// Mirrors <c>contract/openapi.yaml</c>'s <c>RealtimeMessage</c> discriminated
/// union exactly — the JSON shape both backends push over their own
/// transport (SignalR here, native WebSockets in Python) must be byte
/// identical, so these three records are the single place that shape is
/// declared on this side. <c>Type</c> carries the contract's own
/// discriminator value and is never set by a caller. Property names match
/// the contract's own field names exactly (not, for instance, an
/// `OccurredAtUtc` C# convention) — the JSON naming policy
/// (<c>Host/Program.cs</c>'s <c>AddJsonProtocol</c>) only lower-cases the
/// first letter of whatever name is here.
/// </summary>
public abstract record RealtimeMessage(string Type, DateTime OccurredAt);

public sealed record UserDeactivatedMessage(Guid UserId, DateTime OccurredAt)
    : RealtimeMessage("user.deactivated", OccurredAt);

public sealed record UserRoleChangedMessage(Guid UserId, IReadOnlyList<RealtimeRoleSummary> Roles, DateTime OccurredAt)
    : RealtimeMessage("user.role_changed", OccurredAt);

public sealed record JobProgressMessage(Guid JobId, string Status, int Progress, DateTime OccurredAt)
    : RealtimeMessage("job.progress", OccurredAt);

/// <summary>The same fields as <c>contract/openapi.yaml</c>'s <c>Role</c> schema, kept separate from <c>RoleDto</c> so <c>Shared</c> never depends on the <c>Identity</c> feature.</summary>
public sealed record RealtimeRoleSummary(Guid Id, string Name, string? Description, IReadOnlyList<string> Permissions);
