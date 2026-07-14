import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_button.dart';
import '../bloc/auth_bloc.dart';
import '../bloc/auth_event.dart';

/// Shown to authenticated providers whose business profile is incomplete
/// (new / no ServiceCatalog profile / Drafted). Entry point to the onboarding
/// wizard (a separate epic — this hands off).
class OnboardingRequiredPage extends StatelessWidget {
  const OnboardingRequiredPage({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(
        actions: [
          IconButton(
            tooltip: AppStrings.logout,
            icon: const Icon(Icons.logout),
            onPressed: () =>
                context.read<AuthBloc>().add(const LogoutRequested()),
          ),
        ],
      ),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.lg),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.business_center_outlined,
                  size: 72, color: AppColors.primary),
              const SizedBox(height: AppSpacing.lg),
              Text(AppStrings.onboardingRequiredTitle,
                  style: theme.textTheme.headlineSmall,
                  textAlign: TextAlign.center),
              const SizedBox(height: AppSpacing.sm),
              Text(AppStrings.onboardingRequiredBody,
                  style: theme.textTheme.bodyMedium,
                  textAlign: TextAlign.center),
              const SizedBox(height: AppSpacing.xl),
              AppButton(
                label: AppStrings.onboardingStart,
                // TODO(onboarding): navigate into the onboarding wizard epic.
                onPressed: () {},
              ),
            ],
          ),
        ),
      ),
    );
  }
}
