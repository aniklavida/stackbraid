/// Runtime configuration read from build-time `--dart-define` values —
/// Flutter's equivalent of the web frontends' runtime/`NEXT_PUBLIC_*`
/// environment variables. Repointing a build at the other backend is one
/// flag, never a code change:
///
///   flutter run -d macos --dart-define=API_BASE_URL=http://127.0.0.1:8080
///   flutter test integration_test -d macos \
///     --dart-define=API_BASE_URL=http://127.0.0.1:8090
class AppConfig {
  const AppConfig({required this.apiBaseUrl});

  final String apiBaseUrl;

  static const AppConfig instance = AppConfig(
    apiBaseUrl: String.fromEnvironment(
      'API_BASE_URL',
      defaultValue: 'http://127.0.0.1:8080',
    ),
  );
}
