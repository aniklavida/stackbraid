import 'package:stackbraid_client/stackbraid_client.dart';

import '../../../shared/auth/session_port.dart';
import '../data/auth_repository.dart';
import '../domain/validation.dart';

class ValidationFailed implements Exception {
  ValidationFailed(this.fieldErrors);

  final FieldErrors fieldErrors;

  @override
  String toString() => 'ValidationFailed($fieldErrors)';
}

class AuthUseCases {
  AuthUseCases(this._repository);

  final AuthRepository _repository;

  /// `POST /v1/auth/register` only creates the account — the contract is
  /// explicit that it does not issue a session (unlike login/refresh, it
  /// returns the created `User`, not a `TokenPair`). Signing in right after,
  /// with the same credentials this form already collected, is this app's
  /// choice of UX, not something the endpoint does on its own — the same
  /// choice both web frontends make.
  Future<User> registerAccount(RegisterFormValues values, SessionPort session) async {
    final fieldErrors = validateRegisterForm(values);
    if (fieldErrors.isNotEmpty) throw ValidationFailed(fieldErrors);

    await _repository.register(values);
    final tokens = await _repository.signIn(
      LoginFormValues(email: values.email, password: values.password),
    );
    return session.adoptSession(tokens);
  }

  Future<User> signIn(LoginFormValues values, SessionPort session) async {
    final fieldErrors = validateLoginForm(values);
    if (fieldErrors.isNotEmpty) throw ValidationFailed(fieldErrors);

    final tokens = await _repository.signIn(values);
    return session.adoptSession(tokens);
  }

  Future<void> signOut(String? refreshToken, SessionPort session) async {
    try {
      await _repository.signOut(refreshToken);
    } catch (_) {
      // Swallowed deliberately: the session is cleared locally either way,
      // so an offline (or already-expired) sign-out still signs this device
      // out rather than surfacing a network error for an action that, from
      // the user's point of view, already succeeded. The web frontends make
      // the same choice in `shared/auth`.
    } finally {
      await session.clearSession();
    }
  }
}
