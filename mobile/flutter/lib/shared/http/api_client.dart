import 'package:flutter/foundation.dart' show kDebugMode;
import 'package:stackbraid_client/stackbraid_client.dart';

import '../config/app_config.dart';
import 'log_redaction.dart';

/// One `Dio`/generated-client instance for the whole app, built once at
/// startup and handed to every feature's data layer — the only place a
/// base URL or an HTTP interceptor is configured. Every feature talks to
/// the backend exclusively through `clients/dart` (`stackbraid_client`),
/// used unchanged: no hand-written HTTP calls, no copied models.
class ApiClient {
  ApiClient({AppConfig config = AppConfig.instance})
      : client = StackbraidClient(basePathOverride: config.apiBaseUrl) {
    // `StackbraidClient`'s own constructor only installs its default
    // security interceptors (including the `BearerAuthInterceptor` every
    // bearer-secured operation — `logout`, `listUsers`, `assignRole` and
    // more — depends on to actually attach the header `setAccessToken`
    // below registers) when its `interceptors` constructor parameter is
    // left null. Passing a custom list there — even one that only adds a
    // logger — replaces those defaults outright rather than extending
    // them, so the logger is added afterwards, directly on `client.dio`,
    // instead.
    if (kDebugMode) {
      client.dio.interceptors.add(RedactingLogInterceptor());
    }
  }

  final StackbraidClient client;

  late final AuthApi authApi = AuthApi(client.dio);
  late final UsersApi usersApi = UsersApi(client.dio);
  late final RolesApi rolesApi = RolesApi(client.dio);

  /// Attaches the current access token to every subsequent request this
  /// client makes. Called only after a token has been proven good by a
  /// successful `getCurrentUser` call — see
  /// `features/auth/application/use_cases.dart` and
  /// `shared/auth/session_controller.dart`.
  void setAccessToken(String? accessToken) {
    if (accessToken == null) {
      client.removeBearerAuth('bearerAuth');
    } else {
      client.setBearerAuth('bearerAuth', accessToken);
    }
  }
}
