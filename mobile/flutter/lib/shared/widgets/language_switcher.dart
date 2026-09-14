import 'package:flutter/material.dart';

import '../i18n/app_localizations.dart';
import '../i18n/locale_controller.dart';

/// A small, reusable segmented control for English/Spanish — shared UI, not
/// owned by any one feature, since every screen in the app may want it.
class LanguageSwitcher extends StatelessWidget {
  const LanguageSwitcher({super.key, required this.controller});

  final LocaleController controller;

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: controller,
      builder: (context, _) {
        return SegmentedButton<Locale>(
          segments: [
            ButtonSegment(value: const Locale('en'), label: Text(context.t('common.languageEnglish'))),
            ButtonSegment(value: const Locale('es'), label: Text(context.t('common.languageSpanish'))),
          ],
          selected: {controller.locale},
          onSelectionChanged: (selection) => controller.setLocale(selection.first),
        );
      },
    );
  }
}
