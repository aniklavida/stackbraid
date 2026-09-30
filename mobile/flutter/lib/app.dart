import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:stackbraid_client/stackbraid_client.dart';

import 'app_dependencies.dart';
import 'features/auth/presentation/i18n/auth_strings.dart';
import 'features/auth/presentation/login_screen.dart';
import 'features/auth/presentation/profile_screen.dart';
import 'features/auth/presentation/register_screen.dart';
import 'shared/auth/session_controller.dart';
import 'shared/i18n/app_localizations.dart';
import 'shared/i18n/common_strings.dart';
import 'shared/i18n/locale_controller.dart';
import 'shared/i18n/translations.dart';
import 'shared/theme/app_theme.dart';
import 'shared/theme/tokens.dart';
import 'shared/widgets/staleness_indicator.dart';

final Translations _allTranslations = mergeTranslations([commonStrings, authStrings]);

/// Lets `integration_test/identity_flow_test.dart` capture a screenshot via
/// `RenderRepaintBoundary.toImage()` — the macOS desktop target has no
/// native implementation behind `IntegrationTestWidgetsFlutterBinding
/// .takeScreenshot()` (`MissingPluginException: captureScreenshot`, tried
/// first), so this is the fallback that works on every target.
final GlobalKey screenshotBoundaryKey = GlobalKey();

abstract final class AppRoutes {
  static const String initial = '/';
  static const String login = '/login';
  static const String register = '/register';
  static const String home = '/home';
  static const String profile = '/profile';
}

/// The mobile application router.
///
/// Provides named routing, session-aware route guards, the signed-in shell
/// for authenticated screens, and enforces the rule that mobile carries
/// no admin surface (`docs/STRUCTURE.md`: administration is a desktop job).
class AppRouter {
  AppRouter({required this.dependencies});

  final AppDependencies dependencies;
  final GlobalKey<NavigatorState> navigatorKey = GlobalKey<NavigatorState>();

