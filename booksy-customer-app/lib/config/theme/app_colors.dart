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
  static const Color textTertiary = Color(0xFF718096); // Adjusted for placeholders/disabled

  // Semantic Colors (minimal, functional only)
  static const Color success = Color(0xFF059669); // Green - booking confirmed
  static const Color warning = Color(0xFFD97706); // Amber - reminders
  static const Color error = Color(0xFFDC2626); // Red - errors, cancellations
  static const Color info = Color(0xFF0284C7); // Blue - informational

  // Transparent overlays
  static const Color overlay = Color(0x33000000); // 20% black
  static const Color divider = Color(0x1F000000); // 12% black
  static const Color shadowLight = Color(0x0F000000); // 6% black for subtle shadows

  // Accent color for active states (single point of color)
  static const Color accent = Color(0xFF1976D2); // Trust blue, Divar-style
}
