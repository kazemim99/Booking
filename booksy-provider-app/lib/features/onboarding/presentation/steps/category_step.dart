import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../domain/entities/onboarding_data.dart';
import '../cubit/onboarding_cubit.dart';
import '../cubit/onboarding_state.dart';
import '../widgets/step_scaffold.dart';

/// Step 2 — business category (single select).
class CategoryStep extends StatelessWidget {
  const CategoryStep({super.key});

  @override
  Widget build(BuildContext context) {
    final cubit = context.read<OnboardingCubit>();
    return BlocBuilder<OnboardingCubit, OnboardingState>(
      builder: (context, state) {
        final selected = state.data.categoryId;
        return StepScaffold(
          title: AppStrings.categoryTitle,
          subtitle: AppStrings.categorySubtitle,
          loading: state.isSaving,
          onBack: cubit.back,
          onNext: cubit.next,
          child: Wrap(
            spacing: AppSpacing.md,
            runSpacing: AppSpacing.md,
            children: BusinessCategory.all.map((category) {
              final isSelected = selected == category.id;
              return SizedBox(
                width: 150,
                child: InkWell(
                  key: Key('category-${category.id}'),
                  borderRadius: BorderRadius.circular(AppRadius.md),
                  onTap: () => cubit.selectCategory(category.id),
                  child: Container(
                    padding: const EdgeInsets.symmetric(
                      vertical: AppSpacing.lg,
                      horizontal: AppSpacing.md,
                    ),
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(AppRadius.md),
                      border: Border.all(
                        color: isSelected
                            ? Theme.of(context).colorScheme.primary
                            : Theme.of(context).colorScheme.outline,
                        width: isSelected ? 2 : 1,
                      ),
                      color: isSelected
                          ? Theme.of(context)
                              .colorScheme
                              .primary
                              .withValues(alpha: 0.08)
                          : null,
                    ),
                    child: Column(
                      children: [
                        Text(category.emoji,
                            style: const TextStyle(fontSize: 32)),
                        const SizedBox(height: AppSpacing.sm),
                        Text(
                          category.label,
                          textAlign: TextAlign.center,
                          style: Theme.of(context).textTheme.bodyMedium,
                        ),
                      ],
                    ),
                  ),
                ),
              );
            }).toList(),
          ),
        );
      },
    );
  }
}
