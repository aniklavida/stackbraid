import 'package:flutter/material.dart';

/// One Material 3 theme, light and dark — this app has no web/admin split
/// to theme separately (`docs/STRUCTURE.md`: "There is no web/admin split
/// on mobile — administration is a desktop job").
class AppTheme {
  AppTheme._();

  static ThemeData light = ThemeData(
    useMaterial3: true,
    colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF2F6FED)),
    inputDecorationTheme: const InputDecorationTheme(border: OutlineInputBorder()),
  );

  static ThemeData dark = ThemeData(
    useMaterial3: true,
    colorScheme: ColorScheme.fromSeed(
      seedColor: const Color(0xFF2F6FED),
      brightness: Brightness.dark,
    ),
    inputDecorationTheme: const InputDecorationTheme(border: OutlineInputBorder()),
  );
}
