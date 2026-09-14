import 'translations.dart';

/// Genuinely cross-feature text (app title, generic buttons, generic error
/// copy) — everything specific to one feature lives with that feature
/// instead (see `features/auth/presentation/i18n/auth_strings.dart`).
const Translations commonStrings = {
  'en': {
    'common.appTitle': 'StackBraid',
    'common.loading': 'Loading…',
    'common.retry': 'Try again',
    'common.cancel': 'Cancel',
    'common.genericError': 'Something went wrong. Please try again.',
    'common.language': 'Language',
    'common.languageEnglish': 'English',
    'common.languageSpanish': 'Spanish',
  },
  'es': {
    'common.appTitle': 'StackBraid',
    'common.loading': 'Cargando…',
    'common.retry': 'Reintentar',
    'common.cancel': 'Cancelar',
    'common.genericError': 'Algo salió mal. Inténtalo de nuevo.',
    'common.language': 'Idioma',
    'common.languageEnglish': 'Inglés',
    'common.languageSpanish': 'Español',
  },
};
