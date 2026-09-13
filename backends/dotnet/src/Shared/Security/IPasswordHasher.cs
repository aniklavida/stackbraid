namespace StackBraid.Shared.Security;

/// <summary>
/// Hashes and verifies a password. No business meaning of its own — any
/// feature with accounts uses the same one implementation,
/// <see cref="Pbkdf2PasswordHasher"/>.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
