namespace StackBraid.Features.Identity.Contracts.Requests;

/// <summary>Mirrors <c>contract/openapi.yaml</c>'s <c>RegisterRequest</c> schema.</summary>
public sealed record RegisterRequest(string Email, string Password, string DisplayName);
