/// Validation rules — the one thing this layer owns that no other layer
/// should. Mirrors the contract exactly (`RegisterRequest`/`LoginRequest` in
/// `contract/openapi.yaml`, the same rule the Angular and Next.js frontends'
/// own `domain/validation` follow) so a form never lets through what the
/// backend would reject anyway. The backend still re-validates
/// independently — this exists only for a fast, local error message.
///
/// Depends on nothing: no Flutter import, no generated client import, no
/// import from this feature's own `application/`, `data/` or
/// `presentation/`. `test/architecture/boundary_test.dart` enforces that.
library;

class RegisterFormValues {
  const RegisterFormValues({
    required this.email,
    required this.password,
    required this.displayName,
  });

  final String email;
  final String password;
  final String displayName;
}

class LoginFormValues {
  const LoginFormValues({required this.email, required this.password});

  final String email;
  final String password;
}

/// Field name -> a stable error *code*, never a message: `domain/` has no
/// Flutter, no i18n and no locale dependency, so it cannot itself produce
/// English or Spanish text. `features/auth/presentation/i18n/auth_strings.dart`
/// maps each code to `auth.<code>`, a translated key.
typedef FieldErrors = Map<String, String>;

final RegExp _emailPattern = RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$');

FieldErrors validateRegisterForm(RegisterFormValues values) {
  final errors = <String, String>{};
  if (!_emailPattern.hasMatch(values.email)) {
    errors['email'] = 'emailInvalid';
  }
  if (values.password.length < 8) {
    errors['password'] = 'passwordTooShort';
  }
  final trimmedName = values.displayName.trim();
  if (trimmedName.isEmpty) {
    errors['displayName'] = 'displayNameRequired';
  } else if (values.displayName.length > 200) {
    errors['displayName'] = 'displayNameTooLong';
  }
  return errors;
}

FieldErrors validateLoginForm(LoginFormValues values) {
  final errors = <String, String>{};
  if (!_emailPattern.hasMatch(values.email)) {
    errors['email'] = 'emailInvalid';
  }
  if (values.password.isEmpty) {
    errors['password'] = 'passwordRequired';
  }
  return errors;
}
