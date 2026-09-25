import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../config/theme/app_tokens.dart';
import '../../../core/constants/app_strings.dart';
import '../../../core/utils/persian_digits.dart';
import '../../../core/widgets/app_card.dart';
import '../../../core/widgets/app_empty_state.dart';
import '../../../core/widgets/app_error_state.dart';
import '../../../core/widgets/app_page_scaffold.dart';
import '../../../core/widgets/app_snackbar.dart';
import '../../../core/widgets/app_status_badge.dart';
import '../domain/business_review.dart';
import 'reviews_cubit.dart';

/// The business's reviews: its published rating and dimension averages, then every review in any state, with a
/// reply per published review. A reply is not public until an administrator approves it, and the page says so.
/// Expects a [ReviewsCubit] above it.
class ReviewsPage extends StatelessWidget {
  const ReviewsPage({super.key});

  @override
  Widget build(BuildContext context) {
    return AppPageScaffold(
      title: AppStrings.reviewsTitle,
      body: BlocBuilder<ReviewsCubit, ReviewsState>(
        builder: (context, state) {
          final overview = state.overview;
          if (state.loading && !state.loaded) {
            return const Center(child: CircularProgressIndicator());
          }
          if (!state.loaded || overview == null) {
            return AppErrorState(
              message: state.error ?? AppStrings.reviewsLoadFailed,
              onRetry: () => context.read<ReviewsCubit>().load(),
            );
          }
          return RefreshIndicator(
            onRefresh: () => context.read<ReviewsCubit>().load(),
            child: ListView(
              key: const Key('reviews-list'),
              padding: const EdgeInsets.all(AppSpacing.md),
              children: [
                _Summary(overview: overview),
                const SizedBox(height: AppSpacing.md),
                if (overview.items.isEmpty)
                  const AppEmptyState(icon: Icons.rate_review_outlined, message: AppStrings.reviewsEmpty)
                else
                  for (final review in overview.items)
                    Padding(
                      padding: const EdgeInsets.only(bottom: AppSpacing.md),
                      child: _ReviewTile(review: review, busy: state.busyId == review.id),
                    ),
              ],
            ),
          );
        },
      ),
    );
  }
}

String _stars(double value) => PersianDigits.toPersian(value.toStringAsFixed(1));

class _Summary extends StatelessWidget {
  final ReviewsOverview overview;
  const _Summary({required this.overview});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (!overview.hasRating)
            Text(AppStrings.reviewsNoneYet, style: theme.textTheme.titleMedium)
          else
            Row(
              children: [
                const Icon(Icons.star_rounded, color: AppColors.warning),
                const SizedBox(width: AppSpacing.xs),
                Text(_stars(overview.averageRating), style: theme.textTheme.headlineSmall),
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: Text(AppStrings.reviewsPublishedCount(overview.publishedCount),
                      style: theme.textTheme.bodyMedium, overflow: TextOverflow.ellipsis),
                ),
              ],
            ),
          // Only what somebody rated: a zero bar would read as a bad score.
          for (final entry in overview.dimensions.entries)
            Padding(
              key: Key('reviews-breakdown-${entry.key.name}'),
              padding: const EdgeInsets.only(top: AppSpacing.sm),
              child: Row(
                children: [
                  Expanded(flex: 2, child: Text(entry.key.label, style: theme.textTheme.bodySmall)),
                  Expanded(
                    flex: 3,
                    child: LinearProgressIndicator(
                      value: (entry.value.average / 5).clamp(0, 1),
                      minHeight: 6,
                      borderRadius: BorderRadius.circular(AppRadius.sm),
                    ),
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  Text(_stars(entry.value.average), style: theme.textTheme.bodySmall),
                ],
              ),
            ),
        ],
      ),
    );
  }
}

class _ReviewTile extends StatelessWidget {
  final BusinessReview review;
  final bool busy;
  const _ReviewTile({required this.review, required this.busy});

  // Inline buttons opt out of the theme's infinite-width minimum, which throws inside a Row.
  static final _inline = TextButton.styleFrom(
    minimumSize: const Size(0, 40),
    tapTargetSize: MaterialTapTargetSize.shrinkWrap,
    padding: const EdgeInsets.symmetric(horizontal: AppSpacing.sm),
  );

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final muted = theme.textTheme.bodySmall?.copyWith(color: AppColors.ink.withValues(alpha: 0.7));
    final (label, badge) = switch (review.status) {
      ReviewStatus.pending => (AppStrings.reviewStatusPending, AppBadgeStatus.warning),
      ReviewStatus.published => (AppStrings.reviewStatusPublished, AppBadgeStatus.success),
      ReviewStatus.rejected => (AppStrings.reviewStatusRejected, AppBadgeStatus.danger),
      ReviewStatus.hidden => (AppStrings.reviewStatusHidden, AppBadgeStatus.neutral),
    };

