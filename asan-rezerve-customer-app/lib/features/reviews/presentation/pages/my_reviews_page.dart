import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/utils/jalali_formatter.dart';
import '../../../../core/widgets/provider_rating.dart';
import '../../domain/entities/review.dart';
import '../../domain/repositories/review_repository.dart';
import '../widgets/write_review_dialog.dart';

/// "نظرهای من": every review the customer wrote, in any state, with the
/// administrator's reason where one was given (customer-profile › Review
/// Management). A review awaiting approval is marked as such — never shown as
/// though it were live.
class MyReviewsPage extends StatefulWidget {
  /// Repository override for tests; resolved from DI in the app.
  final ReviewRepository? repository;

  const MyReviewsPage({super.key, this.repository});

  @override
  State<MyReviewsPage> createState() => _MyReviewsPageState();
}

class _MyReviewsPageState extends State<MyReviewsPage> {
  late final ReviewRepository _repository =
      widget.repository ?? getIt<ReviewRepository>();
  List<MyReview>? _reviews;
  String? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    final result = await _repository.getMyReviews();
    if (!mounted) return;
    setState(() {
      _loading = false;
      result.fold((f) => _error = f.message, (r) => _reviews = r);
    });
  }

  Future<void> _edit(MyReview review) async {
    final draft = await showWriteReviewDialog(
      context,
      initial: ReviewDraft(
        rating: review.rating,
        comment: review.comment,
        dimensions: review.dimensions,
      ),
    );
    if (draft == null || !mounted) return;
    final result = await _repository.editReview(
      reviewId: review.id,
      rating: draft.rating,
      comment: draft.comment,
      dimensions: draft.dimensions,
    );
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
      content: Text(result.fold((f) => f.message, (_) => AppStrings.reviewEdited)),
    ));
    if (result.isRight()) await _load();
  }

  @override
  Widget build(BuildContext context) {
    final reviews = _reviews;
    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.myReviewsTitle)),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(_error!),
                      const SizedBox(height: AppSpacing.sm),
                      TextButton(
                        key: const Key('my-reviews-retry'),
                        onPressed: _load,
                        child: const Text(AppStrings.retry),
                      ),
                    ],
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _load,
                  child: SingleChildScrollView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: const EdgeInsets.all(AppSpacing.md),
                    child: MyReviewsList(reviews: reviews ?? const [], onEdit: _edit),
                  ),
                ),
    );
  }
}

/// The list itself, separate from loading so it can be tested on its own.
class MyReviewsList extends StatelessWidget {
  final List<MyReview> reviews;
  final void Function(MyReview review)? onEdit;

  const MyReviewsList({super.key, required this.reviews, this.onEdit});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    if (reviews.isEmpty) {
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: AppSpacing.xl),
        child: Center(
          child: Column(
            children: [
              Icon(Icons.star_outline,
                  size: 48, color: theme.colorScheme.onSurfaceVariant),
              const SizedBox(height: AppSpacing.sm),
              Text(AppStrings.myReviewsEmpty, style: theme.textTheme.bodyMedium),
            ],
          ),
        ),
      );
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        for (final review in reviews)
          Card(
            key: Key('my-review-${review.id}'),
            margin: const EdgeInsets.only(bottom: AppSpacing.md),
            child: Padding(
              padding: const EdgeInsets.all(AppSpacing.md),
              child: _MyReviewTile(review: review, onEdit: onEdit),
            ),
          ),
      ],
    );
  }
}

class _MyReviewTile extends StatelessWidget {
  final MyReview review;
  final void Function(MyReview review)? onEdit;

  const _MyReviewTile({required this.review, this.onEdit});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final muted = theme.textTheme.bodySmall
        ?.copyWith(color: theme.colorScheme.onSurfaceVariant);
    final (label, color) = switch (review.status) {
      ReviewModerationStatus.pending => (
          AppStrings.reviewStatusPending,
          theme.colorScheme.tertiary
        ),
      ReviewModerationStatus.published => (
          AppStrings.reviewStatusPublished,
          theme.colorScheme.primary
        ),
      ReviewModerationStatus.rejected => (
          AppStrings.reviewStatusRejected,
          theme.colorScheme.error
        ),
      ReviewModerationStatus.hidden => (
          AppStrings.reviewStatusHidden,
          theme.colorScheme.onSurfaceVariant
        ),
    };
    final reason = review.moderationReason;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Expanded(
              child: Text(
                review.providerName ?? '',
                style: theme.textTheme.titleSmall,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            ),
            Container(
              padding: const EdgeInsets.symmetric(
                  horizontal: AppSpacing.sm, vertical: AppSpacing.xxs),
              decoration: BoxDecoration(
                color: color.withValues(alpha: 0.12),
                borderRadius: BorderRadius.circular(AppRadius.sm),
              ),
              child: Text(label,
                  style: theme.textTheme.labelSmall?.copyWith(color: color)),
            ),
          ],
        ),
        if (review.serviceName != null) Text(review.serviceName!, style: muted),
        const SizedBox(height: AppSpacing.xxs),
        Row(
          children: [
            ProviderRating(rating: review.rating),
            if (review.createdAt != null) ...[
              const SizedBox(width: AppSpacing.sm),
              Text(JalaliFormatter.formatShortDate(review.createdAt!), style: muted),
            ],
            if (review.isEdited) ...[
              const SizedBox(width: AppSpacing.xxs),
              Text(AppStrings.reviewEditedLabel, style: muted),
            ],
          ],
        ),
        if (review.dimensions.isNotEmpty)
          Wrap(
            spacing: AppSpacing.sm,
            children: [
              for (final e in review.dimensions.entries)
                Text(
                  '${e.key.label}: ${JalaliFormatter.toPersianDigits(e.value.toStringAsFixed(0))}',
                  style: muted,
                ),
            ],
          ),
        if (review.comment != null && review.comment!.isNotEmpty) ...[
          const SizedBox(height: AppSpacing.xs),
          Text(review.comment!, style: theme.textTheme.bodyMedium),
        ],
        if (reason != null &&
            reason.isNotEmpty &&
            (review.status == ReviewModerationStatus.rejected ||
                review.status == ReviewModerationStatus.hidden)) ...[
          const SizedBox(height: AppSpacing.xs),
          Text(AppStrings.reviewModerationReason(reason),
              style: theme.textTheme.bodySmall
                  ?.copyWith(color: theme.colorScheme.error)),
        ],
        if (review.providerResponse != null &&
            review.providerResponse!.isNotEmpty) ...[
          const SizedBox(height: AppSpacing.xs),
          Text(AppStrings.reviewProviderReply, style: muted),
          Text(review.providerResponse!, style: theme.textTheme.bodyMedium),
        ],
        if (review.canEdit && onEdit != null)
          Align(
            alignment: AlignmentDirectional.centerEnd,
            child: TextButton.icon(
              key: Key('my-review-${review.id}-edit'),
              onPressed: () => onEdit!(review),
              icon: const Icon(Icons.edit_outlined, size: AppIconSize.sm),
              label: const Text(AppStrings.reviewEditAction),
            ),
          ),
      ],
    );
  }
}
