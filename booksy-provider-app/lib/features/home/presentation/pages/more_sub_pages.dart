import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/app_button.dart';
import '../../../../core/widgets/app_card.dart';
import '../../../../core/widgets/app_empty_state.dart';
import '../../../../core/widgets/app_error_state.dart';
import '../../../../core/widgets/app_snackbar.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../../domain/entities/composer_models.dart';
import '../../domain/entities/more_models.dart';
import '../cubit/more_cubits.dart';

String _formatMoney(double amount, String currency) {
  final rounded =
      amount == amount.roundToDouble() ? '${amount.round()}' : '$amount';
  return currency.isEmpty ? rounded : '$rounded $currency';
}

/// Shared scaffold for the three More read surfaces
/// (spec: provider-more-hub — standard treatments, retry).
class _MoreSubScaffold<T> extends StatelessWidget {
  final String title;
  final MoreState<T> state;
  final VoidCallback onRetry;
  final Widget Function(BuildContext, T) bodyBuilder;
  final List<Widget> actions;

  const _MoreSubScaffold({
    super.key,
    required this.title,
    required this.state,
    required this.onRetry,
    required this.bodyBuilder,
    this.actions = const [],
  });

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Colors.white,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        title: Text(
          title,
          style: const TextStyle(
            fontSize: 17,
            fontWeight: FontWeight.w700,
            color: AppColors.ink,
          ),
        ),
        actions: actions,
      ),
      body: switch (state.status) {
        MoreStatus.loading =>
          const Center(child: CircularProgressIndicator()),
        MoreStatus.failed => AppErrorState(
            message: state.error ?? AppStrings.homeLoadError,
            onRetry: onRetry,
          ),
        MoreStatus.ready => bodyBuilder(context, state.data as T),
      },
    );
  }
}

/// More → گزارش‌ها.
class InsightsPage extends StatelessWidget {
  const InsightsPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider<InsightsCubit>(
      create: (_) => getIt<InsightsCubit>()..load(),
      child: const InsightsView(),
    );
  }
}

