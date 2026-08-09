import 'package:flutter/animation.dart';

/// Spacing scale (4dp base grid).
/// Use these for all padding, margins, and gaps — never arbitrary numbers.
class AppSpacing {
  AppSpacing._();

  static const double xxs = 4;
  static const double xs = 8;
  static const double sm = 12;
  static const double md = 16;
  static const double lg = 24;
  static const double xl = 32;
  static const double xxl = 48;
}

/// Corner radius scale.
class AppRadius {
  AppRadius._();

  static const double sm = 8;
  static const double md = 12;
  static const double lg = 16;
  static const double xl = 24;

  /// Fully rounded (chips, pills, badges).
  static const double full = 999;

  // Component radii aligned to the Provider (Coliride) dimension scale.
  static const double button = 10;
  static const double field = 12;
  static const double snackbar = 12;
  static const double bottomSheet = 14;
  static const double card = 15;
  static const double panel = 16; // dialogs, floating nav pill
}

/// Elevation levels (Material 3 dp values).
class AppElevation {
  AppElevation._();

  static const double none = 0;
  static const double low = 1;
  static const double medium = 3;
  static const double high = 6;
}

/// Motion durations and curves.
/// All animated components must honor MediaQuery.disableAnimations.
class AppMotion {
  AppMotion._();

  static const Duration fast = Duration(milliseconds: 180);
  static const Duration normal = Duration(milliseconds: 250);
  static const Duration slow = Duration(milliseconds: 400);

  static const Curve standard = Curves.easeInOutCubic;
  static const Curve emphasized = Curves.easeOutCubic;
}

/// Icon-size ramp aligned to the Provider app: [sm] inline, [md] functional,
/// [action] inside a tap container, [hero] for empty/feedback illustrations.
class AppIconSize {
  AppIconSize._();

  static const double sm = 16;
  static const double action = 20;
  static const double md = 24;
  static const double hero = 72;
}

/// Minimum touch target size (accessibility baseline).
class AppTouchTarget {
  AppTouchTarget._();

  static const double min = 48;
}
