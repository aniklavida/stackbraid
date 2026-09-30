import 'package:flutter/material.dart';

/// Design tokens for StackBraid mobile.
///
/// Derived strictly from the shared brand values (docs/DESIGN.md & tokens.css):
/// - Hue: 215 (technical cobalt)
/// - Chroma: 0.14
/// - Warmth: 0.005 (near-zero warmth, slate neutrals)
/// - Radius scale: 0.7 (compact engineered curves)
/// - Typography: IBM Plex Sans + IBM Plex Mono
abstract final class AppTokens {
  // Brand foundations
  static const double brandH = 215.0;
  static const double brandC = 0.14;
  static const double brandL = 0.52;
  static const double brandLDark = 0.68;
  static const double warmth = 0.005;
  static const double radiusScale = 0.7;

  // Typography font families
  static const String fontUi = 'IBM Plex Sans';
  static const String fontDisplay = 'IBM Plex Sans';
  static const String fontMono = 'IBM Plex Mono';

  // Spacing scale (4px grid)
  static const double sp1 = 4.0;
  static const double sp2 = 8.0;
  static const double sp3 = 12.0;
  static const double sp4 = 16.0;
  static const double sp5 = 20.0;
  static const double sp6 = 24.0;
  static const double sp8 = 32.0;
  static const double sp10 = 40.0;
  static const double sp12 = 48.0;

  // Radius scale (0.7 brand scale)
  static const double rXs = 2.8;   // ~3px badges, indicators (4 * 0.7)
  static const double rSm = 4.9;   // ~5px chips, small controls (7 * 0.7)
  static const double rMd = 7.0;   // ~7-8px buttons, inputs (10 * 0.7)
  static const double rLg = 9.8;   // ~10px cards (14 * 0.7)
  static const double rXl = 12.6;  // ~13-14px sheets, dialogs (18 * 0.7)
  static const double rPill = 999.0;

  // Mobile layout metrics (390 canvas, safe areas, thumb zone)
  static const double minTouchTarget = 48.0;
  static const double inputHeight = 48.0;
  static const double buttonHeight = 50.0;
  static const double navBarHeight = 44.0;
  static const double tabBarHeight = 64.0;
  static const double screenMargin = 16.0;
  static const double screenMarginWide = 20.0;
  static const double sheetTopRadius = 14.0;
  static const double sheetHandleWidth = 36.0;
  static const double sheetHandleHeight = 5.0;
}

/// Derived semantic color roles for the Light Theme.
abstract final class AppColorsLight {
  static const Color bg = Color(0xFFF7FBFC);      // --bg
  static const Color rail = Color(0xFFECF1F3);    // --rail
  static const Color surf = Color(0xFFFCFEFE);    // --surf
  static const Color subtle = Color(0xFFF2F6F7);  // --subtle
  static const Color bd = Color(0xFFDADFE0);      // --bd
  static const Color bd2 = Color(0xFFCACFD0);     // --bd2
  static const Color line = Color(0xFFE8ECED);    // --line
  static const Color act = Color(0xFFDFE6E8);     // --act
  static const Color hov = Color(0xFFE8ECED);     // --hov
  static const Color chip = Color(0xFFD4D8D9);    // --chip
  static const Color ink = Color(0xFF191B1C);     // --ink
  static const Color ink2 = Color(0xFF313334);    // --ink2
  static const Color mut = Color(0xFF5B5E5F);     // --mut
  static const Color faint = Color(0xFF838788);   // --faint
  static const Color acc = Color(0xFF007B9B);     // --acc
  static const Color acc2 = Color(0xFF005D7C);    // --acc2
  static const Color accbg = Color(0xFFD0F6FF);   // --accbg
  static const Color onAcc = Color(0xFFFFFFFF);   // --on-acc
  static const Color ok = Color(0xFF33854A);      // --ok
  static const Color okbg = Color(0xFFDCF7E1);    // --okbg
  static const Color warn = Color(0xFFB37903);    // --warn
  static const Color warnbg = Color(0xFFFFEECD);  // --warnbg
  static const Color bad = Color(0xFFBD413F);     // --bad
  static const Color badbg = Color(0xFFFFE5E1);   // --badbg
  static const Color info = Color(0xFF3179A6);    // --info
  static const Color infobg = Color(0xFFDDF2FF);  // --infobg
}

/// Derived semantic color roles for the Dark Theme (StackBraid brand default).
abstract final class AppColorsDark {
  static const Color bg = Color(0xFF0D1011);      // --bg
  static const Color rail = Color(0xFF141717);    // --rail
  static const Color surf = Color(0xFF181B1C);    // --surf
  static const Color subtle = Color(0xFF212525);  // --subtle
  static const Color bd = Color(0xFF2E3132);      // --bd
  static const Color bd2 = Color(0xFF3D4041);     // --bd2
  static const Color line = Color(0xFF262A2A);    // --line
  static const Color act = Color(0xFF2F3435);     // --act
  static const Color hov = Color(0xFF242728);     // --hov
  static const Color chip = Color(0xFF303334);    // --chip
  static const Color ink = Color(0xFFE6E8E9);     // --ink
  static const Color ink2 = Color(0xFFCCCECF);    // --ink2
  static const Color mut = Color(0xFFA1A5A6);     // --mut
  static const Color faint = Color(0xFF777B7C);   // --faint
  static const Color acc = Color(0xFF00ADCE);     // --acc
  static const Color acc2 = Color(0xFF00C6E8);    // --acc2
  static const Color accbg = Color(0xFF00353F);   // --accbg
  static const Color onAcc = Color(0xFFFFFFFF);   // --on-acc
  static const Color ok = Color(0xFF73C385);      // --ok
  static const Color okbg = Color(0xFF1A2E1E);    // --okbg
  static const Color warn = Color(0xFFE6B55D);    // --warn
  static const Color warnbg = Color(0xFF3B2B0D);  // --warnbg
  static const Color bad = Color(0xFFF07F77);     // --bad
  static const Color badbg = Color(0xFF442321);   // --badbg
  static const Color info = Color(0xFF77B6E1);    // --info
  static const Color infobg = Color(0xFF152B3B);  // --infobg
}