  Route<dynamic> onGenerateRoute(RouteSettings settings) {
    final isAuthenticated = dependencies.session.status == AuthStatus.authenticated;

    // Explicitly reject admin routes on mobile
    if (settings.name != null && settings.name!.startsWith('/admin')) {
      return MaterialPageRoute(
        settings: settings,
        builder: (_) => Scaffold(
          appBar: AppBar(title: const Text('Admin')),
          body: Center(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 400),
                child: Card(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: const [
                        Icon(Icons.desktop_windows_outlined, size: 40),
                        SizedBox(height: 16),
                        Text(
                          'Administration is a desktop job; no admin surface on mobile.',
                          textAlign: TextAlign.center,
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
      );
    }

    switch (settings.name) {
      case AppRoutes.login:
        if (isAuthenticated) {
          return MaterialPageRoute(
            settings: settings,
            builder: (_) => SignedInShell(dependencies: dependencies),
          );
        }
        return MaterialPageRoute(
          settings: settings,
          builder: (context) => LoginScreen(
            useCases: dependencies.authUseCases,
            session: dependencies.session,
            onCreateAccount: () => Navigator.of(context).pushNamed(AppRoutes.register),
          ),
        );

      case AppRoutes.register:
        if (isAuthenticated) {
          return MaterialPageRoute(
            settings: settings,
            builder: (_) => SignedInShell(dependencies: dependencies),
          );
        }
        return MaterialPageRoute(
          settings: settings,
          builder: (_) => RegisterScreen(
            useCases: dependencies.authUseCases,
            session: dependencies.session,
          ),
        );

      case AppRoutes.home:
        if (!isAuthenticated) {
          return MaterialPageRoute(
            settings: const RouteSettings(name: AppRoutes.login),
            builder: (context) => LoginScreen(
              useCases: dependencies.authUseCases,
              session: dependencies.session,
              onCreateAccount: () => Navigator.of(context).pushNamed(AppRoutes.register),
            ),
          );
        }
        return MaterialPageRoute(
          settings: settings,
          builder: (_) => SignedInShell(dependencies: dependencies, initialTab: 0),
        );

      case AppRoutes.profile:
        if (!isAuthenticated) {
          return MaterialPageRoute(
            settings: const RouteSettings(name: AppRoutes.login),
            builder: (context) => LoginScreen(
              useCases: dependencies.authUseCases,
              session: dependencies.session,
              onCreateAccount: () => Navigator.of(context).pushNamed(AppRoutes.register),
            ),
          );
        }
        return MaterialPageRoute(
          settings: settings,
          builder: (_) => SignedInShell(dependencies: dependencies, initialTab: 1),
        );

      case AppRoutes.initial:
      default:
        return MaterialPageRoute(
          settings: settings,
          builder: (_) => _AuthGate(dependencies: dependencies),
        );
    }
  }
}

class App extends StatefulWidget {
  const App({super.key, required this.dependencies});

  final AppDependencies dependencies;

  @override
  State<App> createState() => _AppState();
}

class _AppState extends State<App> {
  late final AppRouter _router;

  @override
  void initState() {
    super.initState();
    _router = AppRouter(dependencies: widget.dependencies);
    widget.dependencies.bootstrap();
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: Listenable.merge([
        widget.dependencies.localeController,
        widget.dependencies.themeController,
      ]),
      builder: (context, _) {
        final localizations = AppLocalizations(widget.dependencies.localeController.locale, _allTranslations);
        return I18nScope(
          localizations: localizations,
          child: RepaintBoundary(
            key: screenshotBoundaryKey,
            child: MaterialApp(
              title: localizations.t('common.appTitle'),
              debugShowCheckedModeBanner: false,
              theme: AppTheme.light,
              darkTheme: AppTheme.dark,
              themeMode: widget.dependencies.themeController.themeMode,
              supportedLocales: supportedLocales,
              locale: widget.dependencies.localeController.locale,
              localizationsDelegates: const [
                GlobalMaterialLocalizations.delegate,
                GlobalWidgetsLocalizations.delegate,
                GlobalCupertinoLocalizations.delegate,
              ],
              navigatorKey: _router.navigatorKey,
              onGenerateRoute: _router.onGenerateRoute,
              home: _AuthGate(dependencies: widget.dependencies),
            ),
          ),
        );
      },
    );
  }
}

/// Shows the right screen for the current [AuthStatus] — the mobile
/// equivalent of the web frontends' `AuthProvider`-driven route guard, minus
/// any admin section: `docs/STRUCTURE.md` is explicit that mobile carries no
/// web/admin split, so this app has exactly three states, not a role-gated
/// fourth one.
class _AuthGate extends StatelessWidget {
  const _AuthGate({required this.dependencies});

  final AppDependencies dependencies;

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: dependencies.session,
      builder: (context, _) {
        switch (dependencies.session.status) {
          case AuthStatus.loading:
            return const Scaffold(
              body: Center(
                child: SizedBox(
                  width: 24,
                  height: 24,
                  child: CircularProgressIndicator(strokeWidth: 2.5),
                ),
              ),
            );
          case AuthStatus.anonymous:
            return LoginScreen(
              useCases: dependencies.authUseCases,
              session: dependencies.session,
              onCreateAccount: () {
                Navigator.of(context).push(
                  MaterialPageRoute(
                    settings: const RouteSettings(name: AppRoutes.register),
                    builder: (_) => RegisterScreen(
                      useCases: dependencies.authUseCases,
                      session: dependencies.session,
                    ),
                  ),
                );
              },
            );
          case AuthStatus.authenticated:
            return SignedInShell(
              dependencies: dependencies,
              initialTab: 1, // Lands directly on profile for the identity flow
            );
        }
      },
    );
  }
}

/// The signed-in shell wrapping authenticated screens.
///
/// Provides top-level staleness indication (never presenting stale data as current),
/// bottom navigation bar between available features, and readiness for future domain screens.
class SignedInShell extends StatefulWidget {
  const SignedInShell({
    super.key,
    required this.dependencies,
    this.initialTab = 1,
  });

