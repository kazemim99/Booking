import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../../domain/entities/onboarding_data.dart';
import '../cubit/onboarding_cubit.dart';
import '../widgets/step_scaffold.dart';

/// Step 3 — address. Required: addressLine1 + city. Map coordinates are
/// optional (backend defaults lat/lng to 0), matching the Vue LocationStep.
///
/// Advancing from this step creates the organization draft on the server.
class LocationStep extends StatefulWidget {
  const LocationStep({super.key});

  @override
  State<LocationStep> createState() => _LocationStepState();
}

class _LocationStepState extends State<LocationStep> {
  late final TextEditingController _line1;
  late final TextEditingController _line2;
  late final TextEditingController _city;
  late final TextEditingController _province;
  late final TextEditingController _postalCode;

  @override
  void initState() {
    super.initState();
    final a = context.read<OnboardingCubit>().state.data.address;
    _line1 = TextEditingController(text: a.addressLine1);
    _line2 = TextEditingController(text: a.addressLine2);
    _city = TextEditingController(text: a.city);
    _province = TextEditingController(text: a.province);
    _postalCode = TextEditingController(text: a.postalCode);
  }

  @override
  void dispose() {
    _line1.dispose();
    _line2.dispose();
    _city.dispose();
    _province.dispose();
    _postalCode.dispose();
    super.dispose();
  }

  void _commit() {
    final existing = context.read<OnboardingCubit>().state.data.address;
    context.read<OnboardingCubit>().updateAddress(
          OnboardingAddress(
            addressLine1: _line1.text.trim(),
            addressLine2: _line2.text.trim(),
            city: _city.text.trim(),
            province: _province.text.trim(),
            postalCode: _postalCode.text.trim(),
            // Coordinates preserved if a map pin is added later.
            latitude: existing.latitude,
            longitude: existing.longitude,
          ),
        );
  }

  @override
  Widget build(BuildContext context) {
    final cubit = context.read<OnboardingCubit>();
    final saving = context.select<OnboardingCubit, bool>((c) => c.state.isSaving);
    return StepScaffold(
      title: AppStrings.locationTitle,
      subtitle: AppStrings.locationSubtitle,
      loading: saving,
      onBack: cubit.back,
      onNext: () {
        _commit();
        cubit.next();
      },
      child: Column(
        children: [
          AppTextField(
            controller: _line1,
            label: AppStrings.addressLine1,
            key: const Key('onboarding-address-line1'),
          ),
          const SizedBox(height: AppSpacing.md),
          AppTextField(
            controller: _line2,
            label: AppStrings.addressLine2,
            key: const Key('onboarding-address-line2'),
          ),
          const SizedBox(height: AppSpacing.md),
          AppTextField(
            controller: _city,
            label: AppStrings.city,
            key: const Key('onboarding-city'),
          ),
          const SizedBox(height: AppSpacing.md),
          AppTextField(
            controller: _province,
            label: AppStrings.province,
            key: const Key('onboarding-province'),
          ),
          const SizedBox(height: AppSpacing.md),
          AppTextField(
            controller: _postalCode,
            label: AppStrings.postalCode,
            keyboardType: TextInputType.number,
            contentDirection: TextDirection.ltr,
            key: const Key('onboarding-postal-code'),
          ),
        ],
      ),
    );
  }
}
