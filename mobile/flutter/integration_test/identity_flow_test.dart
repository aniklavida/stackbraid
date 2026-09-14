// The same Identity happy path the shared Playwright script
// (e2e/identity-flow.ts) proves for both web frontends, run here as a real
// Flutter integration test against a real running backend — changing only
// --dart-define=API_BASE_URL between backend runs, the same contract-first
// proof `contract/conformance/` already applies to every backend on every
// provider: if every backend passes the same suite, every generated client
// is guaranteed to work against every backend.
//
// Run target: the macOS desktop app (`flutter test integration_test -d
// macos`) — no iOS/Android simulator or emulator is installed or downloaded
// on this machine (see mobile/flutter/README.md, "What was verified").
//
// "Session survives an app restart" is proven as honestly as a single test
// binary can: a brand-new `AppDependencies` (a fresh `Dio`, a fresh
// in-memory access token, a fresh `SessionController`) is built and pumped
// as a second, independent widget tree, reading the SAME persisted refresh
// token back out of the real platform secure storage the first tree wrote
// to. What this does not prove is the OS actually killing and relaunching
// the process — `flutter test integration_test` cannot do that from inside
// one run. The token persistence itself (flutter_secure_storage, backed by
// the real macOS Keychain on this run target) is exactly what would make a
// genuine process relaunch work the same way.
//
// No mobile admin UI exists (`docs/STRUCTURE.md`: "no web/admin split on
// mobile — administration is a desktop job"), so the second test below
// proves "list users and change a role" at the level this app actually
// operates on the contract: the generated Dart client directly, using the
// seeded administrator, exactly the way this app's own data layer would if
// a mobile admin surface is ever added here.
import 'dart:io';
import 'dart:ui' as ui;

import 'package:dio/dio.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:stackbraid_client/stackbraid_client.dart';
import 'package:stackbraid_mobile/app.dart';
import 'package:stackbraid_mobile/app_dependencies.dart';
import 'package:stackbraid_mobile/shared/auth/token_store.dart';
import 'package:stackbraid_mobile/shared/config/app_config.dart';

const seededAdminEmail = 'admin@stackbraid.local';
const seededAdminPassword = 'ChangeMe!123';

// Never a path inside this repository — screenshots are run evidence, not
// a repo artifact, the same rule the shared Playwright spec
// (e2e/identity-flow.ts) follows for the two web frontends' own runs.
const _screenshotDir = String.fromEnvironment('SCREENSHOT_DIR');
const _backendLabel = String.fromEnvironment('BACKEND_LABEL', defaultValue: 'backend');

