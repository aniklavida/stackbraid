from app.shared.security.password_hasher import Pbkdf2PasswordHasher


def test_hash_then_verify_round_trips() -> None:
    hasher = Pbkdf2PasswordHasher()
    hashed = hasher.hash("correct horse battery staple")

    assert hasher.verify("correct horse battery staple", hashed) is True


def test_verify_rejects_wrong_password() -> None:
    hasher = Pbkdf2PasswordHasher()
    hashed = hasher.hash("correct horse battery staple")

    assert hasher.verify("wrong password", hashed) is False


def test_verify_rejects_malformed_hash() -> None:
    hasher = Pbkdf2PasswordHasher()

    assert hasher.verify("anything", "not-a-real-hash") is False


def test_two_hashes_of_same_password_differ() -> None:
    hasher = Pbkdf2PasswordHasher()

    assert hasher.hash("same password") != hasher.hash("same password")
