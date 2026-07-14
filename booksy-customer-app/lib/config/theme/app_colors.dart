import 'package:flutter/material.dart';

/// App color palette following professional, gender-neutral design system
/// Single primary color (dark blue) with neutral tones
class AppColors {
  AppColors._();

  // Primary Color - Dark Blue (Professional, trustworthy, gender-neutral)
  static const Color primary = Color(0xFF1A365D);
  static const Color primaryTint = Color(0xFF2D4A7C); // Hover state
  static const Color primaryShade = Color(0xFF0F2744); // Pressed state

  // Neutral Palette
  static const Color background = Color(0xFFFAFAFA); // Off-white for better card separation
  static const Color backgroundDark = Color(0xFF0F1419);
  static const Color surface = Color(0xFFFFFFFF); // Pure white for cards
  static const Color surfaceDark = Color(0xFF1A1F25);
  static const Color border = Color(0xFFE2E8F0);
  static const Color borderSubtle = Color(0xFFEDF2F7); // Lighter border for cards

  // Text Colors (improved contrast hierarchy)
  static const Color textPrimary = Color(0xFF1A202C); // rgba(0,0,0,0.87) equivalent
  static const Color textPrimaryDark = Color(0xFFF7FAFC);
  static const Color textSecondary = Color(0xFF4A5568); // Darker for better contrast (was #718096)
  static const Color textTertiary = Color(0xFF667085); // 4.77:1 on background — WCAG AA for normal text

  // Semantic Colors — base tones for icons and large UI elements (≥3:1)
  static const Color success = Color(0xFF059669); // Green - booking confirmed
  static const Color warning = Color(0xFFD97706); // Amber - reminders
  static const Color error = Color(0xFFDC2626); // Red - errors, cancellations
  static const Color info = Color(0xFF0284C7); // Blue - informational

  // Semantic text variants — ≥4.5:1 on white and on their tint backgrounds.
  // Use these (not the base tones) whenever semantic color carries text.
  static const Color successText = Color(0xFF047857);
  static const Color warningText = Color(0xFFB45309);
  static const Color errorText = Color(0xFFB91C1C);
  static const Color infoText = Color(0xFF0369A1);

  // Semantic tint backgrounds (badges, banners)
  static const Color successTint = Color(0xFFECFDF5);
  static const Color warningTint = Color(0xFFFFFBEB);
  static const Color errorTint = Color(0xFFFEF2F2);
  static const Color infoTint = Color(0xFFF0F9FF);

  // Transparent overlays
  static const Color overlay = Color(0x33000000); // 20% black
  static const Color divider = Color(0x1F000000); // 12% black
  static const Color shadowLight = Color(0x0F000000); // 6% black for subtle shadows

  // Accent color for active states (single point of color)
  static const Color accent = Color(0xFF1976D2); // Trust blue, Divar-style
}
