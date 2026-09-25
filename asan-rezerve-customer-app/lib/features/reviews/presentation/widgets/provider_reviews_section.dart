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
/// a loading failure, and inventing filler stars would be worse. Under the
/// heading, a line says where a review is written: from a completed visit, in
/// «نوبت‌ها».
class ProviderReviewsSection extends StatelessWidget {
  final ProviderReviews? reviews;

  /// Null while they are still loading.
  final bool loading;

  /// A reader's helpful / not-helpful vote. Null leaves the counts visible but
  /// nothing tappable; the page decides what a guest's tap means.
  final void Function(Review review, bool helpful)? onVote;

  const ProviderReviewsSection({
    super.key,
    this.reviews,
    this.loading = false,
    this.onVote,
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
        // Where a review is written, for guests and customers alike: only from a completed appointment, which
        // nothing on the profile said (QA recording 2026-09-23 #10, "where do I leave my review?").
        const SizedBox(height: AppSpacing.xxs),
        Text(
          AppStrings.reviewsHowToWrite,
          key: const Key('provider-reviews-how-to'),
          style: theme.textTheme.bodySmall
              ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
        ),
        if (data != null && data.dimensions.isNotEmpty) ...[
          const SizedBox(height: AppSpacing.xs),
          // Only the dimensions somebody rated: a zero bar would read as a bad score.
          for (final entry in data.dimensions.entries)
            Padding(
              key: Key('review-breakdown-${entry.key.name}'),
              padding: const EdgeInsets.only(bottom: AppSpacing.xxs),
              child: Row(
                children: [
                  Expanded(
                    flex: 2,
                    child: Text(entry.key.label, style: theme.textTheme.bodySmall),
                  ),
                  Expanded(
                    flex: 3,
                    child: LinearProgressIndicator(
                      value: (entry.value.average / 5).clamp(0, 1),
                      minHeight: 6,
                      borderRadius: BorderRadius.circular(AppRadius.sm),
                    ),
                  ),
                  const SizedBox(width: AppSpacing.xs),
                  Text(
                    JalaliFormatter.toPersianDigits(
                        entry.value.average.toStringAsFixed(1)),
                    style: theme.textTheme.bodySmall,
                  ),
                ],
              ),
            ),
        ],
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
                  _VoteRow(review: review, onVote: onVote),
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

/// "مفید بود / مفید نبود" with the counts, the reader's own vote filled in.
class _VoteRow extends StatelessWidget {
  final Review review;
  final void Function(Review review, bool helpful)? onVote;

  const _VoteRow({required this.review, this.onVote});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final vote = onVote;
    Widget control(bool helpful) {
      final mine = review.myVote ==
          (helpful ? ReviewVote.helpful : ReviewVote.notHelpful);
      final count = helpful ? review.helpfulCount : review.notHelpfulCount;
      return Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          IconButton(
            key: Key('review-${review.id}-${helpful ? 'helpful' : 'not-helpful'}'),
            tooltip: helpful ? AppStrings.reviewHelpful : AppStrings.reviewNotHelpful,
            visualDensity: VisualDensity.compact,
            onPressed: vote == null ? null : () => vote(review, helpful),
            icon: Icon(
              helpful
                  ? (mine ? Icons.thumb_up : Icons.thumb_up_outlined)
                  : (mine ? Icons.thumb_down : Icons.thumb_down_outlined),
              size: AppIconSize.sm,
              color: mine ? theme.colorScheme.primary : null,
            ),
          ),
          Text(JalaliFormatter.toPersianDigits('$count'),
              style: theme.textTheme.bodySmall),
        ],
      );
    }

    return Row(
      children: [control(true), const SizedBox(width: AppSpacing.sm), control(false)],
    );
  }
}
