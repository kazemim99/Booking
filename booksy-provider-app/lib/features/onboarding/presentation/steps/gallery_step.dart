import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../cubit/onboarding_cubit.dart';
import '../cubit/onboarding_state.dart';
import '../widgets/step_scaffold.dart';

/// Step 6 — gallery. Optional in the Vue flow (skippable), so the wizard can
/// advance without images.
///
/// NOTE: image picking/upload (POST step-7/gallery, multipart) is not wired yet
/// — tracked as a follow-up. Providers can add images later from the panel.
class GalleryStep extends StatelessWidget {
  const GalleryStep({super.key});

  @override
  Widget build(BuildContext context) {
    final cubit = context.read<OnboardingCubit>();
    return BlocBuilder<OnboardingCubit, OnboardingState>(
      builder: (context, state) {
        return StepScaffold(
          title: AppStrings.galleryTitle,
          subtitle: AppStrings.gallerySubtitle,
          loading: state.isSaving,
          onBack: cubit.back,
          onNext: cubit.next,
          nextLabel: AppStrings.skip,
          child: Padding(
            padding: const EdgeInsets.symmetric(vertical: AppSpacing.xl),
            child: Column(
              children: [
                Icon(
                  Icons.photo_library_outlined,
                  size: 64,
                  color: Theme.of(context).colorScheme.outline,
                ),
                const SizedBox(height: AppSpacing.md),
                Text(
                  AppStrings.gallerySubtitle,
                  textAlign: TextAlign.center,
                  style: Theme.of(context).textTheme.bodySmall,
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}
