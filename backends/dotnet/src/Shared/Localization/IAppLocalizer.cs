namespace StackBraid.Shared.Localization;

/// <summary>
/// Resolves a message key (never a hard-coded sentence) into human text for
/// a given culture. <c>Problem.code</c> in the contract stays stable across
/// locales — this is what produces the localized <c>title</c>/<c>detail</c>
/// text that sits next to it. Backed by one implementation
/// (<see cref="JsonAppLocalizer"/>); a feature depends only on this
/// interface, never on how the strings are stored.
/// </summary>
public interface IAppLocalizer
{
    /// <summary>
    /// Looks up <paramref name="key"/> in <paramref name="culture"/>, falling
    /// back to the default culture and then to the key itself so a missing
    /// translation degrades to something readable instead of throwing.
    /// </summary>
    string GetString(string key, string? culture = null, IReadOnlyDictionary<string, string>? arguments = null);

    /// <summary>Every culture this localizer has strings for, default first.</summary>
    IReadOnlyList<string> SupportedCultures { get; }
}
