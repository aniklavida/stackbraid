import 'package:flutter/widgets.dart' show Locale;

/// A tiny hand-written date formatter instead of pulling in `package:intl`
/// for one "member since" label — this app already owns a localized-string
/// registry (`shared/i18n/translations.dart`) for exactly this kind of
/// small, per-locale text, and a second i18n mechanism just for dates would
/// be one more thing to keep in sync for no real benefit here.
const Map<String, List<String>> _monthNames = {
  'en': [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December',
  ],
  'es': [
    'enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio',
    'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre',
  ],
};

/// `"March 3, 2026"` (English) / `"3 de marzo de 2026"` (Spanish) — the two
/// locales' own conventional day/month order, not one format translated
/// verbatim into the other.
String formatLongDate(DateTime date, Locale locale) {
  final months = _monthNames[locale.languageCode] ?? _monthNames['en']!;
  final month = months[date.month - 1];
  return switch (locale.languageCode) {
    'es' => '${date.day} de $month de ${date.year}',
    _ => '$month ${date.day}, ${date.year}',
  };
}
