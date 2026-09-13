namespace StackBraid.Features.Identity.Contracts.Requests;

/// <summary>Mirrors <c>contract/openapi.yaml</c>'s <c>RefreshRequest</c> schema — optional, the httpOnly cookie is the fallback.</summary>
public sealed record RefreshRequest(string? RefreshToken);
