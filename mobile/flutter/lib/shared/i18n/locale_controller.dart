import 'package:flutter/widgets.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

const List<Locale> supportedLocales = [Locale('en'), Locale('es')];

/// The app's current [Locale] plus a manual override the user can pick from
/// the profile screen — persisted the same way the refresh token is (secure
/// storage; nothing sensitive about a language choice, but it is the one
/// small-persistence primitive already audited for this app, so reusing it
/// avoids a second dependency for the same job).
class LocaleController extends ChangeNotifier {
  LocaleController({FlutterSecureStorage? storage})
      : _storage = storage ?? const FlutterSecureStorage();

  static const _localeKey = 'stackbraid.locale';

  final FlutterSecureStorage _storage;
  Locale _locale = supportedLocales.first;

  Locale get locale => _locale;

  Future<void> restore() async {
    final saved = await _storage.read(key: _localeKey);
    if (saved != null) {
      final match = supportedLocales.where((l) => l.languageCode == saved);
      if (match.isNotEmpty) {
        _locale = match.first;
        notifyListeners();
      }
    }
  }

  Future<void> setLocale(Locale locale) async {
    if (!supportedLocales.contains(locale)) return;
    _locale = locale;
    notifyListeners();
    await _storage.write(key: _localeKey, value: locale.languageCode);
  }
}
