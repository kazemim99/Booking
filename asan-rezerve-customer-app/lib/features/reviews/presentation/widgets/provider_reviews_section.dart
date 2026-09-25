import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/jalali_formatter.dart';
import '../../../../core/widgets/app_button.dart';
import '../../../../core/widgets/provider_rating.dart';
import '../../domain/entities/review.dart';

/// "نظرها" on a salon's profile: the average with how many people rated and
/// how the stars fall, then what they wrote (openspec/changes/customer-app-discovery-pass,
/// openspec/changes/_inline/customer-reviews-and-nahal-seed).
///
/// A salon nobody has reviewed says so plainly — an empty section would read as
/// a loading failure, and inventing filler stars would be worse. A failed read
/// says THAT, with a retry, rather than claiming the salon has no reviews.
/// Three reviews show first so the profile stays short; the rest are one tap
/// away, and more are read from the server as the customer asks for them.
class ProviderReviewsSection extends StatefulWidget {
  final ProviderReviews? reviews;

  /// Null while they are still loading.
  final bool loading;

  /// The reviews could not be read.
  final bool failed;

  /// The next page is on its way.
  final bool loadingMore;

  /// Read them again after a failure.
  final VoidCallback? onRetry;

  /// Read the next page, when [ProviderReviews.hasMore].
  final VoidCallback? onLoadMore;

  /// A reader's helpful / not-helpful vote. Null leaves the counts visible but
  /// nothing tappable; the page decides what a guest's tap means.
  final void Function(Review review, bool helpful)? onVote;

  /// How many reviews show before «مشاهده همه نظرها».
  static const collapsedCount = 3;

  const ProviderReviewsSection({
    super.key,
    this.reviews,
    this.loading = false,
    this.failed = false,
    this.loadingMore = false,
    this.onRetry,
    this.onLoadMore,
    this.onVote,
  });

  @override
  State<ProviderReviewsSection> createState() => _ProviderReviewsSectionState();
}

class _ProviderReviewsSectionState extends State<ProviderReviewsSection> {
  bool _expanded = false;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final data = widget.reviews;
    final muted = theme.textTheme.bodySmall
        ?.copyWith(color: theme.colorScheme.onSurfaceVariant);

    final items = data?.items ?? const <Review>[];
    final shown = _expanded
        ? items
        : items.take(ProviderReviewsSection.collapsedCount).toList();
    final hiddenCount = (data?.totalReviews ?? items.length) - shown.length;

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
          style: muted,
        ),
        if (data != null && data.totalReviews >= 3 && data.distribution.isNotEmpty) ...[
          const SizedBox(height: AppSpacing.sm),
          _Distribution(data: data),
        ],
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
        if (widget.loading)
          const Padding(
            padding: EdgeInsets.symmetric(vertical: AppSpacing.md),
            child: Center(child: CircularProgressIndicator()),
          )
        else if (widget.failed && items.isEmpty)
          Row(
            key: const Key('provider-reviews-failed'),
            children: [
              Expanded(
                child: Text(AppStrings.reviewsLoadFailed,
                    style: theme.textTheme.bodyMedium
                        ?.copyWith(color: theme.colorScheme.onSurfaceVariant)),
              ),
              if (widget.onRetry case final retry?)
                AppButton.text(
                  key: const Key('provider-reviews-retry'),
                  label: AppStrings.retry,
                  onPressed: retry,
                ),
            ],
          )
        else if (items.isEmpty)
          Text(
            AppStrings.reviewsEmpty,
            key: const Key('provider-reviews-empty'),
            style: theme.textTheme.bodyMedium
                ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
          )
        else ...[
          for (final review in shown)
            _ReviewTile(review: review, onVote: widget.onVote),
          if (!_expanded && hiddenCount > 0)
            AppButton.secondary(
              key: const Key('provider-reviews-show-all'),
              label: AppStrings.reviewsShowAll(JalaliFormatter.toPersianDigits(
                  '${data?.totalReviews ?? items.length}')),
              onPressed: () => setState(() => _expanded = true),
            )
          else if (_expanded && (data?.hasMore ?? false))
            AppButton.secondary(
              key: const Key('provider-reviews-more'),
              label: AppStrings.reviewsMore,
              loading: widget.loadingMore,
              onPressed: widget.loadingMore ? null : widget.onLoadMore,
            ),
        ],
      ],
    );
  }
}

/// How the stars fall, 5 down to 1: the shape one average hides.
class _Distribution extends StatelessWidget {
  final ProviderReviews data;

