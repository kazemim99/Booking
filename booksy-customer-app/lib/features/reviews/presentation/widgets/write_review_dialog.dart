import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';

/// What the customer is leaving: stars, and optionally what they want to say.
class ReviewDraft {
  final double rating;
  final String? comment;

  const ReviewDraft({required this.rating, this.comment});
}

/// Leaving a review after a visit. Returns the draft, or null when dismissed.
Future<ReviewDraft?> showWriteReviewDialog(BuildContext context) =>
    showDialog<ReviewDraft>(
      context: context,
      builder: (_) => const WriteReviewDialog(),
    );

class WriteReviewDialog extends StatefulWidget {
  const WriteReviewDialog({super.key});

  @override
  State<WriteReviewDialog> createState() => _WriteReviewDialogState();
}

class _WriteReviewDialogState extends State<WriteReviewDialog> {
  final _comment = TextEditingController();
  double _rating = 0;
  String? _error;

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
      ReviewDraft(rating: _rating, comment: comment.isEmpty ? null : comment),
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return AlertDialog(
      title: const Text(AppStrings.reviewDialogTitle),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(AppStrings.reviewRatingLabel, style: theme.textTheme.bodyMedium),
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
          if (_error != null)
            Text(
              _error!,
              key: const Key('review-error'),
              style: theme.textTheme.bodySmall
                  ?.copyWith(color: theme.colorScheme.error),
            ),
        ],
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
                child: const Text(AppStrings.reviewWriteAction),
              ),
            ),
          ],
        ),
      ],
    );
  }
}
