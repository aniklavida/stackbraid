import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Manages application theme mode (system, light, dark).
/// Defaults to [ThemeMode.system] so the app automatically respects the OS,
/// with dark theme acting as the StackBraid developer brand default.
class ThemeController extends ChangeNotifier {
  ThemeController({FlutterSecureStorage? storage})
      : _storage = storage ?? const FlutterSecureStorage();

  static const _themeModeKey = 'stackbraid.themeMode';

  final FlutterSecureStorage _storage;
  ThemeMode _themeMode = ThemeMode.system;

  ThemeMode get themeMode => _themeMode;

  Future<void> restore() async {
    final saved = await _storage.read(key: _themeModeKey);
    if (saved != null) {
      for (final mode in ThemeMode.values) {
        if (mode.name == saved) {
          _themeMode = mode;
          notifyListeners();
          return;
        }
      }
    }
  }

  Future<void> setThemeMode(ThemeMode mode) async {
    if (_themeMode == mode) return;
    _themeMode = mode;
    notifyListeners();
    await _storage.write(key: _themeModeKey, value: mode.name);
  }
}
