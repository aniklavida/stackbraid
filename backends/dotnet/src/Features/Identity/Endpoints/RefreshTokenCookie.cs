using Microsoft.AspNetCore.Http;

namespace StackBraid.Features.Identity.Endpoints;

/// <summary>
/// The httpOnly refresh-token cookie every auth endpoint that issues or
/// consumes a token pair reads or writes — see
/// <c>contract/openapi.yaml</c>: <c>refreshToken=&lt;value&gt;; HttpOnly;
/// Secure; SameSite=Strict; Path=/v1/auth</c>. <c>Secure</c> is set
/// unconditionally to match the contract text exactly; a browser only
/// honours it over HTTPS, which is how this is actually served outside
/// local development.
/// </summary>
public static class RefreshTokenCookie
{
    public const string Name = "refreshToken";
    private const string CookiePath = "/v1/auth";

    public static void Set(HttpContext context, string rawRefreshToken) =>
        context.Response.Cookies.Append(Name, rawRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
        });

    public static void Clear(HttpContext context) =>
        context.Response.Cookies.Delete(Name, new CookieOptions { Path = CookiePath });

    public static string? Read(HttpContext context) =>
        context.Request.Cookies.TryGetValue(Name, out var value) ? value : null;
}
