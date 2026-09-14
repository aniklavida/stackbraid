from app.shared.localization.localizer import JsonAppLocalizer


def test_resolves_english_by_default() -> None:
    localizer = JsonAppLocalizer()

    assert localizer.get_string("identity.invalid_credentials") == "Invalid email or password."


def test_resolves_spanish_when_requested() -> None:
    localizer = JsonAppLocalizer()

    assert localizer.get_string("identity.invalid_credentials", culture="es") == "Correo electrónico o contraseña no válidos."


def test_falls_back_to_english_for_unsupported_culture() -> None:
    localizer = JsonAppLocalizer()

    assert localizer.get_string("identity.invalid_credentials", culture="fr") == "Invalid email or password."


def test_falls_back_to_the_key_itself_for_a_missing_key() -> None:
    localizer = JsonAppLocalizer()

    assert localizer.get_string("no.such.key") == "no.such.key"


def test_regional_variant_falls_back_to_the_language_bucket() -> None:
    localizer = JsonAppLocalizer()

    assert localizer.get_string("identity.invalid_credentials", culture="es-MX") == "Correo electrónico o contraseña no válidos."


def test_picks_the_highest_weighted_supported_language_in_a_multi_value_header() -> None:
    localizer = JsonAppLocalizer()

    # "fr" is unsupported and would be tried first under naive first-value
    # parsing; "es" is the highest-weighted range this catalogue supports.
    assert (
        localizer.get_string("identity.invalid_credentials", culture="fr,es;q=0.8,en;q=0.6")
        == "Correo electrónico o contraseña no válidos."
    )


def test_respects_explicit_q_values_out_of_header_order() -> None:
    localizer = JsonAppLocalizer()

    # "en" is listed first but "es" carries the higher weight.
    assert (
        localizer.get_string("identity.invalid_credentials", culture="en;q=0.5,es;q=0.9")
        == "Correo electrónico o contraseña no válidos."
    )


def test_tolerates_a_malformed_q_value_by_treating_it_as_the_default_weight() -> None:
    localizer = JsonAppLocalizer()

    assert (
        localizer.get_string("identity.invalid_credentials", culture="es;q=notanumber")
        == "Correo electrónico o contraseña no válidos."
    )
