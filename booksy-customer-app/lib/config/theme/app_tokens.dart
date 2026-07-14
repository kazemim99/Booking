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

  static const Duration fast = Duration(milliseconds: 150);
  static const Duration normal = Duration(milliseconds: 250);
  static const Duration slow = Duration(milliseconds: 400);

  static const Curve standard = Curves.easeInOutCubic;
  static const Curve emphasized = Curves.easeOutCubic;
}

/// Minimum touch target size (accessibility baseline).
class AppTouchTarget {
  AppTouchTarget._();

  static const double min = 48;
}
