from app.shared.security.opaque_token import generate_raw_token, hash_token


def test_generated_tokens_are_unique() -> None:
    assert generate_raw_token() != generate_raw_token()


def test_hash_is_deterministic() -> None:
    token = generate_raw_token()

    assert hash_token(token) == hash_token(token)


def test_different_tokens_hash_differently() -> None:
    assert hash_token(generate_raw_token()) != hash_token(generate_raw_token())
