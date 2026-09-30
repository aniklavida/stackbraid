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
    'common.offlineCached': 'Offline — showing cached data',
    'common.offlinePending': 'Offline — {count} changes queued for sync',
    'common.syncing': 'Syncing queued changes…',
    'common.staleWarning': 'Data may be out of date',
    'common.navHome': 'Home',
    'common.navProfile': 'Profile',
    'common.homeWelcome': 'Welcome',
    'common.homeReadyTitle': 'Ready for features',
    'common.homeReadySubtitle': 'The mobile shell is connected and ready for domain features.',
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
    'common.offlineCached': 'Sin conexión — mostrando datos en caché',
    'common.offlinePending': 'Sin conexión — {count} cambios en cola',
    'common.syncing': 'Sincronizando cambios en cola…',
    'common.staleWarning': 'Los datos pueden estar desactualizados',
    'common.navHome': 'Inicio',
    'common.navProfile': 'Perfil',
    'common.homeWelcome': 'Bienvenido',
    'common.homeReadyTitle': 'Listo para funciones',
    'common.homeReadySubtitle': 'El entorno móvil está conectado y listo para funciones de dominio.',
  },
};
