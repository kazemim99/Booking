import 'dart:math' as math;
import 'dart:ui' show Color;

/// WCAG 2.x contrast thresholds.
const double kAaText = 4.5;
const double kAaNonText = 3.0;

/// Relative luminance (WCAG 2.x) of an opaque colour.
double relativeLuminance(Color c) {
  double channel(double v) {
    // Round to the 8-bit value first so the ratio matches what a checker computes from the hex.
    final s = (v * 255).roundToDouble() / 255.0;
    return s <= 0.03928
        ? s / 12.92
        : math.pow((s + 0.055) / 1.055, 2.4).toDouble();
  }

  return 0.2126 * channel(c.r) + 0.7152 * channel(c.g) + 0.0722 * channel(c.b);
}

/// WCAG contrast ratio of [foreground] drawn on [background].
///
/// A translucent foreground is composited over the background first (that is what reaches the screen); the
/// background itself must be opaque.
double contrastRatio(Color foreground, Color background) {
  assert(background.a == 1.0, 'contrast needs an opaque background');
  final fg = Color.alphaBlend(foreground, background);
  final a = relativeLuminance(fg);
  final b = relativeLuminance(background);
  return (math.max(a, b) + 0.05) / (math.min(a, b) + 0.05);
}
