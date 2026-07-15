import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';

/// The single loading presentation for screens (spec: feedback-states).
/// Feature code renders this instead of raw [CircularProgressIndicator]s —
/// the only exception is inside shared components like `AppButton`.
class AppLoading extends StatelessWidget {
  /// Optional muted message rendered beneath the spinner.
  final String? message;

  /// Spinner diameter.
  final double size;

  /// Wraps the loader in a [Center] for page-level use.
  final bool centered;

  const AppLoading({
    super.key,
    this.message,
    this.size = 32,
    this.centered = false,
  });

  /// Page-level loader: centered, full-size spinner.
  const AppLoading.page({super.key, this.message})
      : size = 32,
        centered = true;

  @override
  Widget build(BuildContext context) {
    final loader = Column(
      mainAxisSize: MainAxisSize.min,
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        SizedBox(
          width: size,
          height: size,
          child: CircularProgressIndicator(
            strokeWidth: size < 24 ? 2 : 3,
            color: Theme.of(context).colorScheme.primary,
          ),
        ),
        if (message != null) ...[
          const SizedBox(height: AppSpacing.sm),
          Text(
            message!,
            textAlign: TextAlign.center,
            style: Theme.of(context)
                .textTheme
                .bodyMedium
                ?.copyWith(color: AppColors.muted),
          ),
        ],
      ],
    );
    return centered ? Center(child: loader) : loader;
  }
}
