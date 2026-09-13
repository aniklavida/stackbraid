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
    public void GetString_substitutes_named_arguments()
    {
        var text = _sut.GetString("validation.password.min_length", "en", new Dictionary<string, string> { ["minLength"] = "8" });
        text.ShouldBe("must be at least 8 characters");
    }

    [Fact]
    public void SupportedCultures_lists_english_and_spanish()
    {
        _sut.SupportedCultures.ShouldBe(["en", "es"]);
    }
}
