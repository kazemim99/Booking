import 'package:flutter/material.dart';

import '../../config/theme/app_tokens.dart';
import '../constants/app_strings.dart';
import '../utils/jalali_formatter.dart';
import 'price_band.dart';
import 'provider_rating.dart';

/// The one-line provider summary used on cards and on the profile header:
/// category · rating (review count) · price band · distance.
///
/// Every part is optional because the backend does not yet publish all of them
/// (there is no price-band field, ratings/review counts are zero for seeded
/// providers, and the search payload carries no distance). A part that has no
/// value is dropped along with its separator, and when nothing at all can be
/// shown the widget collapses to zero height rather than leaving a row of
/// placeholders or fabricated numbers.
class ProviderMetaLine extends StatelessWidget {
  /// Category (or, on the profile header, the city standing in for it).
  final String? category;
  final double? rating;
  final int? reviewCount;
  final PriceBand? priceBand;

  /// Distance in kilometres, when the response provided one.
  final double? distanceKm;

  const ProviderMetaLine({
    super.key,
    this.category,
    this.rating,
    this.reviewCount,
    this.priceBand,
    this.distanceKm,
  });

  /// Whether this configuration would render anything at all.
  bool get hasContent =>
      (category != null && category!.isNotEmpty) ||
      ProviderRating.hasRating(rating ?? 0, reviewCount) ||
      priceBand != null ||
      distanceKm != null;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final parts = <Widget>[];

    if (category != null && category!.isNotEmpty) {
      parts.add(Text(category!, style: theme.textTheme.bodySmall));
    }
    if (ProviderRating.hasRating(rating ?? 0, reviewCount)) {
      parts.add(ProviderRating(rating: rating!, reviewCount: reviewCount));
    }
    if (priceBand != null) {
      parts.add(PriceBandLabel(band: priceBand!));
    }
    if (distanceKm != null) {
      parts.add(
        Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              Icons.location_on_outlined,
              size: AppIconSize.sm,
              color: theme.colorScheme.onSurfaceVariant,
            ),
            const SizedBox(width: AppSpacing.xxs),
            // Flexible: a Wrap constrains each part to the line width, and at
            // large font scales the label alone can exceed a narrow card.
            Flexible(
              child: Text(
                AppStrings.distanceKmLabel(
                  JalaliFormatter.toPersianDigits(
                    distanceKm!.toStringAsFixed(1),
                  ),
                ),
                style: theme.textTheme.bodySmall,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            ),
          ],
        ),
      );
    }

    if (parts.isEmpty) return const SizedBox.shrink();

    return Wrap(
      spacing: AppSpacing.xs,
      runSpacing: AppSpacing.xxs,
      crossAxisAlignment: WrapCrossAlignment.center,
      children: [
        for (var i = 0; i < parts.length; i++) ...[
          if (i > 0)
            Text(
              '·',
              style: theme.textTheme.bodySmall?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
          parts[i],
        ],
      ],
    );
  }
}
