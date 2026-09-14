import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';

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

final Translations _allTranslations = mergeTranslations([commonStrings, authStrings]);

/// Lets `integration_test/identity_flow_test.dart` capture a screenshot via
/// `RenderRepaintBoundary.toImage()` — the macOS desktop target has no
/// native implementation behind `IntegrationTestWidgetsFlutterBinding
/// .takeScreenshot()` (`MissingPluginException: captureScreenshot`, tried
/// first), so this is the fallback that works on every target.
final GlobalKey screenshotBoundaryKey = GlobalKey();

class App extends StatefulWidget {
  const App({super.key, required this.dependencies});

  final AppDependencies dependencies;

  @override
  State<App> createState() => _AppState();
}

class _AppState extends State<App> {
  @override
  void initState() {
    super.initState();
    widget.dependencies.bootstrap();
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.dependencies.localeController,
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
              supportedLocales: supportedLocales,
              locale: widget.dependencies.localeController.locale,
              localizationsDelegates: const [
                GlobalMaterialLocalizations.delegate,
                GlobalWidgetsLocalizations.delegate,
                GlobalCupertinoLocalizations.delegate,
              ],
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
            return const Scaffold(body: Center(child: CircularProgressIndicator()));
          case AuthStatus.anonymous:
            return LoginScreen(
              useCases: dependencies.authUseCases,
              session: dependencies.session,
              onCreateAccount: () {
                Navigator.of(context).push(
                  MaterialPageRoute(
                    builder: (_) => RegisterScreen(
                      useCases: dependencies.authUseCases,
                      session: dependencies.session,
                    ),
                  ),
                );
              },
            );
          case AuthStatus.authenticated:
            return ProfileScreen(
              useCases: dependencies.authUseCases,
              session: dependencies.session,
              localeController: dependencies.localeController,
            );
        }
      },
    );
  }
}
