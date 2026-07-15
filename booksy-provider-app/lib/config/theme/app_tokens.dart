import 'package:flutter/material.dart';

/// Design tokens for the Provider app, derived from the Coliride visual
/// language: flat & shadowless surfaces, borders over shadows, soft 10–16px
/// corners, navy ink text, blue chrome, green success accent.
/// Kept intentionally small and self-contained (no external token package).
class AppSpacing {
  AppSpacing._();
  static const double xs = 4;
  static const double sm = 8;
  static const double md = 16;
  static const double lg = 24;
  static const double xl = 40;

  /// Interior padding of cards and list rows — the single permitted
  /// off-scale value (Coliride intra-card rhythm).
  static const double card = 12;
}

/// Motion tokens. Every animated affordance in shared components draws its
/// duration and curve from here — no inline `Duration`/curve literals.
class AppMotion {
  AppMotion._();
  static const Duration fast = Duration(milliseconds: 180);
  static const Duration medium = Duration(milliseconds: 250);
  static const Curve curve = Curves.easeOutCubic;
  static const Curve reverseCurve = Curves.easeInCubic;
}

/// Icon size ramp: sm inline, md functional, action inside a 44dp container,
/// hero for empty/feedback states.
class AppIconSize {
  AppIconSize._();
  static const double sm = 16;
  static const double md = 24;
  static const double action = 20;
  static const double hero = 72;
}

class AppRadius {
  AppRadius._();
  static const double sm = 8;
  static const double md = 12;
  static const double lg = 16;

  // Component radii from the Coliride dimension scale.
  static const double button = 10;
  static const double field = 12;
  static const double snackbar = 12;
  static const double bottomSheet = 14;
  static const double card = 15;
  static const double panel = 16;
}

class AppDimens {
  AppDimens._();
  static const double buttonHeight = 46;
  static const double buttonFontSize = 17;
  static const double fieldLabelFontSize = 14;
  static const double inputBorderWidth = 1.9;
  static const double inputFocusBorderWidth = 2.1;
}

/// Brand palette (Coliride): blue chrome for primary actions and app bars,
/// navy ink for text, green for success/selection accents.
class AppColors {
  AppColors._();

  // Brand
  static const Color primary = Color(0xFF3777BF); // blue — buttons, links
  static const Color appBar = Color(0xFF3777C0); // app bar / nav chrome
  static const Color ink = Color(0xFF4D5E80); // navy — headings & body text
  static const Color success = Color(0xFF0AC075); // green — success, checks
  static const Color mapGreen = Color(0xFF1FA96E); // map location pin
  static const Color danger = Color(0xFFFF6171);
  static const Color warning = Color(0xFFFFCB33);

  // Greys / structure
  static const Color border = Color(0xFFEBEEF3); // resting input/card borders
  static const Color borderFocus = Color(0xFFC3CAD9); // focused input border
  static const Color hint = Color(0xFFC3CAD9);
  static const Color icon = Color(0xFFD2DBEB);
  static const Color muted = Color(0xFF96A0B3); // secondary text
  static const Color divider = Color(0xFFE5E9F2);
  static const Color disabled = Color(0xFFC3CAD9); // disabled button fill
  static const Color surfaceSoft = Color(0xFFFAFAFA); // soft item fill
  static const Color primarySoft = Color(0xFFE3F2FD); // tinted icon containers
  static const Color successSoft = Color(0xFFE9FFF6); // completed-state fill

  // Overlay barriers (depth comes from dimming, never elevation).
  static const Color dialogBarrier = Color(0x24000000);
  static const Color sheetBarrier = Color(0x47000000);
}
