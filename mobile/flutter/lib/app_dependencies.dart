import 'features/auth/application/use_cases.dart';
import 'features/auth/data/auth_repository.dart';
import 'shared/auth/session_controller.dart';
import 'shared/auth/token_store.dart';
import 'shared/http/api_client.dart';
import 'shared/http/network_status.dart';
import 'shared/http/offline_queue.dart';
import 'shared/http/staleness_controller.dart';
import 'shared/i18n/locale_controller.dart';
import 'shared/notifications/push_notification_service.dart';

/// The composition root — the mobile equivalent of a backend's `Host` or
/// a web frontend's `app.config.ts` providers list: the one place allowed to
/// import across `shared/` **and** `features/` at once and wire concrete
/// implementations together. Neither `shared/` nor `features/auth/` may
/// import this file or one another the way this file imports both of them
/// — `test/architecture/boundary_test.dart` only scans `lib/shared/` and
/// `lib/features/`, deliberately leaving the composition root itself free
/// to depend on everything, the same exemption `docs/STRUCTURE.md` gives
/// every stack's own `Host`/`app` layer.
class AppDependencies {
  AppDependencies({
    NetworkStatus? networkStatus,
    OfflineQueue? offlineQueue,
    ApiClient? apiClient,
    TokenStore? tokenStore,
    LocaleController? localeController,
  })  : networkStatus = networkStatus ?? apiClient?.networkStatus ?? NetworkStatus(),
        offlineQueue = offlineQueue ?? apiClient?.offlineQueue ?? OfflineQueue(),
        tokenStore = tokenStore ?? SecureTokenStore(),
        localeController = localeController ?? LocaleController(),
        apiClient = apiClient ??
            ApiClient(
              networkStatus: networkStatus,
              offlineQueue: offlineQueue,
            ) {
    stalenessController = StalenessController(
      networkStatus: this.networkStatus,
      offlineQueue: this.offlineQueue,
    );
    session = SessionController(apiClient: this.apiClient, tokenStore: this.tokenStore);
    authRepository = AuthRepositoryImpl(this.apiClient.authApi, this.apiClient.client.dio);
    authUseCases = AuthUseCases(authRepository);
    pushNotifications = PushNotificationService(this.apiClient);
    session.addListener(_syncPushNotifications);
  }

  final NetworkStatus networkStatus;
  final OfflineQueue offlineQueue;
  late final StalenessController stalenessController;
  final ApiClient apiClient;
  final TokenStore tokenStore;
  final LocaleController localeController;
  late final SessionController session;
  late final AuthRepository authRepository;
  late final AuthUseCases authUseCases;
  late final PushNotificationService pushNotifications;

  /// Called once at startup — restores the language preference and attempts
  /// to hydrate a session from the persisted refresh token.
  Future<void> bootstrap() async {
    await Future.wait([localeController.restore(), session.bootstrap()]);
    _syncPushNotifications();
  }

  void _syncPushNotifications() {
    if (session.status == AuthStatus.authenticated) {
      pushNotifications.start();
    }
  }
}
