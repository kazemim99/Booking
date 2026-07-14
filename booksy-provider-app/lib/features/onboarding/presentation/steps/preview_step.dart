import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../domain/entities/onboarding_data.dart';
import '../cubit/onboarding_cubit.dart';
import '../cubit/onboarding_state.dart';
import '../widgets/step_scaffold.dart';

/// Step 7 — review & confirm. Submitting calls step-9/complete.
class PreviewStep extends StatelessWidget {
  const PreviewStep({super.key});

  @override
  Widget build(BuildContext context) {
    final cubit = context.read<OnboardingCubit>();
    return BlocBuilder<OnboardingCubit, OnboardingState>(
      builder: (context, state) {
        final data = state.data;
        final info = data.businessInfo;
        final address = data.address;
        final category = BusinessCategory.all
            .where((c) => c.id == data.categoryId)
            .map((c) => c.label)
            .firstOrNull;
        final openDays = data.businessHours.where((d) => d.isOpen).length;

        return StepScaffold(
          title: AppStrings.previewTitle,
          subtitle: AppStrings.previewSubtitle,
          loading: state.isSaving,
          onBack: cubit.back,
          onNext: cubit.complete,
          nextLabel: AppStrings.confirmAndSubmit,
          child: Column(
            children: [
              _Section(
                title: AppStrings.businessInfoTitle,
                onEdit: () => cubit.goToStep(1),
                rows: {
                  AppStrings.businessName: info.businessName,
                  AppStrings.ownerFirstName: info.ownerFirstName,
                  AppStrings.ownerLastName: info.ownerLastName,
                  AppStrings.businessPhone: info.phone,
                  if (info.email.isNotEmpty) AppStrings.emailOptional: info.email,
                },
              ),
              _Section(
                title: AppStrings.categoryTitle,
                onEdit: () => cubit.goToStep(2),
                rows: {AppStrings.categoryTitle: category ?? '—'},
              ),
              _Section(
                title: AppStrings.locationTitle,
                onEdit: () => cubit.goToStep(3),
                rows: {
                  AppStrings.addressLine1: address.addressLine1,
                  AppStrings.city: address.city,
                  if (address.province.isNotEmpty)
                    AppStrings.province: address.province,
                },
              ),
              _Section(
                title: AppStrings.servicesTitle,
                onEdit: () => cubit.goToStep(4),
                rows: {
                  for (final s in data.services)
                    s.name:
                        '${s.durationHours * 60 + s.durationMinutes} دقیقه · ${s.price.toStringAsFixed(0)} تومان',
                },
              ),
              _Section(
                title: AppStrings.hoursTitle,
                onEdit: () => cubit.goToStep(5),
                rows: {AppStrings.hoursTitle: '$openDays روز کاری'},
              ),
            ],
          ),
        );
      },
    );
  }
}

class _Section extends StatelessWidget {
  final String title;
  final Map<String, String> rows;
  final VoidCallback onEdit;

  const _Section({
    required this.title,
    required this.rows,
    required this.onEdit,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      margin: const EdgeInsets.only(bottom: AppSpacing.md),
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(title, style: theme.textTheme.titleMedium),
                TextButton(onPressed: onEdit, child: const Text(AppStrings.edit)),
              ],
            ),
            if (rows.isEmpty)
              Text('—', style: theme.textTheme.bodySmall)
            else
              ...rows.entries.map(
                (e) => Padding(
                  padding: const EdgeInsets.symmetric(vertical: 2),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        flex: 2,
                        child: Text(e.key, style: theme.textTheme.bodySmall),
                      ),
                      Expanded(
                        flex: 3,
                        child: Text(
                          e.value.isEmpty ? '—' : e.value,
                          style: theme.textTheme.bodyMedium,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
