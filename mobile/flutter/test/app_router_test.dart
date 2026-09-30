import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:stackbraid_client/stackbraid_client.dart';
import 'package:stackbraid_mobile/app.dart';
import 'package:stackbraid_mobile/app_dependencies.dart';
import 'package:stackbraid_mobile/features/auth/presentation/i18n/auth_strings.dart';
import 'package:stackbraid_mobile/shared/auth/session_controller.dart';
import 'package:stackbraid_mobile/shared/auth/token_store.dart';
import 'package:stackbraid_mobile/shared/http/network_status.dart';
import 'package:stackbraid_mobile/shared/http/offline_queue.dart';
import 'package:stackbraid_mobile/shared/i18n/app_localizations.dart';
import 'package:stackbraid_mobile/shared/i18n/common_strings.dart';
import 'package:stackbraid_mobile/shared/i18n/translations.dart';

class _FakeTokenStore implements TokenStore {
  String? token;
  @override
  Future<void> clear() async => token = null;
  @override
  Future<String?> readRefreshToken() async => token;
  @override
  Future<void> saveRefreshToken(String refreshToken) async => token = refreshToken;
}

void main() {
  group('AppRouter and SignedInShell navigation', () {
    testWidgets('unauthenticated access to protected routes guards to login', (tester) async {
      final dependencies = AppDependencies(tokenStore: _FakeTokenStore());
      dependencies.session.status = AuthStatus.anonymous;

      await tester.pumpWidget(App(dependencies: dependencies));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('login-submit')), findsOneWidget);
    });

    testWidgets('authenticated user sees SignedInShell with navigation and staleness indicator', (tester) async {
      tester.view.physicalSize = const Size(800, 1200);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
      });

      final networkStatus = NetworkStatus(initialOnline: false);
      final offlineQueue = OfflineQueue();
      final dependencies = AppDependencies(
        networkStatus: networkStatus,
        offlineQueue: offlineQueue,
        tokenStore: _FakeTokenStore(),
      );

      dependencies.session.status = AuthStatus.authenticated;
      dependencies.session.user = User(
        id: 'user_1',
        email: 'dev@stackbraid.local',
        displayName: 'Dev User',
        status: UserStatus.active,
        createdAt: DateTime.utc(2026, 1, 1),
        updatedAt: DateTime.utc(2026, 1, 1),
        deletedAt: null,
        roles: [],
      );

      final translations = mergeTranslations([commonStrings, authStrings]);
      final localizations = AppLocalizations(const Locale('en'), translations);

      await tester.pumpWidget(
        I18nScope(
          localizations: localizations,
          child: MaterialApp(
            home: SignedInShell(dependencies: dependencies),
          ),
        ),
      );
      await tester.pumpAndSettle();

      // Signed-in shell displays staleness indicator because networkStatus is offline
      expect(find.byKey(const Key('staleness-indicator')), findsOneWidget);
      expect(find.textContaining('Offline — showing cached data'), findsOneWidget);

      // Bottom navigation destinations are present
      expect(find.byKey(const Key('nav-home')), findsOneWidget);
      expect(find.byKey(const Key('nav-profile')), findsOneWidget);

      // Default landing is ProfileScreen
      expect(find.text('Dev User'), findsOneWidget);
      expect(find.text('dev@stackbraid.local'), findsOneWidget);
      expect(find.byKey(const Key('profile-sign-out')), findsOneWidget);

      // Navigate to Home tab
      await tester.tap(find.byKey(const Key('nav-home')));
      await tester.pumpAndSettle();

      expect(find.textContaining('Welcome, Dev User'), findsOneWidget);
      expect(find.text('Ready for features'), findsOneWidget);

      // Navigate back to Profile tab
      await tester.tap(find.byKey(const Key('nav-profile')));
      await tester.pumpAndSettle();

      expect(find.text('Dev User'), findsOneWidget);
      expect(find.byKey(const Key('profile-sign-out')), findsOneWidget);
    });

    testWidgets('admin routes on mobile are blocked with desktop job notice', (tester) async {
      final dependencies = AppDependencies(tokenStore: _FakeTokenStore());
      final router = AppRouter(dependencies: dependencies);

      final route = router.onGenerateRoute(const RouteSettings(name: '/admin/users'));

      await tester.pumpWidget(
        MaterialApp(
          home: Builder(
            builder: (context) {
              return ElevatedButton(
                key: const Key('open-admin'),
                onPressed: () => Navigator.of(context).push(route),
                child: const Text('Go'),
              );
            },
          ),
        ),
      );

      await tester.tap(find.byKey(const Key('open-admin')));
      await tester.pumpAndSettle();

      expect(
        find.text('Administration is a desktop job; no admin surface on mobile.'),
        findsOneWidget,
      );
    });
  });
}
