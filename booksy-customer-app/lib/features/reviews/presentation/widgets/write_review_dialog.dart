import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../domain/entities/review.dart';

/// What the customer is leaving: stars, optionally what they want to say, and
/// any of the four dimensions they chose to rate.
class ReviewDraft {
  final double rating;
  final String? comment;
  final Map<ReviewDimension, double> dimensions;

  const ReviewDraft({
    required this.rating,
    this.comment,
    this.dimensions = const {},
  });
}

/// Leaving a review after a visit — or, given [initial], editing one. Returns
/// the draft, or null when dismissed.
Future<ReviewDraft?> showWriteReviewDialog(BuildContext context,
        {ReviewDraft? initial}) =>
    showDialog<ReviewDraft>(
      context: context,
      builder: (_) => WriteReviewDialog(initial: initial),
    );

class WriteReviewDialog extends StatefulWidget {
  /// The review being edited; null for a new one.
  final ReviewDraft? initial;

  const WriteReviewDialog({super.key, this.initial});

  @override
  State<WriteReviewDialog> createState() => _WriteReviewDialogState();
}

class _WriteReviewDialogState extends State<WriteReviewDialog> {
  final _comment = TextEditingController();
  double _rating = 0;
  final _dimensions = <ReviewDimension, double>{};
  String? _error;

  bool get _editing => widget.initial != null;

  @override
  void initState() {
    super.initState();
    final initial = widget.initial;
    if (initial != null) {
      _rating = initial.rating;
      _comment.text = initial.comment ?? '';
      _dimensions.addAll(initial.dimensions);
    }
  }

  @override
  void dispose() {
    _comment.dispose();
    super.dispose();
  }

  void _submit() {
    // The stars are the review; the words are optional. The server asks for at
    // least ten characters when there are any, so the dialog says so here
    // rather than letting the request come back rejected.
    final comment = _comment.text.trim();
    if (_rating <= 0) {
      setState(() => _error = AppStrings.reviewRatingRequired);
      return;
    }
    if (comment.isNotEmpty && comment.length < 10) {
      setState(() => _error = AppStrings.reviewCommentTooShort);
      return;
    }
    Navigator.of(context).pop(
      ReviewDraft(
        rating: _rating,
        comment: comment.isEmpty ? null : comment,
        dimensions: Map.unmodifiable(_dimensions),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return AlertDialog(
      title: Text(
          _editing ? AppStrings.reviewEditTitle : AppStrings.reviewDialogTitle),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(AppStrings.reviewRatingLabel,
                style: theme.textTheme.bodyMedium),
            const SizedBox(height: AppSpacing.xs),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                for (var star = 1; star <= 5; star++)
                  IconButton(
                    key: Key('review-star-$star'),
                    onPressed: () => setState(() {
                      _rating = star.toDouble();
                      _error = null;
                    }),
                    icon: Icon(
                      star <= _rating ? Icons.star : Icons.star_border,
                      color: theme.colorScheme.primary,
                      size: 32,
                    ),
                  ),
              ],
            ),
            // The dimensions are optional and wait behind a disclosure, so the
            // overall star stays the whole of a quick review (design D1). An
            // edit that already carries some opens with them showing.
            ExpansionTile(
              key: const Key('review-dimensions'),
              tilePadding: EdgeInsets.zero,
              initiallyExpanded: _dimensions.isNotEmpty,
              title: Text(AppStrings.reviewDimensionsToggle,
                  style: theme.textTheme.bodyMedium),
              children: [
                for (final d in ReviewDimension.values)
                  Row(
                    children: [
                      Expanded(
                          child:
                              Text(d.label, style: theme.textTheme.bodySmall)),
                      for (var star = 1; star <= 5; star++)
                        InkResponse(
                          key: Key('review-dim-${d.name}-$star'),
                          onTap: () => setState(() {
                            // The same star again clears it: "not rated" must stay reachable.
                            if (_dimensions[d] == star) {
                              _dimensions.remove(d);
                            } else {
                              _dimensions[d] = star.toDouble();
                            }
                          }),
                          child: Padding(
                            padding: const EdgeInsets.all(AppSpacing.xxs),
                            child: Icon(
                              star <= (_dimensions[d] ?? 0)
                                  ? Icons.star
                                  : Icons.star_border,
                              color: theme.colorScheme.primary,
                              size: 22,
                            ),
                          ),
                        ),
                    ],
                  ),
              ],
            ),
            const SizedBox(height: AppSpacing.sm),
            TextField(
              key: const Key('review-comment'),
              controller: _comment,
              maxLines: 3,
              maxLength: 2000,
              decoration: const InputDecoration(
                labelText: AppStrings.reviewCommentLabel,
                border: OutlineInputBorder(),
              ),
            ),
            if (_editing)
              Text(
                AppStrings.reviewEditNotice,
                style: theme.textTheme.bodySmall
                    ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
              ),
            if (_error != null)
              Text(
                _error!,
                key: const Key('review-error'),
                style: theme.textTheme.bodySmall
                    ?.copyWith(color: theme.colorScheme.error),
              ),
          ],
        ),
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
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: FilledButton(
                key: const Key('review-submit'),
                onPressed: _submit,
                child: Text(_editing
                    ? AppStrings.reviewSaveAction
                    : AppStrings.reviewWriteAction),
              ),
            ),
          ],
        ),
      ],
    );
  }
}
