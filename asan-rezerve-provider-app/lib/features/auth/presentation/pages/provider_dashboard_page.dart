import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_icon_button.dart';
import '../widgets/confirm_logout.dart';

/// Placeholder dashboard — the destination after successful provider auth.
/// The full dashboard is a separate epic; this proves the auth landing.
class ProviderDashboardPage extends StatelessWidget {
  const ProviderDashboardPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text(AppStrings.dashboardTitle),
        actions: [
          AppIconButton(
            tooltip: AppStrings.logout,
            icon: Icons.logout,
            onTap: () => confirmAndLogout(context),
          ),
        ],
      ),
      body: const Center(
        child: Padding(
          padding: EdgeInsets.all(AppSpacing.lg),
          child: Text(
            AppStrings.dashboardWelcome,
            style: TextStyle(fontSize: 18),
            textAlign: TextAlign.center,
          ),
        ),
      ),
    );
  }
}
