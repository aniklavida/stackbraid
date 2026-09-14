using Shouldly;
using StackBraid.Shared.Localization;

namespace StackBraid.Shared.UnitTests.Localization;

public class JsonAppLocalizerTests
{
    private readonly JsonAppLocalizer _sut = new();

    [Fact]
    public void GetString_returns_english_text_by_default()
    {
        _sut.GetString("identity.invalid_credentials").ShouldBe("Invalid email or password.");
    }

    [Fact]
    public void GetString_returns_spanish_text_when_requested()
    {
        _sut.GetString("identity.invalid_credentials", "es").ShouldBe("Correo electrónico o contraseña no válidos.");
    }

    [Fact]
    public void GetString_falls_back_to_default_culture_for_an_unsupported_language()
    {
        _sut.GetString("identity.invalid_credentials", "fr-FR").ShouldBe("Invalid email or password.");
    }

    [Fact]
    public void GetString_matches_a_regional_variant_to_its_base_language()
    {
        _sut.GetString("identity.invalid_credentials", "es-MX").ShouldBe("Correo electrónico o contraseña no válidos.");
    }

    [Fact]
    public void GetString_falls_back_to_the_key_itself_when_missing_everywhere()
    {
        _sut.GetString("no.such.key").ShouldBe("no.such.key");
    }

    [Fact]
    public void GetString_substitutes_named_arguments_into_the_resolved_text()
    {
        // No catalogue entry today needs a placeholder, so this exercises the
        // substitution mechanism directly against the key-as-fallback path
        // (see GetString_falls_back_to_the_key_itself_when_missing_everywhere)
        // rather than a real message — the mechanism itself is what's under test.
        var text = _sut.GetString("hello {name}", "en", new Dictionary<string, string> { ["name"] = "World" });
        text.ShouldBe("hello World");
    }

    [Fact]
    public void SupportedCultures_lists_english_and_spanish()
    {
        _sut.SupportedCultures.ShouldBe(["en", "es"]);
    }

    [Fact]
    public void GetString_picks_the_highest_weighted_supported_language_in_a_multi_value_header()
    {
        // "fr" is unsupported and would be tried first under naive
        // first-value parsing; "es" is the highest-weighted range this
        // catalogue actually supports.
        _sut.GetString("identity.invalid_credentials", "fr,es;q=0.8,en;q=0.6")
            .ShouldBe("Correo electrónico o contraseña no válidos.");
    }

    [Fact]
    public void GetString_respects_explicit_q_values_out_of_header_order()
    {
        // "en" is listed first but "es" carries the higher weight.
        _sut.GetString("identity.invalid_credentials", "en;q=0.5,es;q=0.9")
            .ShouldBe("Correo electrónico o contraseña no válidos.");
    }

    [Fact]
    public void GetString_tolerates_a_malformed_q_value_by_treating_it_as_the_default_weight()
    {
        _sut.GetString("identity.invalid_credentials", "es;q=notanumber")
            .ShouldBe("Correo electrónico o contraseña no válidos.");
    }
}
