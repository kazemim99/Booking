import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/contacts/contact_picker.dart';
import '../../../../core/widgets/app_button.dart';
import '../../../../core/widgets/app_snackbar.dart';
import '../../../home/presentation/widgets/customer_form_dialog.dart';
import '../cubit/onboarding_cubit.dart';

/// Step 8 — registration complete. [onDone] re-checks auth status so the
/// refreshed provider status routes the user into the dashboard.
///
/// Also offers, optionally, to add the salon's regular customers now (typed
/// in, or ticked in the phone's contact picker where the phone has one) so
/// booking them later is a tap (spec: provider-customer-book K6). Skipping it
/// is just pressing the dashboard button, exactly as before.
class CompletionStep extends StatefulWidget {
  final VoidCallback onDone;

  /// The phone's contact picker; the platform's when null (tests pass a fake).
  final ContactPicker? contactPicker;

  const CompletionStep({super.key, required this.onDone, this.contactPicker});

  @override
  State<CompletionStep> createState() => _CompletionStepState();
}

class _CompletionStepState extends State<CompletionStep> {
  late final ContactPicker _contacts =
      widget.contactPicker ?? ContactPicker.platform();
  int _added = 0;
  bool _busy = false;

  Future<void> _add() async {
    final cubit = context.read<OnboardingCubit>();
    final draft = await showCustomerForm(context);
    if (draft == null || !mounted) return;
    setState(() => _busy = true);
    final failure = await cubit.addCustomer(draft);
    if (!mounted) return;
    setState(() {
      _busy = false;
      if (failure == null) _added++;
    });
    failure == null
        ? AppSnackbar.success(context, AppStrings.customerSaved)
        : AppSnackbar.error(context, failure);
  }

  Future<void> _import() async {
    final cubit = context.read<OnboardingCubit>();
    final picked = await _contacts.pick();
    if (!mounted) return;
    if (picked.isEmpty) {
      AppSnackbar.info(context, AppStrings.customerContactsNothing);
      return;
    }
    setState(() => _busy = true);
    final (added, failure) = await cubit.importCustomers(picked);
    if (!mounted) return;
    setState(() {
      _busy = false;
      _added += added;
    });
    failure == null
        ? AppSnackbar.success(context, AppStrings.onboardingCustomersAdded(added))
        : AppSnackbar.error(context, failure);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.check_circle_outline,
                size: 88, color: AppColors.success),
            const SizedBox(height: AppSpacing.lg),
            Text(
              AppStrings.completionTitle,
              style: theme.textTheme.headlineSmall,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: AppSpacing.sm),
            Text(
              AppStrings.completionBody,
              style: theme.textTheme.bodyMedium,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: AppSpacing.xl),
            _customers(theme),
            const SizedBox(height: AppSpacing.xl),
            AppButton(
              key: const Key('onboarding-go-to-dashboard'),
              label: AppStrings.goToDashboard,
              onPressed: _busy ? null : widget.onDone,
            ),
          ],
        ),
      ),
    );
  }

  Widget _customers(ThemeData theme) {
    return Container(
      key: const Key('onboarding-customers'),
      padding: const EdgeInsets.all(AppSpacing.md),
      decoration: BoxDecoration(
        color: AppColors.primarySoft,
        borderRadius: BorderRadius.circular(AppRadius.card),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            AppStrings.onboardingCustomersTitle,
            style: theme.textTheme.titleMedium,
          ),
          const SizedBox(height: AppSpacing.xs),
          Text(
            AppStrings.onboardingCustomersBody,
            style: theme.textTheme.bodySmall,
          ),
          const SizedBox(height: AppSpacing.md),
          // Expanded: this app's themed buttons are infinite-width and crash a bare Row.
          Row(
            children: [
              Expanded(
                child: OutlinedButton.icon(
                  key: const Key('onboarding-customer-add'),
                  onPressed: _busy ? null : _add,
                  icon: const Icon(Icons.person_add_alt_1_outlined,
                      size: AppIconSize.action),
                  label: const Text(AppStrings.customerAdd),
                ),
              ),
              if (_contacts.isSupported) ...[
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: OutlinedButton.icon(
                    key: const Key('onboarding-customer-import'),
                    onPressed: _busy ? null : _import,
                    icon: const Icon(Icons.contact_phone_outlined,
                        size: AppIconSize.action),
                    label: const Text(AppStrings.customerImportContacts),
                  ),
                ),
              ],
            ],
          ),
          if (_added > 0) ...[
            const SizedBox(height: AppSpacing.sm),
            Text(
              AppStrings.onboardingCustomersAdded(_added),
              key: const Key('onboarding-customers-added'),
              style: const TextStyle(color: AppColors.success),
            ),
          ],
        ],
      ),
    );
  }
}
