namespace StackBraid.Features.Identity.Contracts.Requests;

/// <summary>Mirrors <c>contract/openapi.yaml</c>'s <c>LoginRequest</c> schema.</summary>
public sealed record LoginRequest(string Email, string Password);
