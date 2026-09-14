import 'package:flutter_test/flutter_test.dart';
import 'package:stackbraid_mobile/features/auth/domain/validation.dart';

void main() {
  group('validateRegisterForm', () {
    test('accepts a genuinely valid submission', () {
      final errors = validateRegisterForm(
        const RegisterFormValues(
          email: 'fatima.rahman@example.com',
          password: 'correct-horse-battery',
          displayName: 'Fatima Rahman',
        ),
      );
      expect(errors, isEmpty);
    });

    test('rejects a password under 8 characters, matching the contract\'s own RegisterRequest.password minLength', () {
      final errors = validateRegisterForm(
        const RegisterFormValues(email: 'a@example.com', password: 'short', displayName: 'A'),
      );
      expect(errors['password'], 'passwordTooShort');
    });

    test('rejects a malformed email', () {
      final errors = validateRegisterForm(
        const RegisterFormValues(email: 'not-an-email', password: 'correct-horse-battery', displayName: 'A'),
      );
      expect(errors['email'], 'emailInvalid');
    });

    test('rejects an empty display name', () {
      final errors = validateRegisterForm(
        const RegisterFormValues(email: 'a@example.com', password: 'correct-horse-battery', displayName: '   '),
      );
      expect(errors['displayName'], 'displayNameRequired');
    });

    test('rejects a display name over 200 characters, matching the contract\'s own maxLength', () {
      final errors = validateRegisterForm(
        RegisterFormValues(email: 'a@example.com', password: 'correct-horse-battery', displayName: 'x' * 201),
      );
      expect(errors['displayName'], 'displayNameTooLong');
    });
  });

  group('validateLoginForm', () {
    test('accepts a valid submission', () {
      expect(validateLoginForm(const LoginFormValues(email: 'a@example.com', password: 'anything')), isEmpty);
    });

    test('rejects an empty password without guessing a minimum length — the backend, not this form, owns that rule for login', () {
      final errors = validateLoginForm(const LoginFormValues(email: 'a@example.com', password: ''));
      expect(errors['password'], 'passwordRequired');
    });
  });
}
