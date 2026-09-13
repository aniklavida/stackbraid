using System.Security.Cryptography;
using System.Text;

namespace StackBraid.Shared.Security;

/// <summary>
/// Generates and hashes opaque bearer tokens such as a refresh token: a
/// high-entropy random value handed to the caller once, with only its hash
/// ever persisted — a database read alone can never recover a usable token.
/// A fast, deterministic hash (SHA-256) is correct here, unlike password
/// hashing (<see cref="Pbkdf2PasswordHasher"/>): the token is already
/// high-entropy and looked up by hash on every request, so a slow,
/// deliberately-expensive hash would only punish legitimate traffic.
/// </summary>
public static class OpaqueTokenGenerator
{
    private const int TokenSizeBytes = 32;

    public static string GenerateRawToken() =>
        Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenSizeBytes));

    public static string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexStringLower(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
