import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/app_snackbar.dart';
import '../../../auth/presentation/bloc/auth_bloc.dart';
import '../../../auth/presentation/bloc/auth_event.dart';
import '../cubit/onboarding_cubit.dart';
import '../cubit/onboarding_state.dart';
import '../steps/business_info_step.dart';
import '../steps/category_step.dart';
import '../steps/completion_step.dart';
import '../steps/gallery_step.dart';
import '../steps/location_step.dart';
import '../steps/preview_step.dart';
import '../steps/services_step.dart';
import '../steps/working_hours_step.dart';

/// Organization onboarding wizard (mirrors OrganizationRegistrationFlow.vue).
///
/// Hosts the 8-step flow, surfaces save errors, and on completion re-checks the
/// auth session so the refreshed provider status routes into the dashboard.
class OnboardingWizardPage extends StatelessWidget {
  /// Owner phone, pre-filled into step 1 from the authenticated session.
  final String? phoneNumber;

  const OnboardingWizardPage({super.key, this.phoneNumber});

  @override
  Widget build(BuildContext context) {
    return BlocProvider<OnboardingCubit>(
      create: (_) => getIt<OnboardingCubit>()..init(phoneNumber: phoneNumber),
      child: const _OnboardingWizardView(),
    );
  }
}

class _OnboardingWizardView extends StatelessWidget {
  const _OnboardingWizardView();

  @override
  Widget build(BuildContext context) {
    return BlocConsumer<OnboardingCubit, OnboardingState>(
      listenWhen: (prev, next) =>
          next.phase == OnboardingPhase.error &&
          next.errorMessage != prev.errorMessage,
      listener: (context, state) {
        final message = state.errorMessage;
        if (message != null) AppSnackbar.error(context, message);
      },
      builder: (context, state) {
        return Scaffold(
          appBar: AppBar(
            automaticallyImplyLeading: false,
            title: Text(
              AppStrings.onboardingStepLabels[state.step - 1],
            ),
            bottom: PreferredSize(
              preferredSize: const Size.fromHeight(4),
              child: LinearProgressIndicator(
                value: state.step / OnboardingState.totalSteps,
                // Green accent on the blue app-bar chrome (Coliride).
                color: AppColors.success,
                backgroundColor: Colors.white24,
              ),
            ),
            actions: [
              if (!state.isCompleted)
                IconButton(
                  tooltip: AppStrings.logout,
                  icon: const Icon(Icons.logout),
                  onPressed: () =>
                      context.read<AuthBloc>().add(const LogoutRequested()),
                ),
            ],
          ),
          body: _stepBody(context, state),
        );
      },
    );
  }

  Widget _stepBody(BuildContext context, OnboardingState state) {
    switch (state.step) {
      case 1:
        return const BusinessInfoStep();
      case 2:
        return const CategoryStep();
      case 3:
        return const LocationStep();
      case 4:
        return const ServicesStep();
      case 5:
        return const WorkingHoursStep();
      case 6:
        return const GalleryStep();
      case 7:
        return const PreviewStep();
      case 8:
        return CompletionStep(
          // Re-fetch the provider status from the server (the cached JWT still
          // says "Drafted"), so the router redirects onboarding → dashboard.
          onDone: () => context
              .read<AuthBloc>()
              .add(const ProviderStatusRefreshRequested()),
        );
      default:
        return const SizedBox.shrink();
    }
  }
}