  const _Distribution({required this.data});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final total = data.distribution.values.fold<int>(0, (a, b) => a + b);
    return Column(
      key: const Key('review-distribution'),
      children: [
        for (var star = 5; star >= 1; star--)
          Semantics(
            label: AppStrings.reviewDistributionLabel(
              JalaliFormatter.toPersianDigits('$star'),
              JalaliFormatter.toPersianDigits('${data.distribution[star] ?? 0}'),
            ),
            excludeSemantics: true,
            child: Padding(
              padding: const EdgeInsets.only(bottom: AppSpacing.xxs),
              child: Row(
                children: [
                  SizedBox(
                    width: 28,
                    child: Text(JalaliFormatter.toPersianDigits('$star'),
                        style: theme.textTheme.bodySmall),
                  ),
                  Icon(Icons.star_rounded, size: 14, color: theme.colorScheme.primary),
                  const SizedBox(width: AppSpacing.xs),
                  Expanded(
                    child: LinearProgressIndicator(
                      value: total == 0 ? 0 : (data.distribution[star] ?? 0) / total,
                      minHeight: 6,
                      borderRadius: BorderRadius.circular(AppRadius.sm),
                    ),
                  ),
                  const SizedBox(width: AppSpacing.xs),
                  SizedBox(
                    width: 28,
                    child: Text(
                      JalaliFormatter.toPersianDigits('${data.distribution[star] ?? 0}'),
                      style: theme.textTheme.bodySmall,
                      textAlign: TextAlign.end,
                    ),
                  ),
                ],
              ),
            ),
          ),
      ],
    );
  }
}

/// One review: who (an initial, the name the listing gives — «مریم ر.» — and
/// that it followed a real visit), when, the stars, the words, the salon's
/// answer, and the votes.
class _ReviewTile extends StatelessWidget {
  final Review review;
  final void Function(Review review, bool helpful)? onVote;

  const _ReviewTile({required this.review, this.onVote});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final muted = theme.textTheme.bodySmall
        ?.copyWith(color: theme.colorScheme.onSurfaceVariant);
    final name = review.customerName.trim().isEmpty
        ? AppStrings.reviewAnonymous
        : review.customerName.trim();

    return Padding(
      key: Key('review-${review.id}'),
      padding: const EdgeInsets.only(bottom: AppSpacing.md),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              ExcludeSemantics(
                child: CircleAvatar(
                  radius: 18,
                  backgroundColor: theme.colorScheme.primaryContainer,
                  foregroundColor: theme.colorScheme.onPrimaryContainer,
                  child: Text(name.characters.first, style: theme.textTheme.titleSmall),
                ),
              ),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(name, style: theme.textTheme.titleSmall),
                    Wrap(
                      spacing: AppSpacing.xs,
                      crossAxisAlignment: WrapCrossAlignment.center,
                      children: [
                        if (review.createdAt != null)
                          Text(JalaliFormatter.formatShortDate(review.createdAt!), style: muted),
                        // Inline, so at large text it wraps instead of overflowing beside the stars.
                        if (review.isVerified)
                          Text.rich(
                            key: Key('review-${review.id}-verified'),
                            TextSpan(children: [
                              WidgetSpan(
                                alignment: PlaceholderAlignment.middle,
                                child: Icon(Icons.verified_outlined,
                                    size: 14, color: theme.colorScheme.primary),
                              ),
                              const TextSpan(text: ' ${AppStrings.reviewVerifiedVisit}'),
                            ]),
                            style: muted,
                          ),
                      ],
                    ),
                  ],
                ),
              ),
              ProviderRating(rating: review.rating),
            ],
          ),
          if (review.comment != null && review.comment!.isNotEmpty) ...[
            const SizedBox(height: AppSpacing.xs),
            Text(review.comment!, style: theme.textTheme.bodyMedium),
          ],
          _VoteRow(review: review, onVote: onVote),
          if (review.providerResponse != null &&
              review.providerResponse!.isNotEmpty)
            Container(
              key: Key('review-${review.id}-reply'),
              width: double.infinity,
              padding: const EdgeInsets.all(AppSpacing.sm),
              decoration: BoxDecoration(
                color: theme.colorScheme.surfaceContainerHighest,
                borderRadius: BorderRadius.circular(AppRadius.md),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(Icons.storefront_outlined, size: 16, color: theme.colorScheme.onSurfaceVariant),
                      const SizedBox(width: AppSpacing.xxs),
                      Text(AppStrings.reviewProviderReply, style: muted),
                    ],
                  ),
                  const SizedBox(height: AppSpacing.xxs),
                  Text(review.providerResponse!,
                      style: theme.textTheme.bodyMedium),
                ],
              ),
            ),
        ],
      ),
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
