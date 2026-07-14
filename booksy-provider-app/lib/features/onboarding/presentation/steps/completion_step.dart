import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_button.dart';

/// Step 8 — registration complete. [onDone] re-checks auth status so the
/// refreshed provider status routes the user into the dashboard.
class CompletionStep extends StatelessWidget {
  final VoidCallback onDone;

  const CompletionStep({super.key, required this.onDone});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.check_circle_outline,
                size: 88, color: AppColors.primary),
            const SizedBox(height: AppSpacing.lg),
            Text(
              AppStrings.completionTitle,
              style: theme.textTheme.headlineSmall,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: AppSpacing.sm),
            Text(
              AppStrings.completionBody,
              style: theme.textTheme.bodyMedium,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: AppSpacing.xl),
            AppButton(
              key: const Key('onboarding-go-to-dashboard'),
              label: AppStrings.goToDashboard,
              onPressed: onDone,
            ),
          ],
        ),
      ),
    );
  }
}
