import 'package:flutter/material.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_loading.dart';
import '../../../../core/widgets/brand_mark.dart';

/// Shown while the persisted session is restored (auto-login). The router's
/// redirect moves off splash once [AuthNotifier] resolves.
class SplashPage extends StatelessWidget {
  const SplashPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            BrandMark(size: 72, semanticLabel: AppStrings.appName),
            const SizedBox(height: AppSpacing.lg),
            const Text(AppStrings.appName),
            const SizedBox(height: AppSpacing.lg),
            const AppLoading(),
          ],
        ),
      ),
    );
  }
}