  final AppDependencies dependencies;
  final int initialTab;

  @override
  State<SignedInShell> createState() => _SignedInShellState();
}

class _SignedInShellState extends State<SignedInShell> {
  late int _selectedTab;

  @override
  void initState() {
    super.initState();
    _selectedTab = widget.initialTab;
  }

  @override
  Widget build(BuildContext context) {
    final destinations = [
      NavigationDestination(
        key: const Key('nav-home'),
        icon: const Icon(Icons.home_outlined),
        selectedIcon: const Icon(Icons.home),
        label: context.t('common.navHome'),
      ),
      NavigationDestination(
        key: const Key('nav-profile'),
        icon: const Icon(Icons.person_outline),
        selectedIcon: const Icon(Icons.person),
        label: context.t('common.navProfile'),
      ),
    ];

    final screens = [
      HomeScreen(
        user: widget.dependencies.session.user,
        onGoToProfile: () => setState(() => _selectedTab = 1),
      ),
      ProfileScreen(
        useCases: widget.dependencies.authUseCases,
        session: widget.dependencies.session,
        localeController: widget.dependencies.localeController,
        themeController: widget.dependencies.themeController,
      ),
    ];

    return Scaffold(
      body: Column(
        children: [
          StalenessIndicator(controller: widget.dependencies.stalenessController),
          Expanded(
            child: IndexedStack(
              index: _selectedTab,
              children: screens,
            ),
          ),
        ],
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _selectedTab,
        onDestinationSelected: (index) => setState(() => _selectedTab = index),
        destinations: destinations,
      ),
    );
  }
}

/// Home screen for the signed-in shell — ready for more domain features.
class HomeScreen extends StatelessWidget {
  const HomeScreen({
    super.key,
    this.user,
    required this.onGoToProfile,
  });

  final User? user;
  final VoidCallback onGoToProfile;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final width = MediaQuery.sizeOf(context).width;
    final horizontalPadding = width >= 430 ? AppTokens.screenMarginWide : AppTokens.screenMargin;
    final displayName = user?.displayName ?? '';

    return Scaffold(
      appBar: AppBar(
        title: Text(
          context.t('common.appTitle'),
          style: TextStyle(
            fontFamily: AppTokens.fontDisplay,
            fontSize: 20,
            fontWeight: FontWeight.w600,
            letterSpacing: -0.4,
            color: theme.colorScheme.onSurface,
          ),
        ),
      ),
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 440),
            child: ListView(
              padding: EdgeInsets.symmetric(horizontal: horizontalPadding, vertical: 20),
              children: [
                Text(
                  '${context.t('common.homeWelcome')}${displayName.isNotEmpty ? ', $displayName' : ''}',
                  style: theme.textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.w600),
                ),
                const SizedBox(height: 6),
                Text(
                  context.t('common.homeReadySubtitle'),
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                ),
                const SizedBox(height: 24),
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(18),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Icon(
                              Icons.dashboard_customize_outlined,
                              size: 20,
                              color: theme.colorScheme.primary,
                            ),
                            const SizedBox(width: 8),
                            Text(
                              context.t('common.homeReadyTitle'),
                              style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600),
                            ),
                          ],
                        ),
                        const SizedBox(height: 8),
                        Text(
                          context.t('common.homeReadySubtitle'),
                          style: theme.textTheme.bodyMedium?.copyWith(
                            color: theme.colorScheme.onSurfaceVariant,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 16),
                Card(
                  child: ListTile(
                    leading: const Icon(Icons.person_outline, size: 22),
                    title: Text(context.t('auth.profileTitle')),
                    subtitle: Text(
                      user?.email ?? '',
                      style: TextStyle(
                        fontFamily: AppTokens.fontMono,
                        fontSize: 12.5,
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                    trailing: const Icon(Icons.chevron_right, size: 20),
                    onTap: onGoToProfile,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
