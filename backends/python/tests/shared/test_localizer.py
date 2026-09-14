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
