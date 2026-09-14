import 'package:flutter/widgets.dart';

import 'translations.dart';

/// Looks up a namespaced key (`"auth.emailLabel"`) in the current locale,
/// falling back to English and then to the raw key — a missing translation
/// degrades to readable English rather than crashing a screen.
class AppLocalizations {
  const AppLocalizations(this.locale, this.translations);

  final Locale locale;
  final Translations translations;

  String t(String key) {
    return translations[locale.languageCode]?[key] ?? translations['en']?[key] ?? key;
  }

  static AppLocalizations of(BuildContext context) {
    final scope = context.dependOnInheritedWidgetOfExactType<I18nScope>();
    assert(scope != null, 'AppLocalizations.of() called with no I18nScope ancestor.');
    return scope!.localizations;
  }
}

class I18nScope extends InheritedWidget {
  const I18nScope({super.key, required this.localizations, required super.child});

  final AppLocalizations localizations;

  @override
  bool updateShouldNotify(I18nScope oldWidget) =>
      oldWidget.localizations.locale != localizations.locale;
}

extension AppLocalizationsContext on BuildContext {
  /// `context.t('auth.emailLabel')` — the lookup every presentation widget
  /// uses instead of a hard-coded English string.
  String t(String key) => AppLocalizations.of(this).t(key);
}
