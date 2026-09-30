import 'package:flutter/material.dart';

import '../i18n/app_localizations.dart';
import '../theme/theme_controller.dart';

/// Segmented control for choosing between System, Light, and Dark appearance.
class ThemeModeSwitcher extends StatelessWidget {
  const ThemeModeSwitcher({super.key, required this.controller});

  final ThemeController controller;

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: controller,
      builder: (context, _) {
        return SegmentedButton<ThemeMode>(
          segments: [
            ButtonSegment(
              value: ThemeMode.system,
              label: Text(context.t('common.themeSystem')),
              icon: const Icon(Icons.brightness_auto, size: 16),
            ),
            ButtonSegment(
              value: ThemeMode.light,
              label: Text(context.t('common.themeLight')),
              icon: const Icon(Icons.light_mode_outlined, size: 16),
            ),
            ButtonSegment(
              value: ThemeMode.dark,
              label: Text(context.t('common.themeDark')),
              icon: const Icon(Icons.dark_mode_outlined, size: 16),
            ),
          ],
          selected: {controller.themeMode},
          onSelectionChanged: (selection) => controller.setThemeMode(selection.first),
        );
      },
    );
  }
}
