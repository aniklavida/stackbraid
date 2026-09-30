import 'package:flutter/material.dart';

import 'tokens.dart';

/// StackBraid mobile theme definitions.
///
/// Implements the Technical Dense brand direction:
/// - Hue: 215, Chroma: 0.14, Warmth: 0.005, Radius scale: 0.7
/// - Fonts: IBM Plex Sans (UI) + IBM Plex Mono (code/metrics)
/// - Theme defaults: Dark mode default with light mode secondary
/// - Accent budget under 5% of any screen
abstract final class AppTheme {
  /// Light theme
  static final ThemeData light = _buildTheme(
    brightness: Brightness.light,
    bg: AppColorsLight.bg,
    rail: AppColorsLight.rail,
    surf: AppColorsLight.surf,
    subtle: AppColorsLight.subtle,
    bd: AppColorsLight.bd,
    bd2: AppColorsLight.bd2,
    line: AppColorsLight.line,
    act: AppColorsLight.act,
    chip: AppColorsLight.chip,
    ink: AppColorsLight.ink,
    ink2: AppColorsLight.ink2,
    mut: AppColorsLight.mut,
    faint: AppColorsLight.faint,
    acc: AppColorsLight.acc,
    acc2: AppColorsLight.acc2,
    accbg: AppColorsLight.accbg,
    onAcc: AppColorsLight.onAcc,
    bad: AppColorsLight.bad,
    badbg: AppColorsLight.badbg,
    ok: AppColorsLight.ok,
    okbg: AppColorsLight.okbg,
    warn: AppColorsLight.warn,
    warnbg: AppColorsLight.warnbg,
    info: AppColorsLight.info,
    infobg: AppColorsLight.infobg,
  );

  /// Dark theme (StackBraid brand default)
  static final ThemeData dark = _buildTheme(
    brightness: Brightness.dark,
    bg: AppColorsDark.bg,
    rail: AppColorsDark.rail,
    surf: AppColorsDark.surf,
    subtle: AppColorsDark.subtle,
    bd: AppColorsDark.bd,
    bd2: AppColorsDark.bd2,
    line: AppColorsDark.line,
    act: AppColorsDark.act,
    chip: AppColorsDark.chip,
    ink: AppColorsDark.ink,
    ink2: AppColorsDark.ink2,
    mut: AppColorsDark.mut,
    faint: AppColorsDark.faint,
    acc: AppColorsDark.acc,
    acc2: AppColorsDark.acc2,
    accbg: AppColorsDark.accbg,
    onAcc: AppColorsDark.onAcc,
    bad: AppColorsDark.bad,
    badbg: AppColorsDark.badbg,
    ok: AppColorsDark.ok,
    okbg: AppColorsDark.okbg,
    warn: AppColorsDark.warn,
    warnbg: AppColorsDark.warnbg,
    info: AppColorsDark.info,
    infobg: AppColorsDark.infobg,
  );

