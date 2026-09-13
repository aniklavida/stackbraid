namespace StackBraid.Host.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Base64-encoded HMAC-SHA256 signing key. The value in
    /// <c>appsettings.Development.json</c> is a fixed, publicly-known
    /// development default — never used past local development, and
    /// replaced by a real secret (environment variable or secret store)
    /// in any deployed environment.
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "stackbraid";

    public string Audience { get; set; } = "stackbraid-clients";

    /// <summary>
    /// In seconds, not minutes — the conformance suite's expired-token
    /// check needs a genuinely short-lived token to exercise for real (see
    /// <c>contract/conformance/README.md</c>, "What this does not (and
    /// cannot) do"), which a minutes-granularity setting couldn't express.
    /// </summary>
    public int AccessTokenLifetimeSeconds { get; set; } = 900;
}
