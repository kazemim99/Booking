import 'package:flutter/material.dart';

import '../../config/theme/app_colors.dart';
import '../../config/theme/app_tokens.dart';
import '../constants/app_strings.dart';
import '../utils/jalali_formatter.dart';

/// Star + rating (+ optional review count) for a provider.
///
/// The count is the provider's *published* review count
/// (openspec/changes/provider-reviews-and-ratings), so it decides whether there
/// is a rating at all: "★ 0.0 (0 نظر)" reads as a bad salon, not an unrated
/// one. [hasRating] is the single gate every surface uses, and a known zero is
/// shown as [NoReviewsYetLabel] instead.
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

  /// True only when the provider has a real rating to show. A known count
  /// decides; the rating stands in only where no count was sent.
  static bool hasRating(double rating, [int? reviewCount]) =>
      reviewCount != null ? reviewCount > 0 : rating > 0;

  /// True when the count says nobody has reviewed this provider yet. An
  /// unknown count is not "no reviews" — it is simply not shown.
  static bool isUnrated(int? reviewCount) => reviewCount == 0;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final count = reviewCount;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(Icons.star_rounded, size: iconSize, color: AppColors.star),
        const SizedBox(width: AppSpacing.xxs),
        Text(
          JalaliFormatter.toPersianDigits(rating.toStringAsFixed(1)),
          style: theme.textTheme.bodySmall,
        ),
        if (count != null && count > 0) ...[
          // «⭐ ۴.۰ · ۱ نظر»: the dot and a muted count keep the two numbers
          // apart — with only a gap, RTL read «۴.۰ ۱ نظر» as one number
          // (reviews-and-reschedule-round2 item 2).
          const SizedBox(width: AppSpacing.xs),
          ExcludeSemantics(
            child: Text(
              '·',
              key: const Key('provider-rating-separator'),
              style: theme.textTheme.bodySmall
                  ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
          ),
          const SizedBox(width: AppSpacing.xs),
          // Flexible so a narrow card at a large font scale ellipsises the
          // review count instead of overflowing its row.
          Flexible(
            child: Text(
              AppStrings.reviewCountLabel(
                JalaliFormatter.toPersianDigits('$count'),
              ),
              key: const Key('provider-rating-count'),
              style: theme.textTheme.bodySmall
                  ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
          ),
        ],
      ],
    );
  }
}

/// "هنوز نظری ندارد": what a provider with no published reviews shows where a
/// rating would be. Words, never a zero-star row.
class NoReviewsYetLabel extends StatelessWidget {
  const NoReviewsYetLabel({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Text(
      AppStrings.noReviewsYet,
      key: const Key('provider-no-reviews'),
      style: theme.textTheme.bodySmall
          ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
      maxLines: 1,
      overflow: TextOverflow.ellipsis,
    );
  }
}
