namespace StackBraid.Features.Identity.Contracts.Dtos;

/// <summary>Mirrors <c>contract/openapi.yaml</c>'s <c>TokenPair</c> schema. <c>TokenType</c> is always the literal <c>"Bearer"</c>.</summary>
public sealed record TokenPairDto(string AccessToken, string RefreshToken, string TokenType, DateTime ExpiresAt)
{
    public const string BearerTokenType = "Bearer";
}
