using System.Text.RegularExpressions;

namespace StackBraid.Features.Identity.Domain.ValueObjects;

/// <summary>
/// A validated, normalized email address. Normalization is lower-casing
/// only — this domain treats <c>Ada@Example.com</c> and
/// <c>ada@example.com</c> as the same account, which is what
/// "the email address is already registered" (see
/// <c>contract/openapi.yaml</c>'s <c>/v1/auth/register</c>) actually means
/// to a user.
/// </summary>
public sealed partial record Email
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Email address cannot be empty.", nameof(input));
        }

        var normalized = input.Trim().ToLowerInvariant();
        if (!EmailPattern().IsMatch(normalized))
        {
            throw new ArgumentException($"'{input}' is not a valid email address.", nameof(input));
        }

        return new Email(normalized);
    }

    public override string ToString() => Value;

    /// <summary>
    /// Lets query code (e.g. <c>EF.Functions.Like(u.Email, ...)</c>) hand an
    /// <see cref="Email"/> to an API that expects a raw string, without
    /// every caller writing <c>.Value</c> by hand.
    /// </summary>
    public static implicit operator string(Email email) => email.Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
