using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackBraid.Features.Identity.Application.Abstractions;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Endpoints.Authorization;

namespace StackBraid.Host.Security;

/// <summary>
/// The real implementation of the port <c>Application</c> declares —
/// everything JWT-specific (signing, claims, expiry) lives here, in the
/// composition root, exactly as <c>contract/openapi.yaml</c>'s
/// <c>bearerAuth</c> security scheme describes.
/// </summary>
public sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly JwtOptions _options;

    public JwtAccessTokenIssuer(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public IssuedAccessToken Issue(User user)
    {
        var now = DateTime.UtcNow;
        var expiresAtUtc = now.AddSeconds(_options.AccessTokenLifetimeSeconds);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(JwtRegisteredClaimNames.Name, user.DisplayName),
        };

        var permissions = user.Roles.SelectMany(r => r.Permissions).Distinct();
        claims.AddRange(permissions.Select(permission => new Claim(PermissionClaim.Type, permission)));

        var signingKey = new SymmetricSecurityKey(Convert.FromBase64String(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var value = new JwtSecurityTokenHandler().WriteToken(token);
        return new IssuedAccessToken(value, expiresAtUtc);
    }
}
