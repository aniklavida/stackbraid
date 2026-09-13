namespace StackBraid.Features.Identity.Endpoints.Authorization;

/// <summary>
/// The claim type an access token carries one instance of per permission
/// code the user's roles grant (see <c>Host</c>'s <c>JwtAccessTokenIssuer</c>
/// for where these are written). <see cref="RequirePermissionExtensions"/>
/// is what reads them back.
/// </summary>
public static class PermissionClaim
{
    public const string Type = "permission";
}
