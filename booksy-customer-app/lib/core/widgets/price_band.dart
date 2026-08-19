import 'package:flutter/material.dart';

import '../../config/theme/app_text_styles.dart';
import '../constants/app_strings.dart';

/// A provider's relative price level, rendered as `$` / `$$` / `$$$`.
///
/// **The backend does not compute a price band.** There is no field for it on
/// any provider payload, so the band shown here is derived on the client from
/// the provider's *own* service prices via [PriceBandX.fromPrices]. When a
/// provider has no priced service — which is the common case for freshly
/// seeded data — the derivation returns `null` and callers must render nothing
/// rather than a fabricated band.
enum PriceBand {
  low,
  mid,
  high;

  /// Derives a band from a provider's service prices.
  ///
  /// Returns `null` when there is nothing to derive from (no services, or every
  /// price is zero/absent) — the caller then hides the element entirely.
  ///
  /// The band is taken from the **median** price so a single very cheap add-on
  /// or one premium package cannot drag a salon into the wrong band. Thresholds
  /// are presentation-only, expressed in the price unit the provider's own
  /// services use (Toman for the Iranian market), and deliberately live in one
  /// place: when the backend starts publishing a real band, delete this
  /// derivation and map the field instead.
  static PriceBand? fromPrices(Iterable<double> prices) {
    final priced = prices.where((p) => p > 0).toList()..sort();
    if (priced.isEmpty) return null;

    final middle = priced.length ~/ 2;
    final median = priced.length.isOdd
        ? priced[middle]
        : (priced[middle - 1] + priced[middle]) / 2;
    if (median <= _midThreshold) return PriceBand.low;
    if (median <= _highThreshold) return PriceBand.mid;
    return PriceBand.high;
  }

  static const double _midThreshold = 200000;
  static const double _highThreshold = 600000;

  String get label => switch (this) {
        PriceBand.low => AppStrings.priceBandLow,
        PriceBand.mid => AppStrings.priceBandMid,
        PriceBand.high => AppStrings.priceBandHigh,
      };
}

/// Renders a [PriceBand] as compact inline text. Rendering is the caller's
/// choice: pass a non-null band only when one could actually be derived.
class PriceBandLabel extends StatelessWidget {
  final PriceBand band;

  const PriceBandLabel({super.key, required this.band});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Text(
      band.label,
      // Latin glyphs inside an RTL paragraph: forced LTR so `$$` never
      // reorders against neighbouring Persian text.
      textDirection: TextDirection.ltr,
      style: theme.textTheme.bodySmall?.copyWith(
        fontWeight: AppTextStyles.semibold,
      ),
    );
  }
}
