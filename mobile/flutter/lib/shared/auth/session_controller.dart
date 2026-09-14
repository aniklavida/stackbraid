import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:stackbraid_client/stackbraid_client.dart';

import '../http/api_client.dart';
import 'session_port.dart';
import 'token_store.dart';

enum AuthStatus { loading, authenticated, anonymous }

/// The mobile equivalent of the web frontends' `AuthProvider` — hydrates a
/// session from the persisted refresh token on first launch (their
/// equivalent of "hydrate from the httpOnly cookie on first load"), renews
/// the access token silently ~30s before it expires, and publishes the
/// current [AuthStatus]/[user] to anything listening (the router deciding
/// which screen to show, the profile screen, the app shell's sign-out
/// button).
///
/// Talks to `AuthApi` (the generated client) directly for refresh and
/// "who am I" — the same split a web frontend's `AuthProvider`/session
/// service makes between session housekeeping (shared, generic) and the
/// feature's own register/login/logout calls
/// (`features/auth/data/auth_repository.dart`).
class SessionController extends ChangeNotifier implements SessionPort {
  SessionController({required ApiClient apiClient, required TokenStore tokenStore})
      : _apiClient = apiClient,
        _tokenStore = tokenStore;

  final ApiClient _apiClient;
  final TokenStore _tokenStore;
  Timer? _refreshTimer;
  bool _bootstrapped = false;

  AuthStatus status = AuthStatus.loading;
  User? user;
  String? _accessToken;
  DateTime? _expiresAt;

  String? get accessToken => _accessToken;

  bool hasPermission(String permission) =>
      user?.roles.any((role) => role.permissions.contains(permission)) ?? false;

  /// The one place outside this controller that ever touches the persisted
  /// refresh token directly — sign-out needs to send it to
  /// `POST /v1/auth/logout` so the backend revokes that exact token, not
  /// just this device's in-memory idea of the session.
  Future<String?> readPersistedRefreshToken() => _tokenStore.readRefreshToken();

  /// Call once, at app start. A missing or rejected refresh token ends in
  /// the ordinary signed-out state, silently — not an error a user sees.
  Future<void> bootstrap() async {
    if (_bootstrapped) return;
    _bootstrapped = true;
    final refreshToken = await _tokenStore.readRefreshToken();
    if (refreshToken == null) {
      status = AuthStatus.anonymous;
      notifyListeners();
      return;
    }
    try {
      final response = await _apiClient.authApi.refreshToken(
        refreshRequest: RefreshRequest(refreshToken: refreshToken),
      );
      await _adopt(response.data!);
    } catch (_) {
      await clearSession();
    }
  }

  @override
  Future<User> adoptSession(TokenPair tokens) => _adopt(tokens);

  Future<User> _adopt(TokenPair tokens) async {
    final userResponse = await _apiClient.authApi.getCurrentUser(
      headers: {'Authorization': 'Bearer ${tokens.accessToken}'},
    );
    final resolvedUser = userResponse.data!;

    _accessToken = tokens.accessToken;
    _expiresAt = tokens.expiresAt;
    user = resolvedUser;
    status = AuthStatus.authenticated;
    _apiClient.setAccessToken(tokens.accessToken);
    await _tokenStore.saveRefreshToken(tokens.refreshToken);
    _scheduleRefresh();
    notifyListeners();
    return resolvedUser;
  }

  @override
  Future<void> clearSession() async {
    _refreshTimer?.cancel();
    _refreshTimer = null;
    _accessToken = null;
    _expiresAt = null;
    user = null;
    status = AuthStatus.anonymous;
    _apiClient.setAccessToken(null);
    await _tokenStore.clear();
    notifyListeners();
  }

  void _scheduleRefresh() {
    _refreshTimer?.cancel();
    final expiresAt = _expiresAt;
    if (expiresAt == null) return;
    // Silently renew ~30s before the access token's own expiry rather than
    // waiting for a request to fail — matches the web frontends' timer.
    final delay = expiresAt.difference(DateTime.now()) - const Duration(seconds: 30);
    _refreshTimer = Timer(delay.isNegative ? const Duration(seconds: 1) : delay, _silentRefresh);
  }

  Future<void> _silentRefresh() async {
    final refreshToken = await _tokenStore.readRefreshToken();
    if (refreshToken == null) {
      await clearSession();
      return;
    }
    try {
      final response = await _apiClient.authApi.refreshToken(
        refreshRequest: RefreshRequest(refreshToken: refreshToken),
      );
      await _adopt(response.data!);
    } catch (_) {
      await clearSession();
    }
  }

  @override
  void dispose() {
    _refreshTimer?.cancel();
    super.dispose();
  }
}
