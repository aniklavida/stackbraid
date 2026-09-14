import 'features/auth/application/use_cases.dart';
import 'features/auth/data/auth_repository.dart';
import 'shared/auth/session_controller.dart';
import 'shared/auth/token_store.dart';
import 'shared/http/api_client.dart';
import 'shared/i18n/locale_controller.dart';

/// The composition root — the mobile equivalent of a backend's `Host` or
/// Angular's `app.config.ts` providers list: the one place allowed to
/// import across `shared/` **and** `features/` at once and wire concrete
/// implementations together. Neither `shared/` nor `features/auth/` may
/// import this file or one another the way this file imports both of them
/// — `test/architecture/boundary_test.dart` only scans `lib/shared/` and
/// `lib/features/`, deliberately leaving the composition root itself free
/// to depend on everything, the same exemption `docs/STRUCTURE.md` gives
/// every stack's own `Host`/`app` layer.
class AppDependencies {
  AppDependencies()
      : apiClient = ApiClient(),
        tokenStore = SecureTokenStore(),
        localeController = LocaleController() {
    session = SessionController(apiClient: apiClient, tokenStore: tokenStore);
    authRepository = AuthRepositoryImpl(apiClient.authApi, apiClient.client.dio);
    authUseCases = AuthUseCases(authRepository);
  }

  final ApiClient apiClient;
  final TokenStore tokenStore;
  final LocaleController localeController;
  late final SessionController session;
  late final AuthRepository authRepository;
  late final AuthUseCases authUseCases;

  /// Called once at startup — restores the language preference and attempts
  /// to hydrate a session from the persisted refresh token.
  Future<void> bootstrap() async {
    await Future.wait([localeController.restore(), session.bootstrap()]);
  }
}