/// Separated from [InsightsPage] so tests can pump it with a fake cubit.
class InsightsView extends StatelessWidget {
  const InsightsView({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<InsightsCubit, MoreState<InsightsSummary>>(
      builder: (context, state) => _MoreSubScaffold<InsightsSummary>(
        title: AppStrings.moreInsights,
        state: state,
        onRetry: context.read<InsightsCubit>().load,
        bodyBuilder: (context, insights) => ListView(
          padding: const EdgeInsets.all(AppSpacing.md),
          children: [
            const Text(
              AppStrings.insightsAllTime,
              style: TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w700,
                color: AppColors.muted,
              ),
            ),
            const SizedBox(height: AppSpacing.sm),
            Row(
              children: [
                _tile(AppStrings.insightsTotal, '${insights.totalBookings}',
                    key: 'insights-total'),
                const SizedBox(width: AppSpacing.sm),
                _tile(AppStrings.insightsCompleted,
                    '${insights.completedBookings}',
                    color: AppColors.success),
              ],
            ),
            const SizedBox(height: AppSpacing.sm),
            Row(
              children: [
                _tile(AppStrings.insightsCancelled,
                    '${insights.cancelledBookings}'),
                const SizedBox(width: AppSpacing.sm),
                _tile(AppStrings.insightsNoShow, '${insights.noShowBookings}'),
              ],
            ),
            const SizedBox(height: AppSpacing.md),
            _wideTile(
              AppStrings.insightsTurnover,
              _formatMoney(insights.totalRevenue, insights.currency),
            ),
            const SizedBox(height: AppSpacing.sm),
            _wideTile(
              AppStrings.insightsCompletedRevenue,
              _formatMoney(insights.completedRevenue, insights.currency),
            ),
            const SizedBox(height: AppSpacing.lg),
            const Text(
              AppStrings.insightsLast30,
              style: TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w700,
                color: AppColors.muted,
              ),
            ),
            const SizedBox(height: AppSpacing.sm),
            Row(
              children: [
                _tile(AppStrings.insightsTotal,
                    '${insights.bookingsTrailing30d}',
                    key: 'insights-30d'),
                const Expanded(child: SizedBox()),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _tile(String label, String value, {Color? color, String? key}) {
    return Expanded(
      child: AppCard(
        key: key == null ? null : Key(key),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              value,
              style: TextStyle(
                fontSize: 22,
                fontWeight: FontWeight.w700,
                color: color ?? AppColors.ink,
              ),
            ),
            const SizedBox(height: AppSpacing.xs),
            Text(
              label,
              style: const TextStyle(fontSize: 12, color: AppColors.muted),
            ),
          ],
        ),
      ),
    );
  }

  Widget _wideTile(String label, String value) {
    return AppCard(
      child: Row(
        children: [
          Expanded(
            child: Text(
              label,
              style: const TextStyle(fontSize: 14, color: AppColors.ink),
            ),
          ),
          Text(
            value,
            style: const TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w700,
              color: AppColors.ink,
            ),
          ),
        ],
      ),
    );
  }
}

/// More → خدمات (read-only).
class ServicesPage extends StatelessWidget {
  const ServicesPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider<ServicesCubit>(
      create: (_) => getIt<ServicesCubit>()..load(),
      child: const ServicesView(),
    );
  }
}

/// Separated from [ServicesPage] so tests can pump it with a fake cubit.
class ServicesView extends StatelessWidget {
  const ServicesView({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<ServicesCubit, MoreState<List<ComposerService>>>(
      builder: (context, state) => _MoreSubScaffold<List<ComposerService>>(
        title: AppStrings.moreServices,
        state: state,
        onRetry: context.read<ServicesCubit>().load,
        bodyBuilder: (context, services) => services.isEmpty
            ? const AppEmptyState(
                icon: Icons.design_services_outlined,
                message: AppStrings.servicesEmpty,
              )
            : ListView.separated(
                padding: const EdgeInsets.all(AppSpacing.md),
                itemCount: services.length,
                separatorBuilder: (_, _) =>
                    const Divider(color: AppColors.divider, height: 1),
                itemBuilder: (context, i) {
                  final s = services[i];
                  return ListTile(
                    key: Key('service-row-${s.id}'),
                    contentPadding: EdgeInsets.zero,
                    title: Text(
                      s.name,
                      style:
                          const TextStyle(fontSize: 15, color: AppColors.ink),
                    ),
                    subtitle: Text(
                      AppStrings.serviceMeta(
                          s.durationMinutes, _formatMoney(s.price, '')),
                      style: const TextStyle(
                          fontSize: 12, color: AppColors.muted),
                    ),
                  );
                },
              ),
      ),
    );
  }
}

/// More → تیم (read-only).
class StaffPage extends StatelessWidget {
  const StaffPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider<StaffCubit>(
      create: (_) => getIt<StaffCubit>()..load(),
      child: const StaffView(),
    );
  }
}

/// Separated from [StaffPage] so tests can pump it with a fake cubit.
class StaffView extends StatelessWidget {
  const StaffView({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<StaffCubit, MoreState<List<ProviderStaffMember>>>(
      builder: (context, state) =>
          _MoreSubScaffold<List<ProviderStaffMember>>(
        title: AppStrings.moreStaff,
        state: state,
        onRetry: context.read<StaffCubit>().load,
        actions: [
          IconButton(
            key: const Key('staff-add'),
            tooltip: AppStrings.staffAdd,
            icon: const Icon(Icons.person_add_alt, color: AppColors.primary),
            onPressed: () =>
                StaffFormSheet.show(context, context.read<StaffCubit>()),
          ),
        ],
        bodyBuilder: (context, staff) => staff.isEmpty
            ? AppEmptyState(
                icon: Icons.people_outline,
                message: AppStrings.staffEmpty,
                actionLabel: '+ ${AppStrings.staffAdd}',
                onAction: () =>
                    StaffFormSheet.show(context, context.read<StaffCubit>()),
              )
            : ListView.separated(
                padding: const EdgeInsets.all(AppSpacing.md),
                itemCount: staff.length,
                separatorBuilder: (_, _) =>
                    const Divider(color: AppColors.divider, height: 1),
                itemBuilder: (context, i) {
                  final m = staff[i];
                  return ListTile(
                    key: Key('staff-row-${m.id}'),
                    contentPadding: EdgeInsets.zero,
                    onTap: () => StaffFormSheet.show(
                        context, context.read<StaffCubit>(),
                        member: m),
                    leading: CircleAvatar(
                      backgroundColor: AppColors.primarySoft,
                      child: Text(
                        m.name.isNotEmpty ? m.name.characters.first : '؟',
                        style: const TextStyle(
                          color: AppColors.primary,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                    title: Text(
                      m.name,
                      style: TextStyle(
                        fontSize: 15,
                        color: m.isActive ? AppColors.ink : AppColors.muted,
                      ),
                    ),
                    subtitle: Text(
                      [
                        if (m.role.isNotEmpty) m.role,
                        if (!m.isActive) AppStrings.staffInactive,
                      ].join(' · '),
                      style: const TextStyle(
                          fontSize: 12, color: AppColors.muted),
                    ),
                    trailing: const Icon(Icons.chevron_left,
                        size: AppIconSize.action, color: AppColors.muted),
                  );
                },
              ),
      ),
    );
  }
}

/// More → مشخصات کسب‌وکار (spec: provider-business-profile-editing).
class BusinessProfilePage extends StatelessWidget {
  const BusinessProfilePage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider<BusinessProfileCubit>(
      create: (_) => getIt<BusinessProfileCubit>()..load(),
      child: const BusinessProfileView(),
    );
  }
}

/// Separated from [BusinessProfilePage] so tests can pump it with a fake
/// cubit.
class BusinessProfileView extends StatelessWidget {
  const BusinessProfileView({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<BusinessProfileCubit, MoreState<BusinessProfile>>(
      builder: (context, state) => _MoreSubScaffold<BusinessProfile>(
        title: AppStrings.moreBusinessProfile,
        state: state,
        onRetry: context.read<BusinessProfileCubit>().load,
        bodyBuilder: (context, profile) => _BusinessProfileForm(
          profile: profile,
          cubit: context.read<BusinessProfileCubit>(),
        ),
      ),
    );
  }
}

class _BusinessProfileForm extends StatefulWidget {
  final BusinessProfile profile;
  final BusinessProfileCubit cubit;

  const _BusinessProfileForm({required this.profile, required this.cubit});

  @override
  State<_BusinessProfileForm> createState() => _BusinessProfileFormState();
}

class _BusinessProfileFormState extends State<_BusinessProfileForm> {
  late final _name =
      TextEditingController(text: widget.profile.businessName);
  late final _description =
      TextEditingController(text: widget.profile.description);
  bool _saving = false;

  @override
  void dispose() {
    _name.dispose();
    _description.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    setState(() => _saving = true);
    final failure = await widget.cubit.save(
      businessName: _name.text.trim(),
      description: _description.text.trim(),
    );
    if (!mounted) return;
    if (failure == null) {
      Navigator.of(context).pop();
      AppSnackbar.success(context, AppStrings.businessProfileSaved);
    } else {
      // Failure preserves the edited values (spec).
      setState(() => _saving = false);
      AppSnackbar.error(context, failure.message);
    }
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(AppSpacing.md),
      children: [
        AppTextField(
          key: const Key('business-name'),
          controller: _name,
          label: AppStrings.businessProfileName,
          onChanged: (_) => setState(() {}),
        ),
        const SizedBox(height: AppSpacing.md),
        AppTextField(
          key: const Key('business-description'),
          controller: _description,
          label: AppStrings.businessProfileDescription,
          maxLines: 5,
          minLines: 3,
        ),
        const SizedBox(height: AppSpacing.lg),
        AppButton(
          key: const Key('business-save'),
          label: AppStrings.businessProfileSave,
          loading: _saving,
          onPressed: _name.text.trim().isEmpty ? null : _save,
        ),
      ],
    );
  }
}

/// Add/edit form for a team member (spec: provider-staff-management).
/// Pre-filled = edit (offers a confirm-guarded remove); empty = add.
class StaffFormSheet extends StatefulWidget {
  final StaffCubit cubit;
  final ProviderStaffMember? member;

  const StaffFormSheet({super.key, required this.cubit, this.member});

  static Future<void> show(
    BuildContext context,
    StaffCubit cubit, {
    ProviderStaffMember? member,
  }) {
    return showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(
          top: Radius.circular(AppRadius.bottomSheet),
        ),
      ),
      builder: (_) => Padding(
        padding:
            EdgeInsets.only(bottom: MediaQuery.of(context).viewInsets.bottom),
        child: StaffFormSheet(cubit: cubit, member: member),
      ),
    );
  }

  @override
  State<StaffFormSheet> createState() => _StaffFormSheetState();
}

class _StaffFormSheetState extends State<StaffFormSheet> {
  late final _firstName =
      TextEditingController(text: widget.member?.firstName ?? '');
  late final _lastName =
      TextEditingController(text: widget.member?.lastName ?? '');
  late final _phone = TextEditingController(text: widget.member?.phone ?? '');
  late final _role = TextEditingController(text: widget.member?.role ?? '');
  bool _submitting = false;