/// A bounded stand-in for `pumpAndSettle`. `pumpAndSettle` refuses to return
/// while ANY indeterminate animation is still ticking anywhere in the tree
/// (a blinking text-field cursor left over from a just-submitted form is
/// the classic cause) and will wait its full internal timeout — measured
/// here at the default ten minutes — rather than return once the screen
/// this test actually cares about has appeared. Pumping a fixed, generous
/// number of real frames gets the same practical effect (network calls and
/// the resulting rebuild have long since finished) without that failure
/// mode.
Future<void> settle(WidgetTester tester, {int times = 30, Duration step = const Duration(milliseconds: 200)}) async {
  for (var i = 0; i < times; i++) {
    await tester.pump(step);
  }
}

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  // `IntegrationTestWidgetsFlutterBinding.takeScreenshot()` has no native
  // implementation on the macOS desktop target
  // (`MissingPluginException: captureScreenshot`, confirmed by trying it) —
  // capturing the `RepaintBoundary` `lib/app.dart` wraps around the whole
  // app (`screenshotBoundaryKey`) directly works on every target instead.
  Future<void> shot(WidgetTester tester, String name) async {
    if (_screenshotDir.isEmpty) return;
    await tester.runAsync(() async {
      final boundary = screenshotBoundaryKey.currentContext!.findRenderObject() as RenderRepaintBoundary;
      final image = await boundary.toImage(pixelRatio: 2.0);
      final byteData = await image.toByteData(format: ui.ImageByteFormat.png);
      final file = File('$_screenshotDir/flutter-$_backendLabel-$name.png');
      await file.parent.create(recursive: true);
      await file.writeAsBytes(byteData!.buffer.asUint8List());
    });
  }

  setUp(() async {
    // Every run starts from a clean device: no leftover refresh token from
    // a previous run against a since-torn-down throwaway Postgres.
    await SecureTokenStore().clear();
  });

  testWidgets(
    'register, sign in, the session survives a fresh app instance, profile, sign out, sign in again',
    (tester) async {
      final stamp = DateTime.now().millisecondsSinceEpoch;
      final email = 'sadia.islam+$stamp@example.com';
      const password = 'correct-horse-battery-staple';
      const displayName = 'Sadia Islam';

      // --- Fresh launch, signed out -----------------------------------
      var dependencies = AppDependencies();
      await tester.pumpWidget(App(key: UniqueKey(), dependencies: dependencies));
      await settle(tester);
      expect(find.byKey(const Key('login-submit')), findsOneWidget);
      await shot(tester, '01-login-signed-out');

      // --- Register ------------------------------------------------------
      await tester.tap(find.byKey(const Key('login-go-register')));
      await settle(tester);
      await shot(tester, '02-register-form-empty');

      await tester.enterText(find.byKey(const Key('register-display-name')), displayName);
      await tester.enterText(find.byKey(const Key('register-email')), email);
      await tester.enterText(find.byKey(const Key('register-password')), password);
      await shot(tester, '03-register-form-filled');
      await tester.tap(find.byKey(const Key('register-submit')));
      await settle(tester);

      // Registration alone issues no session — this app signs the new
      // account in right after, landing on its profile (same rule the web
      // frontends' own identity-flow spec proves).
      expect(find.text(displayName), findsOneWidget);
      expect(find.text(email), findsOneWidget);
      await shot(tester, '04-profile-after-register');

      final refreshTokenAfterRegister = await dependencies.tokenStore.readRefreshToken();
      expect(refreshTokenAfterRegister, isNotNull, reason: 'the refresh token must be persisted after signing in');

      // --- The session survives a fresh app instance ----------------------
      // A brand-new AppDependencies (fresh Dio, fresh in-memory access
      // token, fresh SessionController) reading back the SAME persisted
      // refresh token — see the file header for exactly what this does and
      // does not prove.
      dependencies = AppDependencies();
      await tester.pumpWidget(App(key: UniqueKey(), dependencies: dependencies));
      await settle(tester);
      expect(
        find.text(displayName),
        findsOneWidget,
        reason: 'a fresh app instance must hydrate the session from the persisted refresh token, not show login',
      );
      await shot(tester, '05-profile-after-fresh-app-instance');

      // --- Sign out --------------------------------------------------------------
      await tester.tap(find.byKey(const Key('profile-sign-out')));
      await settle(tester);
      expect(find.byKey(const Key('login-submit')), findsOneWidget);
      expect(await dependencies.tokenStore.readRefreshToken(), isNull);
      await shot(tester, '06-login-after-sign-out');

      // --- Sign back in with the same credentials ---------------------------------
      await tester.enterText(find.byKey(const Key('login-email')), email);
      await tester.enterText(find.byKey(const Key('login-password')), password);
      await tester.tap(find.byKey(const Key('login-submit')));
      await settle(tester);
      expect(find.text(displayName), findsOneWidget);
      await shot(tester, '07-profile-after-signing-in-again');
    },
  );

  testWidgets(
    'the seeded administrator can list users and assign a role through the generated client '
    '(no mobile admin UI — docs/STRUCTURE.md: administration is a desktop job)',
    (tester) async {
      final dio = Dio(BaseOptions(baseUrl: AppConfig.instance.apiBaseUrl));
      final authApi = AuthApi(dio);
      final usersApi = UsersApi(dio);
      final rolesApi = RolesApi(dio);

      final stamp = DateTime.now().millisecondsSinceEpoch;
      final targetEmail = 'nazia.chowdhury+$stamp@example.com';

      // A real account for the role assignment to land on — created
      // through the same register/login endpoints the app itself uses,
      // just without going through the UI for this API-level proof.
      await authApi.registerUser(
        registerRequest: RegisterRequest(
          email: targetEmail,
          password: 'correct-horse-battery-staple',
          displayName: 'Nazia Chowdhury',
        ),
      );

      final adminLogin = await authApi.login(
        loginRequest: LoginRequest(email: seededAdminEmail, password: seededAdminPassword),
      );
      final adminAccessToken = adminLogin.data!.accessToken;
      final authHeader = {'Authorization': 'Bearer $adminAccessToken'};

      final usersPage = await usersApi.listUsers(pageSize: 50, headers: authHeader);
      final targetUser = usersPage.data!.items.firstWhere((u) => u.email == targetEmail);
      expect(targetUser.roles, isEmpty);

      final roles = await rolesApi.listRoles(headers: authHeader);
      final userRole = roles.data!.firstWhere((r) => r.name == 'user');

      final updated = await rolesApi.assignRole(
        userId: targetUser.id,
        assignRoleRequest: AssignRoleRequest(roleId: userRole.id),
        headers: authHeader,
      );
      expect(updated.data!.roles.map((r) => r.name), contains('user'));
    },
  );
}
