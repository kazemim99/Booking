import 'package:flutter/material.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_button.dart';

/// Shared chrome for a wizard step: title, subtitle, scrollable body, and a
/// back/next action row.
class StepScaffold extends StatelessWidget {
  final String title;
  final String subtitle;
  final Widget child;
  final VoidCallback? onNext;
  final VoidCallback? onBack;
  final String nextLabel;
  final bool loading;

  const StepScaffold({
    super.key,
    required this.title,
    required this.subtitle,
    required this.child,
    this.onNext,
    this.onBack,
    this.nextLabel = AppStrings.next,
    this.loading = false,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      children: [
        Expanded(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(AppSpacing.lg),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(title, style: theme.textTheme.headlineSmall),
                const SizedBox(height: AppSpacing.xs),
                Text(subtitle, style: theme.textTheme.bodyMedium),
                const SizedBox(height: AppSpacing.lg),
                child,
              ],
            ),
          ),
        ),
        SafeArea(
          top: false,
          child: Padding(
            padding: const EdgeInsets.all(AppSpacing.lg),
            child: Row(
              children: [
                if (onBack != null) ...[
                  Expanded(
                    child: OutlinedButton(
                      onPressed: loading ? null : onBack,
                      child: const Text(AppStrings.previous),
                    ),
                  ),
                  const SizedBox(width: AppSpacing.md),
                ],
                Expanded(
                  child: AppButton(
                    label: nextLabel,
                    loading: loading,
                    onPressed: onNext,
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
