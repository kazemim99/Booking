import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/jalali_formatter.dart';
import '../../../../core/widgets/provider_rating.dart';
import '../../domain/entities/review.dart';

/// "نظرها" on a salon's profile: the average with how many people rated, then
/// what they wrote (openspec/changes/customer-app-discovery-pass).
///
/// A salon nobody has reviewed says so plainly — an empty section would read as
/// a loading failure, and inventing filler stars would be worse.
class ProviderReviewsSection extends StatelessWidget {
  final ProviderReviews? reviews;

  /// Null while they are still loading.
  final bool loading;

  const ProviderReviewsSection({
    super.key,
    this.reviews,
    this.loading = false,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final data = reviews;

    return Column(
      key: const Key('provider-reviews-section'),
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Expanded(
              child: Text(AppStrings.reviewsTitle, style: theme.textTheme.titleLarge),
            ),
            if (data != null && data.totalReviews > 0)
              ProviderRating(
                rating: data.averageRating,
                reviewCount: data.totalReviews,
              ),
          ],
        ),
        const SizedBox(height: AppSpacing.sm),
        if (loading)
          const Padding(
            padding: EdgeInsets.symmetric(vertical: AppSpacing.md),
            child: Center(child: CircularProgressIndicator()),
          )
        else if (data == null || data.items.isEmpty)
          Text(
            AppStrings.reviewsEmpty,
            key: const Key('provider-reviews-empty'),
            style: theme.textTheme.bodyMedium
                ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
          )
        else
          for (final review in data.items)
            Padding(
              key: Key('review-${review.id}'),
              padding: const EdgeInsets.only(bottom: AppSpacing.md),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          review.customerName.trim().isEmpty
                              ? AppStrings.reviewAnonymous
                              : review.customerName,
                          style: theme.textTheme.titleSmall,
                        ),
                      ),
                      ProviderRating(rating: review.rating),
                    ],
                  ),
                  if (review.createdAt != null)
                    Text(
                      JalaliFormatter.formatShortDate(review.createdAt!),
                      style: theme.textTheme.bodySmall
                          ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                    ),
                  if (review.comment != null && review.comment!.isNotEmpty) ...[
                    const SizedBox(height: AppSpacing.xxs),
                    Text(review.comment!, style: theme.textTheme.bodyMedium),
                  ],
                  if (review.providerResponse != null &&
                      review.providerResponse!.isNotEmpty) ...[
                    const SizedBox(height: AppSpacing.xs),
                    Container(
                      padding: const EdgeInsets.all(AppSpacing.sm),
                      decoration: BoxDecoration(
                        color: theme.colorScheme.surfaceContainerHighest,
                        borderRadius: BorderRadius.circular(AppRadius.md),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            AppStrings.reviewProviderReply,
                            style: theme.textTheme.bodySmall?.copyWith(
                              color: theme.colorScheme.onSurfaceVariant,
                            ),
                          ),
                          Text(review.providerResponse!,
                              style: theme.textTheme.bodyMedium),
                        ],
                      ),
                    ),
                  ],
                ],
              ),
            ),
      ],
    );
  }
}
