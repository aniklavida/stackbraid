namespace StackBraid.Features.Identity.Contracts.Requests;

/// <summary>Mirrors <c>contract/openapi.yaml</c>'s <c>UpdateUserRequest</c> — every field optional; only supplied fields change.</summary>
public sealed record UpdateUserRequest(string? DisplayName, string? Email);
