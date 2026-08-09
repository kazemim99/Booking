import 'package:flutter/material.dart';

/// App color palette — aligned to the Provider app's Coliride visual language
/// (blue chrome for primary actions & app bars, navy ink for text, green for
/// success/selection accents). Flat surfaces separated by borders, not shadows.
///
/// Field names are preserved from the previous palette so no call site changes;
/// only values move (a few additive fields support the aligned theme).
class AppColors {
  AppColors._();

  // Primary — Coliride blue (buttons, links, primary chrome)
  static const Color primary = Color(0xFF3777BF);
  static const Color primaryTint = Color(0xFF5B93CE); // Hover / lighter state
  static const Color primaryShade = Color(0xFF2E6299); // Pressed / darker state

  /// App-bar / top-chrome blue (a hair different from [primary], per Coliride).
  static const Color appBar = Color(0xFF3777C0);

  // Neutral Palette
  static const Color background = Color(0xFFFFFFFF); // White scaffold (flat look)
  static const Color backgroundDark = Color(0xFF0F1419);
  static const Color surface = Color(0xFFFFFFFF); // White cards
  static const Color surfaceDark = Color(0xFF1A1F25);
  static const Color surfaceSoft = Color(0xFFFAFAFA); // Soft item fill
  static const Color border = Color(0xFFEBEEF3); // Resting card/input borders
  static const Color borderSubtle = Color(0xFFEBEEF3); // Card borders
  static const Color borderFocus = Color(0xFFC3CAD9); // Focused input border

  // Text Colors — navy ink family (AA-verified on white)
  static const Color textPrimary = Color(0xFF4D5E80); // Navy ink — 6.5:1 on white
  static const Color textPrimaryDark = Color(0xFFF7FAFC);
  static const Color textSecondary = Color(0xFF5A6B8C); // Navy-grey — 5.4:1 (AA)
  static const Color textTertiary = Color(0xFF667085); // 4.77:1 on white (AA)

  // Input chrome (Coliride)
  static const Color hint = Color(0xFFC3CAD9); // Field hint text
  static const Color iconMuted = Color(0xFFD2DBEB); // Field prefix/suffix icons

  // Semantic Colors — base tones for icons / fills / badges (white text on top)
  static const Color success = Color(0xFF0AC075); // Green - booking confirmed
  static const Color warning = Color(0xFFFFCB33); // Amber/yellow accent - fills only
  static const Color error = Color(0xFFFF6171); // Coral - errors, cancellations
  static const Color info = Color(0xFF3777BF); // Blue - informational (unified)

  /// Input error border only — the darker Coliride red used for field borders,
  /// never [error] (which stays on badges/toasts/buttons).
  static const Color inputErrorBorder = Color(0xFFE74A3B);

  // Semantic text variants — ≥4.5:1 on white and on their tint backgrounds.
  // Use these (not the base tones) whenever semantic color carries text.
  static const Color successText = Color(0xFF047857);
  static const Color warningText = Color(0xFFB45309);
  static const Color errorText = Color(0xFFB91C1C);
  static const Color infoText = Color(0xFF0369A1);

  // Semantic tint backgrounds (badges, banners)
  static const Color successTint = Color(0xFFE9FFF6);
  static const Color warningTint = Color(0xFFFFFBEB);
  static const Color errorTint = Color(0xFFFEF2F2);
  static const Color infoTint = Color(0xFFF0F9FF);

  // Transparent overlays / barriers (depth from dimming, never elevation)
  static const Color overlay = Color(0x33000000); // 20% black
  static const Color divider = Color(0xFFE5E9F2); // Coliride hairline divider
  static const Color shadowLight = Color(0x0F000000); // Retained; unused (flat)

  // Selection / active-state accent — green, matching Coliride (checks, tabs).
  static const Color accent = Color(0xFF0AC075);
}
