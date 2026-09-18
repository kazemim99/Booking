import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/person_name.dart';
import '../../../../core/utils/persian_digits.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../../domain/entities/onboarding_data.dart';
import '../cubit/onboarding_cubit.dart';
import '../cubit/onboarding_state.dart';
import '../widgets/step_scaffold.dart';

/// The fields this step validates, used to track which ones the user has left
/// (blurred) so an error appears under a field only after they've been in it.
enum _Field { name, fullName, phone, description }

/// Step 1 — business identity. Required: business name, owner full name,
/// phone, description. Email is optional.
///
/// Validation is shown inline, under each field — never as a snackbar, which
/// used to cover the Next button. A field shows its error once the user leaves
/// it (blur), and every invalid field shows its error when Next is pressed.
class BusinessInfoStep extends StatefulWidget {
  const BusinessInfoStep({super.key});

  @override
  State<BusinessInfoStep> createState() => _BusinessInfoStepState();
}

class _BusinessInfoStepState extends State<BusinessInfoStep> {
  late final TextEditingController _name;
  late final TextEditingController _fullName;
  late final TextEditingController _email;
  late final TextEditingController _phone;
  late final TextEditingController _description;

  /// The phone was verified at sign-in, so it is read-only — unless it is somehow
  /// missing, in which case locking a required field would dead-end onboarding.
  late final bool _phoneLocked;

  final Set<_Field> _touched = {};
  bool _submitted = false;

  @override
  void initState() {
    super.initState();
    final info = context.read<OnboardingCubit>().state.data.businessInfo;
    _name = TextEditingController(text: info.businessName);
    _fullName = TextEditingController(
      text: PersonName.join(info.ownerFirstName, info.ownerLastName),
    );
    _email = TextEditingController(text: info.email);
    _phone = TextEditingController(text: info.phone);
    _description = TextEditingController(text: info.description);
    _phoneLocked = info.phone.trim().isNotEmpty;
  }

  @override
  void dispose() {
    _name.dispose();
    _fullName.dispose();
    _email.dispose();
    _phone.dispose();
    _description.dispose();
    super.dispose();
  }

  /// The error for [field], or null when it is valid. Pure — ignores whether the
  /// user has touched the field; [_errorFor] decides whether to show it.
  String? _validate(_Field field) {
    switch (field) {
      case _Field.name:
        return _name.text.trim().isEmpty ? AppStrings.fieldRequired : null;
      case _Field.fullName:
        if (_fullName.text.trim().isEmpty) return AppStrings.fieldRequired;
        return PersonName.split(_fullName.text) == null
            ? AppStrings.fullNameNeedsBoth
            : null;
      case _Field.phone:
        return _phone.text.trim().isEmpty ? AppStrings.fieldRequired : null;
      case _Field.description:
        return _description.text.trim().isEmpty
            ? AppStrings.fieldRequired
            : null;
    }
  }

  String? _errorFor(_Field field) =>
      (_submitted || _touched.contains(field)) ? _validate(field) : null;

  void _markTouched(_Field field) => setState(() => _touched.add(field));

  void _commit() {
    final name = PersonName.split(_fullName.text);
    context.read<OnboardingCubit>().updateBusinessInfo(
          BusinessInfo(
            businessName: _name.text.trim(),
            // Only sent once split succeeds (Next is blocked otherwise); both parts
            // are required by the backend.
            ownerFirstName: name?.first ?? '',
            ownerLastName: name?.last ?? '',
            email: _email.text.trim(),
            phone: _phone.text.trim(),
            description: _description.text.trim(),
          ),
        );
  }

  void _onNext() {
    final anyInvalid = _Field.values.any((f) => _validate(f) != null);
    if (anyInvalid) {
      // Show every invalid field's message inline. No snackbar: the fields
      // themselves say what is wrong, and a snackbar would cover this button.
      setState(() => _submitted = true);
      return;
    }
    _commit();
    context.read<OnboardingCubit>().next();
  }

  @override
  Widget build(BuildContext context) {
    final saving = context.select<OnboardingCubit, bool>((c) => c.state.isSaving);
    return StepScaffold(
      title: AppStrings.businessInfoTitle,
      subtitle: AppStrings.businessInfoSubtitle,
      loading: saving,
      onNext: _onNext,
      child: Column(
        children: [
          AppTextField(
            controller: _name,
            label: AppStrings.businessName,
            isRequired: true,
            errorText: _errorFor(_Field.name),
            onBlur: () => _markTouched(_Field.name),
            onChanged: (_) => setState(() {}),
            key: const Key('onboarding-business-name'),
          ),
          const SizedBox(height: AppSpacing.md),
          AppTextField(
            controller: _fullName,
            label: AppStrings.ownerFullName,
            hint: AppStrings.ownerFullNameHint,
            isRequired: true,
            errorText: _errorFor(_Field.fullName),
            onBlur: () => _markTouched(_Field.fullName),
            onChanged: (_) => setState(() {}),
            key: const Key('onboarding-owner-full-name'),
          ),
          const SizedBox(height: AppSpacing.md),
          AppTextField(
            controller: _phone,
            label: AppStrings.businessPhone,
            hint: _phoneLocked ? AppStrings.businessPhoneFromAccount : null,
            isRequired: true,
            readOnly: _phoneLocked,
            keyboardType: TextInputType.phone,
            contentDirection: TextDirection.ltr,
            inputFormatters: const [DigitsOnlyInputFormatter()],
            maxLength: 11,
            errorText: _errorFor(_Field.phone),
            onBlur: _phoneLocked ? null : () => _markTouched(_Field.phone),
            onChanged: (_) => setState(() {}),
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
            isRequired: true,
            // A real text area: several visible lines that grow with the text.
            keyboardType: TextInputType.multiline,
            minLines: 3,
            maxLines: 6,
            errorText: _errorFor(_Field.description),
            onBlur: () => _markTouched(_Field.description),
            onChanged: (_) => setState(() {}),
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
