import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_button.dart';
import '../../domain/entities/provider_status.dart';
import '../bloc/auth_bloc.dart';
import '../bloc/auth_event.dart';

/// Shown when an authenticated provider's account is blocked
/// (Suspended/Inactive/Archived). AUTH_SPECIFICATION.md E-14 / D-1 — this is a
/// deliberate improvement over Vue, which would let them reach the dashboard.
class AccountBlockedPage extends StatelessWidget {
  final ProviderStatus? status;

  const AccountBlockedPage({super.key, this.status});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final statusName = (status ?? ProviderStatus.suspended).wireName;
    return Scaffold(
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.lg),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.lock_outline, size: 72, color: AppColors.warning),
              const SizedBox(height: AppSpacing.lg),
              Text(AppStrings.accountBlockedTitle,
                  style: theme.textTheme.headlineSmall,
                  textAlign: TextAlign.center),
              const SizedBox(height: AppSpacing.sm),
              Text(AppStrings.accountBlockedBody(statusName),
                  style: theme.textTheme.bodyMedium,
                  textAlign: TextAlign.center),
              const SizedBox(height: AppSpacing.xl),
              AppButton(
                label: AppStrings.accountBlockedLogout,
                onPressed: () =>
                    context.read<AuthBloc>().add(const LogoutRequested()),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
