import 'package:flutter/material.dart';

/// Design tokens for the Provider app.
/// Kept intentionally small and self-contained (no external token package).
class AppSpacing {
  AppSpacing._();
  static const double xs = 4;
  static const double sm = 8;
  static const double md = 16;
  static const double lg = 24;
  static const double xl = 40;
}

class AppRadius {
  AppRadius._();
  static const double sm = 8;
  static const double md = 12;
  static const double lg = 16;
}

/// Brand palette. Provider surfaces use a business-oriented green/teal accent
/// (matching the Vue provider registration branding: `#10b981`).
class AppColors {
  AppColors._();
  static const Color primary = Color(0xFF10B981); // emerald-500
  static const Color primaryDark = Color(0xFF059669);
  static const Color danger = Color(0xFFEF4444);
  static const Color warning = Color(0xFFF59E0B);
}
