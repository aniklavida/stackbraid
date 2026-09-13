using StackBraid.Features.Identity.Domain.Entities;

namespace StackBraid.Features.Identity.Application.Abstractions;

public sealed record IssuedAccessToken(string Value, DateTime ExpiresAtUtc);

/// <summary>
/// Issues the short-lived access token a login or refresh returns. The
/// concrete signing mechanism (JWT) is an HTTP-layer concern — see
/// <c>contract/openapi.yaml</c>'s <c>bearerAuth</c> security scheme — so
/// this handler-facing port only asks for a token and its expiry, and the
/// real implementation is registered by whatever composes the HTTP host.
/// </summary>
public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(User user);
}
