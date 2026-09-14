import '../../../../shared/i18n/translations.dart';

/// This feature's own translations — ships with the feature that owns it,
/// the same rule this project's web frontends already follow, each with
/// its own scoped-per-feature translation loader.
const Translations authStrings = {
  'en': {
    'auth.signInTitle': 'Sign in',
    'auth.registerTitle': 'Create account',
    'auth.emailLabel': 'Email',
    'auth.passwordLabel': 'Password',
    'auth.displayNameLabel': 'Display name',
    'auth.signInButton': 'Sign in',
    'auth.createAccountButton': 'Create account',
    'auth.signOutButton': 'Sign out',
    'auth.noAccountPrompt': "Don't have an account?",
    'auth.haveAccountPrompt': 'Already have an account?',
    'auth.createOneLink': 'Create one',
    'auth.signInLink': 'Sign in',
    'auth.profileTitle': 'Profile',
    'auth.memberSince': 'Member since',
    'auth.rolesLabel': 'Roles',
    'auth.noRoles': 'No roles assigned yet.',
    'auth.emailInvalid': 'Enter a valid email address.',
    'auth.passwordTooShort': 'Use at least 8 characters.',
    'auth.passwordRequired': 'Enter your password.',
    'auth.displayNameRequired': 'Tell us what to call you.',
    'auth.displayNameTooLong': 'Keep it under 200 characters.',
    'auth.welcomeBack': 'Welcome back',
  },
  'es': {
    'auth.signInTitle': 'Iniciar sesión',
    'auth.registerTitle': 'Crear cuenta',
    'auth.emailLabel': 'Correo electrónico',
    'auth.passwordLabel': 'Contraseña',
    'auth.displayNameLabel': 'Nombre visible',
    'auth.signInButton': 'Iniciar sesión',
    'auth.createAccountButton': 'Crear cuenta',
    'auth.signOutButton': 'Cerrar sesión',
    'auth.noAccountPrompt': '¿No tienes una cuenta?',
    'auth.haveAccountPrompt': '¿Ya tienes una cuenta?',
    'auth.createOneLink': 'Crea una',
    'auth.signInLink': 'Inicia sesión',
    'auth.profileTitle': 'Perfil',
    'auth.memberSince': 'Miembro desde',
    'auth.rolesLabel': 'Roles',
    'auth.noRoles': 'Todavía no tiene roles asignados.',
    'auth.emailInvalid': 'Introduce una dirección de correo válida.',
    'auth.passwordTooShort': 'Usa al menos 8 caracteres.',
    'auth.passwordRequired': 'Introduce tu contraseña.',
    'auth.displayNameRequired': 'Dinos cómo llamarte.',
    'auth.displayNameTooLong': 'Máximo 200 caracteres.',
    'auth.welcomeBack': 'Bienvenido de nuevo',
  },
};

/// Turns a `domain/validation.dart` error *code* (e.g. `"emailInvalid"`)
/// into the translation key for it. Kept in the feature's own presentation
/// i18n file rather than the domain layer, so `domain/` stays free of any
/// locale/Flutter dependency.
String authFieldErrorKey(String code) => 'auth.$code';
