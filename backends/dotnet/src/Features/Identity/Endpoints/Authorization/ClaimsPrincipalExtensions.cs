using System.Security.Claims;

namespace StackBraid.Features.Identity.Endpoints.Authorization;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The user ID an access token's <c>sub</c> claim carries — see <c>Host</c>'s <c>JwtAccessTokenIssuer</c>.</summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return subject is not null && Guid.TryParse(subject, out var id)
            ? id
            : throw new InvalidOperationException("The current principal does not carry a valid user id claim.");
    }
}
