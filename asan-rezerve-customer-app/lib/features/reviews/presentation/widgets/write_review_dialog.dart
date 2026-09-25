import 'package:flutter/material.dart';

import '../../../../config/theme/app_colors.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/persian_formatter.dart';
import '../../../../core/widgets/app_button.dart';
import '../../../../core/widgets/app_text_field.dart';
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
/// the draft, or null when dismissed. [subject] names what is being reviewed
/// (salon · service); [rating] is a star already chosen where the customer
/// was asked «تجربه‌تان چطور بود؟».
Future<ReviewDraft?> showWriteReviewDialog(BuildContext context,
        {ReviewDraft? initial, String? subject, double? rating}) =>
    showDialog<ReviewDraft>(
      context: context,
      builder: (_) =>
          WriteReviewDialog(initial: initial, subject: subject, rating: rating),
    );

class WriteReviewDialog extends StatefulWidget {
  /// The review being edited; null for a new one.
  final ReviewDraft? initial;

  /// What is being reviewed, shown under the title — the customer should never
  /// wonder which visit they are rating.
  final String? subject;

  /// A star the customer already picked before the dialog opened.
  final double? rating;

  const WriteReviewDialog({super.key, this.initial, this.subject, this.rating});

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
    _rating = widget.rating ?? 0;
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
      // A narrower inset than Material's 40 dp, so five 48 dp stars fit the
      // content on a 360 dp phone.
      insetPadding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.md, vertical: AppSpacing.lg),
      title: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(_editing
              ? AppStrings.reviewEditTitle
              : AppStrings.reviewDialogTitle),
          if (widget.subject case final subject? when subject.isNotEmpty)
            Padding(
              padding: const EdgeInsets.only(top: AppSpacing.xxs),
              child: Text(
                subject,
                key: const Key('review-subject'),
                style: theme.textTheme.bodySmall
                    ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
              ),
            ),
        ],
      ),
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
                  _StarButton(
                    key: Key('review-star-$star'),
                    label: AppStrings.reviewStarLabel(_digits(star)),
                    filled: star <= _rating,
                    selected: star == _rating,
                    iconSize: 32,
                    onPressed: () => setState(() {
                      _rating = star.toDouble();
                      _error = null;
                    }),
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
                // Each label sits above its own row, so the five 48 dp stars
                // have the dialog's whole width, not what a label beside
                // them would leave.
                for (final d in ReviewDimension.values) ...[
                  Align(
                    alignment: AlignmentDirectional.centerStart,
                    child: Text(d.label, style: theme.textTheme.bodySmall),
                  ),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      for (var star = 1; star <= 5; star++)
                        _StarButton(
                          key: Key('review-dim-${d.name}-$star'),
                          label: AppStrings.reviewDimensionStarLabel(
                              d.label, _digits(star)),
                          filled: star <= (_dimensions[d] ?? 0),
                          selected: _dimensions[d] == star,
                          iconSize: 24,
                          onPressed: () => setState(() {
                            // The same star again clears it: "not rated" must stay reachable.
                            if (_dimensions[d] == star) {
                              _dimensions.remove(d);
                            } else {
                              _dimensions[d] = star.toDouble();
                            }
                          }),
                        ),
                    ],
                  ),
                ],
              ],
            ),
            const SizedBox(height: AppSpacing.sm),
            AppTextField(
              key: const Key('review-comment'),
              controller: _comment,
              label: AppStrings.reviewCommentLabel,
              maxLines: 3,
              maxLength: 2000,
              showCounter: true,
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
              child: AppButton.secondary(
                label: AppStrings.cancel,
                onPressed: () => Navigator.of(context).pop(),
              ),
            ),
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: AppButton(
                key: const Key('review-submit'),
                label: _editing
                    ? AppStrings.reviewSaveAction
                    : AppStrings.reviewWriteAction,
                onPressed: _submit,
              ),
            ),
          ],
        ),
      ],
    );
  }
}

String _digits(int n) => PersianFormatter.toPersianDigits('$n');

/// One star: a 48 dp target whose tooltip (and so its spoken name) says how
/// many stars it is, carrying the selected state on the current choice.
class _StarButton extends StatelessWidget {
  final String label;
  final bool filled;
  final bool selected;
  final double iconSize;
  final VoidCallback onPressed;

  const _StarButton({
    super.key,
    required this.label,
    required this.filled,
    required this.selected,
    required this.iconSize,
    required this.onPressed,
  });

  @override
  Widget build(BuildContext context) {
    return IconButton(
      tooltip: label,
      isSelected: selected,
      onPressed: onPressed,
      padding: EdgeInsets.zero,
      constraints: const BoxConstraints(
        minWidth: AppTouchTarget.min,
        minHeight: AppTouchTarget.min,
      ),
      icon: Icon(
        filled ? Icons.star : Icons.star_border,
        color: filled
            ? AppColors.star
            : Theme.of(context).colorScheme.onSurfaceVariant,
        size: iconSize,
      ),
    );
  }
}
