import 'package:stackbraid_client/stackbraid_client.dart';

/// The session port a feature's `application/` use cases depend on to
/// commit or drop a signed-in session, without depending on
/// `shared/auth/session_controller.dart`'s concrete state-management choice
/// (a `ChangeNotifier`) directly.
///
/// Defined in `shared/`, not inside `features/auth/application/`, because
/// Dart is nominally typed: a concrete session controller must `implements`
/// this exact type to satisfy it, unlike the web frontends' TypeScript
/// `AuthProvider`, which satisfies an equivalent `SessionPort` purely
/// structurally without importing it. Putting the interface here keeps
/// `shared` free of any import from `features/` while still letting
/// `features/auth/application/use_cases.dart` depend on it (an application
/// layer depending on `shared` is normal — only the reverse, `shared`
/// depending on a feature, is the rule this repository enforces).
abstract class SessionPort {
  Future<User> adoptSession(TokenPair tokens);
  Future<void> clearSession();
}
