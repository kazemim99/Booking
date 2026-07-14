import 'package:flutter/material.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';

/// Shown while the persisted session is restored (auto-login). The router's
/// redirect moves off splash once [AuthNotifier] resolves.
class SplashPage extends StatelessWidget {
  const SplashPage({super.key});

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.storefront_rounded, size: 72, color: AppColors.primary),
            SizedBox(height: AppSpacing.lg),
            Text(AppStrings.appName),
            SizedBox(height: AppSpacing.lg),
            CircularProgressIndicator(),
          ],
        ),
      ),
    );
  }
}