    return AppCard(
      key: Key('review-${review.id}'),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.star_rounded, size: AppIconSize.sm, color: AppColors.warning),
              const SizedBox(width: AppSpacing.xs),
              Text(_stars(review.rating), style: theme.textTheme.titleSmall),
              const Spacer(),
              AppStatusBadge(label: label, status: badge),
            ],
          ),
          if (review.dimensions.isNotEmpty)
            Padding(
              padding: const EdgeInsets.only(top: AppSpacing.xs),
              child: Wrap(
                spacing: AppSpacing.sm,
                children: [
                  for (final e in review.dimensions.entries)
                    Text('${e.key.label}: ${PersianDigits.toPersian(e.value.toStringAsFixed(0))}', style: muted),
                ],
              ),
            ),
          if (review.comment != null && review.comment!.isNotEmpty) ...[
            const SizedBox(height: AppSpacing.sm),
            Text(review.comment!, style: theme.textTheme.bodyMedium),
          ],
          if (review.hasReply) ...[
            const SizedBox(height: AppSpacing.sm),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(AppSpacing.sm),
              decoration: BoxDecoration(
                color: AppColors.primary.withValues(alpha: 0.06),
                borderRadius: BorderRadius.circular(AppRadius.sm),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(AppStrings.yourReply, style: muted),
                  Text(review.reply!, style: theme.textTheme.bodyMedium),
                  const SizedBox(height: AppSpacing.xs),
                  Text(
                    switch (review.replyStatus) {
                      ReplyStatus.published => AppStrings.replyStatusPublished,
                      ReplyStatus.rejected => AppStrings.replyStatusRejected(review.replyReason ?? ''),
                      _ => AppStrings.replyStatusPending,
                    },
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: review.replyStatus == ReplyStatus.rejected ? AppColors.danger : null,
                    ),
                  ),
                ],
              ),
            ),
          ],
          if (review.status == ReviewStatus.pending) ...[
            const SizedBox(height: AppSpacing.xs),
            Text(AppStrings.reviewPendingNoReply, style: muted),
          ],
          if (review.status == ReviewStatus.published)
            Align(
              alignment: AlignmentDirectional.centerEnd,
              child: busy
                  ? const Padding(
                      padding: EdgeInsets.all(AppSpacing.sm),
                      child: SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2)),
                    )
                  : Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        if (review.hasReply)
                          TextButton(
                            key: Key('review-${review.id}-remove-reply'),
                            style: _inline,
                            onPressed: () => _remove(context),
                            child: const Text(AppStrings.replyRemoveAction),
                          ),
                        TextButton(
                          key: Key(review.hasReply ? 'review-${review.id}-edit-reply' : 'review-${review.id}-reply'),
                          style: _inline,
                          onPressed: () => _write(context),
                          child: Text(review.hasReply ? AppStrings.replyEditAction : AppStrings.replyAction),
                        ),
                      ],
                    ),
            ),
        ],
      ),
    );
  }

  Future<void> _write(BuildContext context) async {
    final cubit = context.read<ReviewsCubit>();
    final text = await showDialog<String>(
      context: context,
      builder: (_) => ReplyDialog(initial: review.reply),
    );
    if (text == null || !context.mounted) return;
    final error = await cubit.reply(review.id, text);
    if (!context.mounted) return;
    error == null ? AppSnackbar.success(context, AppStrings.replySent) : AppSnackbar.error(context, error);
  }

  Future<void> _remove(BuildContext context) async {
    final error = await context.read<ReviewsCubit>().removeReply(review.id);
    if (!context.mounted) return;
    error == null ? AppSnackbar.success(context, AppStrings.replyRemoved) : AppSnackbar.error(context, error);
  }
}

/// Writing or rewriting the reply. Returns the text, or null when dismissed.
class ReplyDialog extends StatefulWidget {
  final String? initial;
  const ReplyDialog({super.key, this.initial});

  @override
  State<ReplyDialog> createState() => _ReplyDialogState();
}

class _ReplyDialogState extends State<ReplyDialog> {
  late final _text = TextEditingController(text: widget.initial ?? '');
  String? _error;

  @override
  void dispose() {
    _text.dispose();
    super.dispose();
  }

  void _submit() {
    final text = _text.text.trim();
    if (text.isEmpty) {
      setState(() => _error = AppStrings.replyRequired);
      return;
    }
    Navigator.of(context).pop(text);
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text(AppStrings.replyDialogTitle),
      content: TextField(
        key: const Key('reply-text'),
        controller: _text,
        maxLines: 4,
        maxLength: 1000,
        decoration: InputDecoration(hintText: AppStrings.replyHint, errorText: _error),
      ),
      actions: [
        Row(
          children: [
            Expanded(
              child: OutlinedButton(
                onPressed: () => Navigator.of(context).pop(),
                child: const Text(AppStrings.cancel),
              ),
            ),
            const SizedBox(width: AppSpacing.sm),
            Expanded(
              child: FilledButton(
                key: const Key('reply-submit'),
                onPressed: _submit,
                child: const Text(AppStrings.replySend),
              ),
            ),
          ],
        ),
      ],
    );
  }
}
