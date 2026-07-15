import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';
import '../constants/app_strings.dart';

/// Standard recoverable-error view (spec: feedback-states): muted warning
/// icon, muted message, and an optional outlined retry button.
class AppErrorState extends StatelessWidget {
  final String message;
  final VoidCallback? onRetry;
  final IconData icon;

  const AppErrorState({
    super.key,
    required this.message,
    this.onRetry,
    this.icon = Icons.warning_rounded,
  });

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(icon, size: 50, color: AppColors.muted),
          const SizedBox(height: AppSpacing.sm),
          Text(
            message,
            textAlign: TextAlign.center,
            style: Theme.of(context)
                .textTheme
                .bodyMedium
                ?.copyWith(color: AppColors.muted, height: 1.4),
          ),
          if (onRetry != null) ...[
            const SizedBox(height: AppSpacing.md),
            OutlinedButton(
              key: const Key('app-error-retry'),
              onPressed: onRetry,
              // Compact width: the theme's full-width minimum would stretch
              // the retry across the error view.
              style: OutlinedButton.styleFrom(
                minimumSize: const Size(0, 40),
                padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
              ),
              child: const Text(AppStrings.retry),
            ),
          ],
        ],
      ),
    );
  }
}