  bool get _isEdit => widget.member != null;

  @override
  void dispose() {
    _firstName.dispose();
    _lastName.dispose();
    _phone.dispose();
    _role.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() => _submitting = true);
    final failure = _isEdit
        ? await widget.cubit.updateStaff(
            widget.member!.id,
            firstName: _firstName.text.trim(),
            lastName: _lastName.text.trim(),
            phoneNumber: _phone.text.trim(),
            role: _role.text.trim(),
          )
        : await widget.cubit.addStaff(
            firstName: _firstName.text.trim(),
            lastName: _lastName.text.trim(),
            phoneNumber: _phone.text.trim(),
            role: _role.text.trim(),
          );
    if (!mounted) return;
    if (failure == null) {
      Navigator.pop(context);
      AppSnackbar.success(
          context, _isEdit ? AppStrings.staffUpdated : AppStrings.staffAdded);
    } else {
      // Failure preserves the entered values for retry (spec).
      setState(() => _submitting = false);
      AppSnackbar.error(context, failure.message);
    }
  }

  Future<void> _remove() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text(AppStrings.staffRemoveConfirmTitle),
        content:
            Text(AppStrings.staffRemoveConfirmBody(widget.member!.name)),
        actions: [
          TextButton(
            key: const Key('staff-remove-cancel'),
            onPressed: () => Navigator.pop(dialogContext, false),
            child: const Text(AppStrings.cancel),
          ),
          TextButton(
            key: const Key('staff-remove-confirm'),
            onPressed: () => Navigator.pop(dialogContext, true),
            style: TextButton.styleFrom(foregroundColor: AppColors.danger),
            child: const Text(AppStrings.staffRemoveConfirm),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    setState(() => _submitting = true);
    final failure = await widget.cubit.removeStaff(widget.member!.id);
    if (!mounted) return;
    if (failure == null) {
      Navigator.pop(context);
      AppSnackbar.success(context, AppStrings.staffRemoved);
    } else {
      setState(() => _submitting = false);
      AppSnackbar.error(context, failure.message);
    }
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              _isEdit ? AppStrings.staffEdit : AppStrings.staffAdd,
              style: const TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w700,
                color: AppColors.ink,
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              key: const Key('staff-first-name'),
              controller: _firstName,
              label: AppStrings.staffFirstName,
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: AppSpacing.sm),
            AppTextField(
              key: const Key('staff-last-name'),
              controller: _lastName,
              label: AppStrings.staffLastName,
            ),
            const SizedBox(height: AppSpacing.sm),
            AppTextField(
              key: const Key('staff-phone'),
              controller: _phone,
              label: AppStrings.staffPhone,
              keyboardType: TextInputType.phone,
            ),
            const SizedBox(height: AppSpacing.sm),
            AppTextField(
              key: const Key('staff-role'),
              controller: _role,
              label: AppStrings.staffRole,
            ),
            const SizedBox(height: AppSpacing.md),
            AppButton(
              key: const Key('staff-save'),
              label: AppStrings.staffSave,
              loading: _submitting,
              onPressed:
                  _firstName.text.trim().isEmpty ? null : _submit,
            ),
            if (_isEdit)
              Align(
                alignment: AlignmentDirectional.center,
                child: TextButton(
                  key: const Key('staff-remove'),
                  onPressed: _submitting ? null : _remove,
                  style:
                      TextButton.styleFrom(foregroundColor: AppColors.danger),
                  child: const Text(AppStrings.staffRemove),
                ),
              ),
            const SizedBox(height: AppSpacing.sm),
          ],
        ),
      ),
    );
  }
}