  static ThemeData _buildTheme({
    required Brightness brightness,
    required Color bg,
    required Color rail,
    required Color surf,
    required Color subtle,
    required Color bd,
    required Color bd2,
    required Color line,
    required Color act,
    required Color chip,
    required Color ink,
    required Color ink2,
    required Color mut,
    required Color faint,
    required Color acc,
    required Color acc2,
    required Color accbg,
    required Color onAcc,
    required Color bad,
    required Color badbg,
    required Color ok,
    required Color okbg,
    required Color warn,
    required Color warnbg,
    required Color info,
    required Color infobg,
  }) {
    final isDark = brightness == Brightness.dark;

    final colorScheme = ColorScheme(
      brightness: brightness,
      primary: acc,
      onPrimary: onAcc,
      primaryContainer: accbg,
      onPrimaryContainer: isDark ? acc : acc2,
      secondary: ink2,
      onSecondary: surf,
      secondaryContainer: subtle,
      onSecondaryContainer: ink,
      tertiary: info,
      onTertiary: onAcc,
      tertiaryContainer: infobg,
      onTertiaryContainer: info,
      error: bad,
      onError: isDark ? bg : onAcc,
      errorContainer: badbg,
      onErrorContainer: bad,
      surface: surf,
      onSurface: ink,
      onSurfaceVariant: mut,
      outline: bd,
      outlineVariant: bd2,
      shadow: Colors.black.withValues(alpha: isDark ? 0.4 : 0.08),
      scrim: Colors.black.withValues(alpha: isDark ? 0.6 : 0.3),
      surfaceContainerLowest: bg,
      surfaceContainerLow: subtle,
      surfaceContainer: surf,
      surfaceContainerHigh: subtle,
      surfaceContainerHighest: rail,
    );

    final textTheme = TextTheme(
      displayLarge: TextStyle(
        fontFamily: AppTokens.fontDisplay,
        fontSize: 34,
        fontWeight: FontWeight.w600,
        letterSpacing: -0.5,
        color: ink,
      ),
      displayMedium: TextStyle(
        fontFamily: AppTokens.fontDisplay,
        fontSize: 28,
        fontWeight: FontWeight.w600,
        letterSpacing: -0.3,
        color: ink,
      ),
      displaySmall: TextStyle(
        fontFamily: AppTokens.fontDisplay,
        fontSize: 22,
        fontWeight: FontWeight.w600,
        letterSpacing: -0.2,
        color: ink,
      ),
      headlineLarge: TextStyle(
        fontFamily: AppTokens.fontUi,
        fontSize: 28,
        fontWeight: FontWeight.w600,
        letterSpacing: -0.3,
        color: ink,
      ),
      headlineMedium: TextStyle(
        fontFamily: AppTokens.fontUi,
        fontSize: 22,
        fontWeight: FontWeight.w600,
        color: ink,
      ),
      headlineSmall: TextStyle(
        fontFamily: AppTokens.fontUi,
        fontSize: 20,
        fontWeight: FontWeight.w600,
        color: ink,
      ),
      titleLarge: TextStyle(
        fontFamily: AppTokens.fontUi,
        fontSize: 18,
        fontWeight: FontWeight.w600,
        color: ink,
      ),
      titleMedium: TextStyle(
        fontFamily: AppTokens.fontUi,
        fontSize: 16,
        fontWeight: FontWeight.w600,
        color: ink,
      ),
      titleSmall: TextStyle(
        fontFamily: AppTokens.fontUi,
        fontSize: 14,
        fontWeight: FontWeight.w600,
        color: ink,
      ),
      bodyLarge: TextStyle(
        fontFamily: AppTokens.fontUi,
        fontSize: 16,
        fontWeight: FontWeight.w400,
        color: ink,
      ),
      bodyMedium: TextStyle(
        fontFamily: AppTokens.fontUi,
        fontSize: 14,
        fontWeight: FontWeight.w400,
        color: ink2,
      ),
      bodySmall: TextStyle(
        fontFamily: AppTokens.fontUi,
        fontSize: 12.5,
        fontWeight: FontWeight.w400,
        color: mut,
      ),
      labelLarge: TextStyle(
        fontFamily: AppTokens.fontUi,
        fontSize: 14,
        fontWeight: FontWeight.w500,
        color: ink,
      ),
      labelMedium: TextStyle(
        fontFamily: AppTokens.fontUi,
        fontSize: 12,
        fontWeight: FontWeight.w500,
        color: mut,
      ),
      labelSmall: TextStyle(
        fontFamily: AppTokens.fontUi,
        fontSize: 11,
        fontWeight: FontWeight.w600,
        color: faint,
      ),
    );

    return ThemeData(
      useMaterial3: true,
      brightness: brightness,
      colorScheme: colorScheme,
      scaffoldBackgroundColor: bg,
      fontFamily: AppTokens.fontUi,
      textTheme: textTheme,
      splashFactory: InkSparkle.splashFactory,
      dividerColor: line,
      cardColor: surf,

      // App bar
      appBarTheme: AppBarTheme(
        backgroundColor: bg,
        foregroundColor: ink,
        elevation: 0,
        scrolledUnderElevation: 0,
        centerTitle: false,
        titleTextStyle: TextStyle(
          fontFamily: AppTokens.fontUi,
          fontSize: 17,
          fontWeight: FontWeight.w600,
          color: ink,
        ),
        iconTheme: IconThemeData(color: ink, size: 20),
      ),

      // Primary filled button (one per screen, 50px tall, radius 7px)
      filledButtonTheme: FilledButtonThemeData(
        style: ButtonStyle(
          backgroundColor: WidgetStateProperty.resolveWith((states) {
            if (states.contains(WidgetState.disabled)) {
              return acc.withValues(alpha: 0.4);
            }
            return acc;
          }),
          foregroundColor: WidgetStateProperty.all(onAcc),
          minimumSize: WidgetStateProperty.all(
            const Size(AppTokens.minTouchTarget, AppTokens.buttonHeight),
          ),
          padding: WidgetStateProperty.all(
            const EdgeInsets.symmetric(horizontal: 20),
          ),
          shape: WidgetStateProperty.all(
            RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(AppTokens.rMd),
            ),
          ),
          textStyle: WidgetStateProperty.all(
            const TextStyle(
              fontFamily: AppTokens.fontUi,
              fontSize: 16,
              fontWeight: FontWeight.w600,
            ),
          ),
          elevation: WidgetStateProperty.all(0),
        ),
      ),

      // Secondary outlined button (hairline border, 50px tall, radius 7px)
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: ButtonStyle(
          foregroundColor: WidgetStateProperty.resolveWith((states) {
            if (states.contains(WidgetState.disabled)) {
              return faint;
            }
            return ink;
          }),
          side: WidgetStateProperty.resolveWith((states) {
            if (states.contains(WidgetState.disabled)) {
              return BorderSide(color: bd.withValues(alpha: 0.5), width: 1.0);
            }
            return BorderSide(color: bd, width: 1.0);
          }),
          minimumSize: WidgetStateProperty.all(
            const Size(AppTokens.minTouchTarget, AppTokens.buttonHeight),
          ),
          padding: WidgetStateProperty.all(
            const EdgeInsets.symmetric(horizontal: 20),
          ),
          shape: WidgetStateProperty.all(
            RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(AppTokens.rMd),
            ),
          ),
          textStyle: WidgetStateProperty.all(
            const TextStyle(
              fontFamily: AppTokens.fontUi,
              fontSize: 16,
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
      ),

      // Text / ghost button (48px hit target, accent or ink)
      textButtonTheme: TextButtonThemeData(
        style: ButtonStyle(
          foregroundColor: WidgetStateProperty.all(acc),
          minimumSize: WidgetStateProperty.all(
            const Size(AppTokens.minTouchTarget, AppTokens.minTouchTarget),
          ),
          padding: WidgetStateProperty.all(
            const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
          ),
          textStyle: WidgetStateProperty.all(
            const TextStyle(
              fontFamily: AppTokens.fontUi,
              fontSize: 14,
              fontWeight: FontWeight.w500,
            ),
          ),
        ),
      ),

      // Inputs (16px text size to prevent iOS zoom, 48px height, radius 7px)
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: surf,
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        hintStyle: TextStyle(
          fontFamily: AppTokens.fontUi,
          fontSize: 16,
          color: faint,
        ),
        labelStyle: TextStyle(
          fontFamily: AppTokens.fontUi,
          fontSize: 14,
          color: mut,
          fontWeight: FontWeight.w500,
        ),
        floatingLabelStyle: TextStyle(
          fontFamily: AppTokens.fontUi,
          fontSize: 13,
          color: acc,
          fontWeight: FontWeight.w500,
        ),
        errorStyle: TextStyle(
          fontFamily: AppTokens.fontUi,
          fontSize: 12,
          color: bad,
          fontWeight: FontWeight.w500,
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppTokens.rMd),
          borderSide: BorderSide(color: bd, width: 1.0),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppTokens.rMd),
          borderSide: BorderSide(color: acc, width: 2.0),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppTokens.rMd),
          borderSide: BorderSide(color: bad, width: 1.0),
        ),
        focusedErrorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppTokens.rMd),
          borderSide: BorderSide(color: bad, width: 2.0),
        ),
        disabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppTokens.rMd),
          borderSide: BorderSide(color: bd.withValues(alpha: 0.5), width: 1.0),
        ),
      ),

      // Cards (radius 10px, 1px border, 0 elevation)
      cardTheme: CardThemeData(
        color: surf,
        elevation: 0,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppTokens.rLg),
          side: BorderSide(color: bd, width: 1.0),
        ),
      ),

      // Navigation bar (bottom tab bar, 64px, safe area)
      navigationBarTheme: NavigationBarThemeData(
        height: AppTokens.tabBarHeight,
        backgroundColor: rail,
        surfaceTintColor: Colors.transparent,
        indicatorColor: accbg,
        iconTheme: WidgetStateProperty.resolveWith((states) {
          if (states.contains(WidgetState.selected)) {
            return IconThemeData(color: acc, size: 22);
          }
          return IconThemeData(color: mut, size: 22);
        }),
        labelTextStyle: WidgetStateProperty.resolveWith((states) {
          final isSelected = states.contains(WidgetState.selected);
          return TextStyle(
            fontFamily: AppTokens.fontUi,
            fontSize: 12,
            fontWeight: isSelected ? FontWeight.w600 : FontWeight.w500,
            color: isSelected ? acc : mut,
          );
        }),
      ),

      // SnackBar (floating, dark neutral, radius 7px)
      snackBarTheme: SnackBarThemeData(
        backgroundColor: ink,
        contentTextStyle: TextStyle(
          fontFamily: AppTokens.fontUi,
          fontSize: 14,
          color: bg,
        ),
        actionTextColor: acc,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppTokens.rMd),
        ),
        behavior: SnackBarBehavior.floating,
      ),

      // Dialogs (modal, radius 13-14px, 1px border)
      dialogTheme: DialogThemeData(
        backgroundColor: surf,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppTokens.rXl),
          side: BorderSide(color: bd, width: 1.0),
        ),
        titleTextStyle: TextStyle(
          fontFamily: AppTokens.fontUi,
          fontSize: 18,
          fontWeight: FontWeight.w600,
          color: ink,
        ),
        contentTextStyle: TextStyle(
          fontFamily: AppTokens.fontUi,
          fontSize: 14,
          color: ink2,
        ),
      ),

      // Bottom sheets (radius top 14px, handle 36x5)
      bottomSheetTheme: BottomSheetThemeData(
        backgroundColor: surf,
        modalBackgroundColor: surf,
        elevation: 0,
        shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(
            top: Radius.circular(AppTokens.sheetTopRadius),
          ),
        ),
        dragHandleColor: bd2,
        dragHandleSize: const Size(
          AppTokens.sheetHandleWidth,
          AppTokens.sheetHandleHeight,
        ),
        showDragHandle: true,
      ),

      // Progress indicator (accent color)
      progressIndicatorTheme: ProgressIndicatorThemeData(
        color: acc,
        linearTrackColor: subtle,
        circularTrackColor: subtle,
      ),

      // Segmented control (buttons, radius 7px)
      segmentedButtonTheme: SegmentedButtonThemeData(
        style: ButtonStyle(
          backgroundColor: WidgetStateProperty.resolveWith((states) {
            if (states.contains(WidgetState.selected)) {
              return accbg;
            }
            return Colors.transparent;
          }),
          foregroundColor: WidgetStateProperty.resolveWith((states) {
            if (states.contains(WidgetState.selected)) {
              return acc;
            }
            return ink2;
          }),
          side: WidgetStateProperty.all(BorderSide(color: bd, width: 1.0)),
          textStyle: WidgetStateProperty.all(
            const TextStyle(
              fontFamily: AppTokens.fontUi,
              fontSize: 14,
              fontWeight: FontWeight.w500,
            ),
          ),
          shape: WidgetStateProperty.all(
            RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(AppTokens.rMd),
            ),
          ),
        ),
      ),

      // Divider
      dividerTheme: DividerThemeData(
        color: line,
        thickness: 1.0,
        space: 1.0,
      ),

      // Chip
      chipTheme: ChipThemeData(
        backgroundColor: subtle,
        disabledColor: subtle.withValues(alpha: 0.5),
        selectedColor: accbg,
        labelStyle: TextStyle(
          fontFamily: AppTokens.fontUi,
          fontSize: 12,
          fontWeight: FontWeight.w500,
          color: ink,
        ),
        side: BorderSide(color: bd, width: 1.0),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppTokens.rSm),
        ),
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      ),

      // ListTile
      listTileTheme: ListTileThemeData(
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
        titleTextStyle: TextStyle(
          fontFamily: AppTokens.fontUi,
          fontSize: 16,
          fontWeight: FontWeight.w500,
          color: ink,
        ),
        subtitleTextStyle: TextStyle(
          fontFamily: AppTokens.fontUi,
          fontSize: 13.5,
          color: mut,
        ),
        iconColor: mut,
      ),
    );
  }
}
