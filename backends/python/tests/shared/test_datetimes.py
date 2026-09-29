from datetime import datetime, timedelta, timezone

from app.shared.persistence.datetimes import ensure_utc, ensure_utc_optional


def test_ensure_utc_attaches_utc_to_a_naive_value() -> None:
    naive = datetime(2026, 1, 1, 12, 0, 0)

    assert ensure_utc(naive) == naive.replace(tzinfo=timezone.utc)
    assert ensure_utc(naive).tzinfo == timezone.utc


def test_ensure_utc_converts_an_offset_value_to_utc() -> None:
    offset = datetime(2026, 1, 1, 12, 0, 0, tzinfo=timezone(timedelta(hours=5)))

    assert ensure_utc(offset) == datetime(2026, 1, 1, 7, 0, 0, tzinfo=timezone.utc)


def test_ensure_utc_optional_passes_none_through() -> None:
    assert ensure_utc_optional(None) is None


def test_normalized_values_compare_with_an_aware_now() -> None:
    expires_at = ensure_utc(datetime(2030, 1, 1, 0, 0, 0))
    now = datetime(2026, 1, 1, tzinfo=timezone.utc)

    assert expires_at > now
