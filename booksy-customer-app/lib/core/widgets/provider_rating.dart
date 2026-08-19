import 'package:flutter/material.dart';

import '../../config/theme/app_colors.dart';
import '../../config/theme/app_tokens.dart';
import '../constants/app_strings.dart';
import '../utils/jalali_formatter.dart';

/// Star + rating (+ optional review count) for a provider.
///
/// Every seeded provider currently has `averageRating == 0` and
/// `totalReviews == 0`. Showing "★ 0.0 (0 نظر)" reads as a *bad* salon rather
/// than an unrated one, so [hasRating] is the single gate every surface uses:
/// when there is no rating yet, nothing is rendered at all.
class ProviderRating extends StatelessWidget {
  final double rating;
  final int? reviewCount;

  /// Icon/text size. Defaults to the inline ramp value.
  final double iconSize;

  const ProviderRating({
    super.key,
    required this.rating,
    this.reviewCount,
    this.iconSize = AppIconSize.sm,
  });

  /// True only when the provider has a real rating to show.
  static bool hasRating(double rating, [int? reviewCount]) =>
      rating > 0 || (reviewCount ?? 0) > 0;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final count = reviewCount;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(Icons.star_rounded, size: iconSize, color: AppColors.warning),
        const SizedBox(width: AppSpacing.xxs),
        Text(
          JalaliFormatter.toPersianDigits(rating.toStringAsFixed(1)),
          style: theme.textTheme.bodySmall,
        ),
        if (count != null && count > 0) ...[
          const SizedBox(width: AppSpacing.xxs),
          // Flexible so a narrow card at a large font scale ellipsises the
          // review count instead of overflowing its row.
          Flexible(
            child: Text(
              AppStrings.reviewCountLabel(
                JalaliFormatter.toPersianDigits('$count'),
              ),
              style: theme.textTheme.bodySmall,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
          ),
        ],
      ],
    );
  }
}
