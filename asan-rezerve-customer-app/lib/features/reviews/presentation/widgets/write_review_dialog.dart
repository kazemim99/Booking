import 'package:flutter/material.dart';

import '../../../../config/theme/app_colors.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/persian_formatter.dart';
import '../../../../core/widgets/app_button.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../../domain/entities/review.dart';

/// What the customer is leaving: a star for each of the four aspects, the
/// overall that follows from them, optionally what they want to say, and
/// whether their name signs it.
class ReviewDraft {
  /// The overall star — the four aspects' average to the nearest half
  /// ([overallOf]). Sent explicitly so an older server that still requires it
  /// takes the review too.
  final double rating;
  final String? comment;
  final Map<ReviewDimension, double> dimensions;

  /// False when the customer ticked «نامم در نظر نمایش داده نشود»: the public
  /// review then reads «مشتری».
  final bool showName;

  const ReviewDraft({
    required this.rating,
    this.comment,
    this.dimensions = const {},
    this.showName = true,
  });

  /// The overall star: the aspects' average to the nearest half, halves away
  /// from zero (3.25 → 3.5, 3.75 → 4). Zero when none is rated.
  static double overallOf(Map<ReviewDimension, double> dimensions) {
    if (dimensions.isEmpty) return 0;
    final average =
        dimensions.values.reduce((a, b) => a + b) / dimensions.length;
    return (average * 2).round() / 2;
  }
}

/// Leaving a review after a visit — or, given [initial], editing one. Returns
/// the draft, or null when dismissed. [subject] names what is being reviewed
/// (salon · service); [rating] is a star already chosen where the customer
/// was asked «تجربه‌تان چطور بود؟» — every aspect starts at it.
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
  final _dimensions = <ReviewDimension, double>{};
  bool _hideName = false;
  String? _error;

  bool get _editing => widget.initial != null;

  /// All four aspects are the review (reviews-and-reschedule-round2 D1).
  bool get _complete =>
      ReviewDimension.values.every((d) => (_dimensions[d] ?? 0) > 0);

  @override
  void initState() {
    super.initState();
    final picked = widget.rating;
    if (picked != null && picked > 0) {
      final star = picked.round().clamp(1, 5).toDouble();
      for (final d in ReviewDimension.values) {
        _dimensions[d] = star;
      }
    }
    final initial = widget.initial;
    if (initial != null) {
      _comment.text = initial.comment ?? '';
      _hideName = !initial.showName;
      // A review written before the aspects were required carries only its
      // overall star (or a few aspects): each missing aspect starts at that
      // star rounded, so the edit can be saved as it stands.
      final fallback = initial.rating.round().clamp(1, 5).toDouble();
      for (final d in ReviewDimension.values) {
        _dimensions[d] = initial.dimensions[d] ?? fallback;
      }
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
    if (!_complete) {
      setState(() => _error = AppStrings.reviewAspectsRequired);
      return;
    }
    if (comment.isNotEmpty && comment.length < 10) {
      setState(() => _error = AppStrings.reviewCommentTooShort);
      return;
    }
    final dimensions = Map<ReviewDimension, double>.unmodifiable(_dimensions);
    Navigator.of(context).pop(
      ReviewDraft(
        rating: ReviewDraft.overallOf(dimensions),
        comment: comment.isEmpty ? null : comment,
        dimensions: dimensions,
        showName: !_hideName,
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
            // The four aspects ARE the rating: no separate overall row to
            // disagree with them (reviews-and-reschedule-round2 D1). Each
            // label sits above its own row, so the five 48 dp stars have the
            // dialog's whole width.
            for (final d in ReviewDimension.values) ...[
              Align(
                alignment: AlignmentDirectional.centerStart,
                child: Text(d.label, style: theme.textTheme.bodyMedium),
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
                      iconSize: 28,
                      onPressed: () => setState(() {
                        _dimensions[d] = star.toDouble();
                        _error = null;
                      }),
                    ),
                ],
              ),
            ],
            const SizedBox(height: AppSpacing.xs),
            _OverallSummary(
              value: _complete ? ReviewDraft.overallOf(_dimensions) : null,
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
            CheckboxListTile(
              key: const Key('review-hide-name'),
              value: _hideName,
              onChanged: (v) => setState(() => _hideName = v ?? false),
              contentPadding: EdgeInsets.zero,
              controlAffinity: ListTileControlAffinity.leading,
              title: Text(AppStrings.reviewHideName,
                  style: theme.textTheme.bodyMedium),
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
                // Until every aspect has its star there is nothing to send.
                onPressed: _complete ? _submit : null,
              ),
            ),
          ],
        ),
      ],
    );
  }
}

/// «امتیاز کلی: ۴.۵» with its stars, halves shown, live as the aspects are
/// rated; until all four are, it says what is still missing.
class _OverallSummary extends StatelessWidget {
  final double? value;

  const _OverallSummary({required this.value});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final v = value;
    if (v == null) {
      return Text(
        AppStrings.reviewAspectsRequired,
        key: const Key('review-overall-missing'),
        style: theme.textTheme.bodySmall
            ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
      );
    }
    final label = AppStrings.reviewOverallLabel(
        PersianFormatter.toPersianDigits(v.toStringAsFixed(1)));
    return Semantics(
      label: label,
      excludeSemantics: true,
      child: Row(
        key: const Key('review-overall'),
        children: [
          Flexible(
            child: Text(label,
                key: const Key('review-overall-label'),
                style: theme.textTheme.titleSmall),
          ),
          const SizedBox(width: AppSpacing.xs),
          for (var star = 1; star <= 5; star++)
            Icon(
              v >= star
                  ? Icons.star_rounded
                  : v >= star - 0.5
                      ? Icons.star_half_rounded
                      : Icons.star_border_rounded,
              key: Key('review-overall-star-$star'),
              size: AppIconSize.action,
              color: v >= star - 0.5
                  ? AppColors.star
                  : theme.colorScheme.onSurfaceVariant,
            ),
        ],
      ),
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
