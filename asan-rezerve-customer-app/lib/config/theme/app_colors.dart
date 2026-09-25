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

  // Floating bottom navigation (on [appBar]). Labels are full white in every state (4.6:1) and the
  // active tab is told apart by weight, filled icon and [navIndicator] — never by dimming the text,
  // which put inactive labels at ~3:1. Inactive icons are an opaque pale blue at 3.17:1 on the bar.
  static const Color navLabel = Color(0xFFFFFFFF);
  static const Color navIconActive = Color(0xFFFFFFFF);
  static const Color navIconInactive = Color(0xFFC7D8EE);

  /// The pill behind the active bottom-navigation icon (white at 18% over [appBar]).
  static const Color navIndicator = Color(0x2EFFFFFF);

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

  // Input chrome. DECISION (customer-app-ux-review-fixes A.3): these deliberately diverge from the
  // provider app's Coliride values (hint #C3CAD9 at 1.64:1, icons #D2DBEB at 1.39:1), which a
  // customer cannot read. Hint text is AA (4.97:1 on white, 4.77:1 on [surfaceSoft]) and field
  // icons are ≥ 3:1 on both (3.47 / 3.33).
  static const Color hint = Color(0xFF667085); // Field hint text
  static const Color fieldIcon = Color(0xFF808A9E); // Field prefix/suffix icons

  /// Decorative only (empty-state illustrations) — never a field icon or anything that must be read.
  static const Color iconMuted = Color(0xFFD2DBEB);

  // Semantic Colors — base tones for icons / fills / badges (white text on top)
  static const Color success = Color(0xFF0AC075); // Green - booking confirmed
  static const Color warning = Color(0xFFFFCB33); // Amber/yellow accent - fills only

  /// Every filled rating star, display and input alike. A dark gold at 3.33:1 on white (3.19:1 on [surfaceSoft]):
  /// a star is a graphic that carries the rating, and the yellow [warning] fill was 1.52:1.
  static const Color star = Color(0xFFB98300);
  /// Coral — badges and fills only. White on it is 2.92:1, so it never carries text or backs a
  /// button: `colorScheme.error` and destructive buttons use [errorText].
  static const Color error = Color(0xFFFF6171);
  static const Color info = Color(0xFF3777BF); // Blue - informational (unified)

  /// Input error border only — the darker Coliride red used for field borders,
  /// never [error] (which stays on badges and fills).
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

  // Selection / active-state accent — green, matching Coliride. It is 2.38:1 on white, so it only
  // fills large shapes; anything that must be read or seen uses a darker/lighter partner below.
  static const Color accent = Color(0xFF0AC075);

  /// The AA green (5.48:1 on white): tab labels, the tab indicator, selected checkboxes/switches.
  static const Color accentStrong = successText;

  /// `secondaryContainer`: a light green tint (selected segment, map notice, progress track) with
  /// [onAccentContainer] text at 6.63:1 — so nothing falls back to the green [accent].
  static const Color accentContainer = Color(0xFFD7F5E9);
  static const Color onAccentContainer = Color(0xFF065F46);

  /// Snack-bar action on the navy snack bar (5.07:1); the green [accent] there was 2.73:1.
  static const Color accentOnDark = Color(0xFFA7F3D0);
}
