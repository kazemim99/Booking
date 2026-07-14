import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/persian_digits.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../../domain/entities/onboarding_data.dart';
import '../cubit/onboarding_cubit.dart';
import '../cubit/onboarding_state.dart';
import '../widgets/step_scaffold.dart';

/// Step 1 — business identity. Required: name, owner first/last, phone.
class BusinessInfoStep extends StatefulWidget {
  const BusinessInfoStep({super.key});

  @override
  State<BusinessInfoStep> createState() => _BusinessInfoStepState();
}

class _BusinessInfoStepState extends State<BusinessInfoStep> {
  late final TextEditingController _name;
  late final TextEditingController _firstName;
  late final TextEditingController _lastName;
  late final TextEditingController _email;
  late final TextEditingController _phone;
  late final TextEditingController _description;

  @override
  void initState() {
    super.initState();
    final info = context.read<OnboardingCubit>().state.data.businessInfo;
    _name = TextEditingController(text: info.businessName);
    _firstName = TextEditingController(text: info.ownerFirstName);
    _lastName = TextEditingController(text: info.ownerLastName);
    _email = TextEditingController(text: info.email);
    _phone = TextEditingController(text: info.phone);
    _description = TextEditingController(text: info.description);
  }

  @override
  void dispose() {
    _name.dispose();
    _firstName.dispose();
    _lastName.dispose();
    _email.dispose();
    _phone.dispose();
    _description.dispose();
    super.dispose();
  }

  void _commit() {
    context.read<OnboardingCubit>().updateBusinessInfo(
          BusinessInfo(
            businessName: _name.text.trim(),
            ownerFirstName: _firstName.text.trim(),
            ownerLastName: _lastName.text.trim(),
            email: _email.text.trim(),
            phone: _phone.text.trim(),
            description: _description.text.trim(),
          ),
        );
  }

  @override
  Widget build(BuildContext context) {
    final saving = context.select<OnboardingCubit, bool>((c) => c.state.isSaving);
    return StepScaffold(
      title: AppStrings.businessInfoTitle,
      subtitle: AppStrings.businessInfoSubtitle,
      loading: saving,
      onNext: () {
        _commit();
        context.read<OnboardingCubit>().next();
      },
      child: Column(
        children: [
          AppTextField(
            controller: _name,
            label: AppStrings.businessName,
            key: const Key('onboarding-business-name'),
          ),
          const SizedBox(height: AppSpacing.md),
          AppTextField(
            controller: _firstName,
            label: AppStrings.ownerFirstName,
            key: const Key('onboarding-owner-first-name'),
          ),
          const SizedBox(height: AppSpacing.md),
          AppTextField(
            controller: _lastName,
            label: AppStrings.ownerLastName,
            key: const Key('onboarding-owner-last-name'),
          ),
          const SizedBox(height: AppSpacing.md),
          AppTextField(
            controller: _phone,
            label: AppStrings.businessPhone,
            keyboardType: TextInputType.phone,
            contentDirection: TextDirection.ltr,
            inputFormatters: const [DigitsOnlyInputFormatter()],
            maxLength: 11,
            key: const Key('onboarding-phone'),
          ),
          const SizedBox(height: AppSpacing.md),
          AppTextField(
            controller: _email,
            label: AppStrings.emailOptional,
            keyboardType: TextInputType.emailAddress,
            contentDirection: TextDirection.ltr,
            key: const Key('onboarding-email'),
          ),
          const SizedBox(height: AppSpacing.md),
          AppTextField(
            controller: _description,
            label: AppStrings.businessDescription,
            key: const Key('onboarding-description'),
          ),
        ],
      ),
    );
  }
}

/// Convenience so steps can read the phase without a full BlocBuilder.
extension OnboardingSelect on OnboardingState {
  bool get busy => phase == OnboardingPhase.saving;
}
