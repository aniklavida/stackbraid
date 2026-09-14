import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// The mobile equivalent of the token-storage rule `docs/SPEC.md` states for
/// every StackBraid client: an access token that never touches disk, plus a
/// refresh token kept somewhere ordinary application code cannot casually
/// read it back out of — the web frontends get that from a browser's
/// httpOnly cookie jar. A native app has no browser and no such cookie, so
/// **the refresh token is persisted through platform secure storage instead**
/// (iOS/macOS Keychain, Android Keystore-backed `EncryptedSharedPreferences`
/// — never plain `SharedPreferences`/`UserDefaults`, never a plain file).
/// The access token stays in-memory only, in
/// `shared/auth/session_controller.dart` — matching the in-memory-access-token
/// half of that same rule exactly.
abstract class TokenStore {
  Future<String?> readRefreshToken();
  Future<void> saveRefreshToken(String token);
  Future<void> clear();
}

class SecureTokenStore implements TokenStore {
  SecureTokenStore({FlutterSecureStorage? storage})
      : _storage = storage ??
            const FlutterSecureStorage(
              aOptions: AndroidOptions(encryptedSharedPreferences: true),
              // The newer "data protection" Keychain
              // (`kSecUseDataProtectionKeychain`, this plugin's own default)
              // ties access to the app's code-signing identity even outside
              // the App Sandbox, and refuses every call with
              // "-34018, A required entitlement isn't present" for an
              // ad hoc-signed local build with no Apple Developer Team
              // configured. The traditional per-user login Keychain (this
              // setting) has no such requirement and is what an ad hoc
              // local build — and this app's own verified macOS run — can
              // actually use; a real, properly signed distribution build
              // may opt back into the newer keychain by dropping this
              // override once a Developer Team is configured (see
              // macos/Runner/Configs/LocalSigning.xcconfig.example).
              mOptions: MacOsOptions(useDataProtectionKeyChain: false),
            );

  static const _refreshTokenKey = 'stackbraid.refreshToken';

  final FlutterSecureStorage _storage;

  @override
  Future<String?> readRefreshToken() => _storage.read(key: _refreshTokenKey);

  @override
  Future<void> saveRefreshToken(String token) =>
      _storage.write(key: _refreshTokenKey, value: token);

  @override
  Future<void> clear() => _storage.delete(key: _refreshTokenKey);
}
