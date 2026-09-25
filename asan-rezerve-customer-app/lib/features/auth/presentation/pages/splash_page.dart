import 'package:flutter/material.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';

/// Branding-only splash. The router holds this route until the stored
/// session resolves (AuthNotifier.sessionResolved), then redirects to home —
/// authenticated or guest — so login never flashes on cold start.
class SplashPage extends StatelessWidget {
  const SplashPage({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.calendar_today_rounded,
              size: 100,
              color: theme.colorScheme.primary,
            ),
            const SizedBox(height: AppSpacing.lg),
            Text(AppStrings.appName, style: theme.textTheme.displaySmall),
            const SizedBox(height: AppSpacing.xs),
            Text(
              AppStrings.appTagline,
              style: theme.textTheme.bodyLarge?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
            const SizedBox(height: AppSpacing.xxl),
            const CircularProgressIndicator(),
          ],
        ),
      ),
    );
  }
}
