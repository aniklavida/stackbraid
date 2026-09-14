import 'package:flutter_test/flutter_test.dart';
import 'package:stackbraid_client/stackbraid_client.dart';
import 'package:stackbraid_mobile/features/auth/application/use_cases.dart';
import 'package:stackbraid_mobile/features/auth/data/auth_repository.dart';
import 'package:stackbraid_mobile/features/auth/domain/validation.dart';
import 'package:stackbraid_mobile/shared/auth/session_port.dart';

User _user({String email = 'a@example.com', String displayName = 'A'}) => User(
      id: 'u1',
      email: email,
      displayName: displayName,
      status: UserStatus.active,
      roles: const [],
      createdAt: DateTime.utc(2026, 1, 1),
      updatedAt: DateTime.utc(2026, 1, 1),
    );

TokenPair _tokens() => TokenPair(
      accessToken: 'access-token',
      refreshToken: 'refresh-token',
      tokenType: TokenPairTokenTypeEnum.bearer,
      expiresAt: DateTime.utc(2026, 1, 1).add(const Duration(minutes: 15)),
    );

class _FakeAuthRepository implements AuthRepository {
  RegisterFormValues? registerCalledWith;
  LoginFormValues? signInCalledWith;
  String? signOutCalledWith;
  bool signOutThrows = false;

  @override
  Future<User> register(RegisterFormValues values) async {
    registerCalledWith = values;
    return _user(email: values.email, displayName: values.displayName);
  }

  @override
  Future<TokenPair> signIn(LoginFormValues values) async {
    signInCalledWith = values;
    return _tokens();
  }

  @override
  Future<TokenPair> refresh(String refreshToken) async => _tokens();

  @override
  Future<void> signOut(String? refreshToken) async {
    signOutCalledWith = refreshToken;
    if (signOutThrows) throw StateError('offline');
  }

  @override
  Future<User> currentUser(String accessToken) async => _user();
}

class _FakeSession implements SessionPort {
  TokenPair? adopted;
  bool cleared = false;

  @override
  Future<User> adoptSession(TokenPair tokens) async {
    adopted = tokens;
    return _user();
  }

  @override
  Future<void> clearSession() async {
    cleared = true;
  }
}

void main() {
  group('AuthUseCases.registerAccount', () {
    test('validates first and never calls the repository on invalid input', () async {
      final repo = _FakeAuthRepository();
      final useCases = AuthUseCases(repo);
      final session = _FakeSession();

      await expectLater(
        () => useCases.registerAccount(
          const RegisterFormValues(email: 'not-an-email', password: 'x', displayName: ''),
          session,
        ),
        throwsA(isA<ValidationFailed>()),
      );
      expect(repo.registerCalledWith, isNull);
    });

    test('registers, then signs in with the same credentials, then adopts the session', () async {
      final repo = _FakeAuthRepository();
      final useCases = AuthUseCases(repo);
      final session = _FakeSession();

      await useCases.registerAccount(
        const RegisterFormValues(email: 'sadia@example.com', password: 'correct-horse-battery', displayName: 'Sadia'),
        session,
      );

      expect(repo.registerCalledWith?.email, 'sadia@example.com');
      expect(repo.signInCalledWith?.email, 'sadia@example.com');
      expect(session.adopted, isNotNull);
    });
  });

  group('AuthUseCases.signIn', () {
    test('rejects an empty password locally, never calling the repository', () async {
      final repo = _FakeAuthRepository();
      final useCases = AuthUseCases(repo);
      final session = _FakeSession();

      await expectLater(
        () => useCases.signIn(const LoginFormValues(email: 'a@example.com', password: ''), session),
        throwsA(isA<ValidationFailed>()),
      );
      expect(repo.signInCalledWith, isNull);
    });

    test('signs in and adopts the returned session', () async {
      final repo = _FakeAuthRepository();
      final useCases = AuthUseCases(repo);
      final session = _FakeSession();

      await useCases.signIn(const LoginFormValues(email: 'a@example.com', password: 'anything'), session);
      expect(session.adopted?.accessToken, 'access-token');
    });
  });

  group('AuthUseCases.signOut', () {
    test('clears the session even when the repository call fails', () async {
      final repo = _FakeAuthRepository()..signOutThrows = true;
      final useCases = AuthUseCases(repo);
      final session = _FakeSession();

      await useCases.signOut('a-refresh-token', session);

      expect(repo.signOutCalledWith, 'a-refresh-token');
      expect(session.cleared, isTrue);
    });
  });
}
