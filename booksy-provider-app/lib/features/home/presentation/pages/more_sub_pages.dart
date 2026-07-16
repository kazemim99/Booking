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
import '../../../onboarding/domain/entities/onboarding_data.dart'
    show ClockTime, DayHours;
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

/// More → ساعات کاری (spec: provider-working-hours-editing).
class BusinessHoursPage extends StatelessWidget {
  const BusinessHoursPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider<BusinessHoursCubit>(
      create: (_) => getIt<BusinessHoursCubit>()..load(),
      child: const BusinessHoursView(),
    );
  }
}

/// Separated from [BusinessHoursPage] so tests can pump it with a fake cubit.
class BusinessHoursView extends StatefulWidget {
  const BusinessHoursView({super.key});

  @override
  State<BusinessHoursView> createState() => _BusinessHoursViewState();
}

class _BusinessHoursViewState extends State<BusinessHoursView> {
  bool _saving = false;

  /// Saturday-first display order (Iranian week; backend 0=Sunday…6=Saturday).
  static const List<int> _dayOrder = [6, 0, 1, 2, 3, 4, 5];

  Future<void> _save(BusinessHoursCubit cubit) async {
    setState(() => _saving = true);
    final failure = await cubit.save();
    if (!mounted) return;
    if (failure == null) {
      Navigator.of(context).pop();
      AppSnackbar.success(context, AppStrings.hoursSaved);
    } else {
      // Failure preserves the edited week (spec).
      setState(() => _saving = false);
      AppSnackbar.error(context, failure.message);
    }
  }

  Future<void> _pickTime(
    BuildContext context, {
    required ClockTime? current,
    required void Function(ClockTime) onPicked,
  }) async {
    final picked = await showTimePicker(
      context: context,
      initialTime: TimeOfDay(
          hour: current?.hours ?? 9, minute: current?.minutes ?? 0),
    );
    if (picked != null) onPicked(ClockTime(picked.hour, picked.minute));
  }

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<BusinessHoursCubit, MoreState<List<DayHours>>>(
      builder: (context, state) {
        final cubit = context.read<BusinessHoursCubit>();
        return _MoreSubScaffold<List<DayHours>>(
          title: AppStrings.moreWorkingHours,
          state: state,
          onRetry: cubit.load,
          bodyBuilder: (context, days) {
            final byDay = {for (final d in days) d.dayOfWeek: d};
            return ListView(
              padding: const EdgeInsets.all(AppSpacing.md),
              children: [
                for (final dayOfWeek in _dayOrder)
                  if (byDay[dayOfWeek] != null)
                    _dayRow(context, cubit, byDay[dayOfWeek]!),
                const SizedBox(height: AppSpacing.lg),
                AppButton(
                  key: const Key('hours-save'),
                  label: AppStrings.hoursSave,
                  loading: _saving,
                  onPressed: () => _save(cubit),
                ),
              ],
            );
          },
        );
      },
    );
  }

  Widget _dayRow(
      BuildContext context, BusinessHoursCubit cubit, DayHours day) {
    return Container(
      margin: const EdgeInsets.only(bottom: AppSpacing.sm),
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.card,
        vertical: AppSpacing.xs,
      ),
      decoration: BoxDecoration(
        color: day.isOpen ? Colors.white : AppColors.surfaceSoft,
        borderRadius: BorderRadius.circular(AppRadius.card),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        children: [
          Row(
            children: [
              Switch(
                key: Key('hours-switch-${day.dayOfWeek}'),
                value: day.isOpen,
                activeThumbColor: AppColors.primary,
                onChanged: (v) => cubit.toggleDay(day.dayOfWeek, v),
              ),
              const SizedBox(width: AppSpacing.xs),
              Expanded(
                child: Text(
                  AppStrings.weekDays[day.dayOfWeek % 7],
                  style: TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w600,
                    color: day.isOpen ? AppColors.ink : AppColors.muted,
                  ),
                ),
              ),
              if (!day.isOpen)
                const Text(
                  AppStrings.hoursClosedDay,
                  style: TextStyle(fontSize: 13, color: AppColors.muted),
                )
              else ...[
                TextButton(
                  key: Key('hours-open-${day.dayOfWeek}'),
                  onPressed: () => _pickTime(
                    context,
                    current: day.openTime,
                    onPicked: (t) => cubit.setOpenTime(day.dayOfWeek, t),
                  ),
                  child: Text(day.openTime?.label ?? '—'),
                ),
                const Text('–',
                    style: TextStyle(color: AppColors.muted)),
                TextButton(
                  key: Key('hours-close-${day.dayOfWeek}'),
                  onPressed: () => _pickTime(
                    context,
                    current: day.closeTime,
                    onPicked: (t) => cubit.setCloseTime(day.dayOfWeek, t),
                  ),
                  child: Text(day.closeTime?.label ?? '—'),
                ),
              ],
            ],
          ),
          if (day.isOpen && day.breaks.isNotEmpty)
            Align(
              alignment: AlignmentDirectional.centerStart,
              child: Padding(
                padding: const EdgeInsets.only(bottom: AppSpacing.xs),
                child: Wrap(
                  spacing: AppSpacing.xs,
                  children: [
                    for (final b in day.breaks)
                      Chip(
                        key: Key(
                            'hours-break-${day.dayOfWeek}-${b.start.label}'),
                        label: Text(
                          '${AppStrings.hoursBreak} ${b.start.label}–${b.end.label}',
                          style: const TextStyle(fontSize: 11),
                        ),
                        backgroundColor: AppColors.surfaceSoft,
                        side: const BorderSide(color: AppColors.border),
                        visualDensity: VisualDensity.compact,
                      ),
                  ],
                ),
              ),
            ),
        ],
      ),
    );
  }
}

/// More → تعطیلات و مرخصی (spec: provider-holidays-management).
class HolidaysPage extends StatelessWidget {
  const HolidaysPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider<HolidaysCubit>(
      create: (_) => getIt<HolidaysCubit>()..load(),
      child: const HolidaysView(),
    );
  }
}

/// Separated from [HolidaysPage] so tests can pump it with a fake cubit.
class HolidaysView extends StatelessWidget {
  const HolidaysView({super.key});

  static String _dm(DateTime d) => '${d.day}/${d.month}/${d.year}';

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<HolidaysCubit, MoreState<List<ProviderHoliday>>>(
      builder: (context, state) =>
          _MoreSubScaffold<List<ProviderHoliday>>(
        title: AppStrings.moreHolidays,
        state: state,
        onRetry: context.read<HolidaysCubit>().load,
        actions: [
          IconButton(
            key: const Key('holiday-add'),
            tooltip: AppStrings.holidayAdd,
            icon: const Icon(Icons.add_circle_outline,
                color: AppColors.primary),
            onPressed: () =>
                _HolidayFormSheet.show(context, context.read<HolidaysCubit>()),
          ),
        ],
        bodyBuilder: (context, holidays) => holidays.isEmpty
            ? AppEmptyState(
                icon: Icons.beach_access_outlined,
                message: AppStrings.holidaysEmpty,
                actionLabel: '+ ${AppStrings.holidayAdd}',
                onAction: () => _HolidayFormSheet.show(
                    context, context.read<HolidaysCubit>()),
              )
            : ListView.separated(
                padding: const EdgeInsets.all(AppSpacing.md),
                itemCount: holidays.length,
                separatorBuilder: (_, _) =>
                    const Divider(color: AppColors.divider, height: 1),
                itemBuilder: (context, i) {
                  final h = holidays[i];
                  return ListTile(
                    key: Key('holiday-row-${h.id}'),
                    contentPadding: EdgeInsets.zero,
                    leading: const Icon(Icons.event_busy_outlined,
                        color: AppColors.primary),
                    title: Text(
                      h.reason,
                      style: const TextStyle(
                          fontSize: 15, color: AppColors.ink),
                    ),
                    subtitle: Text(
                      [
                        _dm(h.date),
                        if (h.isRecurring) AppStrings.holidayRecurringBadge,
                      ].join(' · '),
                      style: const TextStyle(
                          fontSize: 12, color: AppColors.muted),
                    ),
                    trailing: IconButton(
                      key: Key('holiday-remove-${h.id}'),
                      icon: const Icon(Icons.delete_outline,
                          size: AppIconSize.action, color: AppColors.danger),
                      onPressed: () => _confirmRemove(context, h),
                    ),
                  );
                },
              ),
      ),
    );
  }

  Future<void> _confirmRemove(
      BuildContext context, ProviderHoliday holiday) async {
    final cubit = context.read<HolidaysCubit>();
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text(AppStrings.holidayRemoveConfirmTitle),
        content: Text(AppStrings.holidayRemoveConfirmBody(_dm(holiday.date))),
        actions: [
          TextButton(
            key: const Key('holiday-remove-cancel'),
            onPressed: () => Navigator.pop(dialogContext, false),
            child: const Text(AppStrings.cancel),
          ),
          TextButton(
            key: const Key('holiday-remove-confirm'),
            onPressed: () => Navigator.pop(dialogContext, true),
            style: TextButton.styleFrom(foregroundColor: AppColors.danger),
            child: const Text(AppStrings.staffRemoveConfirm),
          ),
        ],
      ),
    );
    if (confirmed != true || !context.mounted) return;
    final failure = await cubit.removeHoliday(holiday.id);
    if (!context.mounted) return;
    if (failure == null) {
      AppSnackbar.success(context, AppStrings.holidayRemoved);
    } else {
      AppSnackbar.error(context, failure.message);
    }
  }
}

/// Add-holiday form sheet (date picker + reason + yearly recurrence).
class _HolidayFormSheet extends StatefulWidget {
  final HolidaysCubit cubit;

  const _HolidayFormSheet({required this.cubit});

  static Future<void> show(BuildContext context, HolidaysCubit cubit) {
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
        child: _HolidayFormSheet(cubit: cubit),
      ),
    );
  }

  @override
  State<_HolidayFormSheet> createState() => _HolidayFormSheetState();
}

class _HolidayFormSheetState extends State<_HolidayFormSheet> {
  final _reason = TextEditingController();
  DateTime _date = DateTime.now().add(const Duration(days: 1));
  bool _recurring = false;
  bool _submitting = false;

  @override
  void dispose() {
    _reason.dispose();
    super.dispose();
  }

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: DateTime(now.year, now.month, now.day),
      lastDate: now.add(const Duration(days: 365)),
    );
    if (picked != null) setState(() => _date = picked);
  }

  Future<void> _submit() async {
    setState(() => _submitting = true);
    final failure = await widget.cubit.addHoliday(
      date: _date,
      reason: _reason.text.trim(),
      isRecurring: _recurring,
    );
    if (!mounted) return;
    if (failure == null) {
      Navigator.pop(context);
      AppSnackbar.success(context, AppStrings.holidayAdded);
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
            const Text(
              AppStrings.holidayAdd,
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w700,
                color: AppColors.ink,
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            OutlinedButton.icon(
              key: const Key('holiday-date'),
              onPressed: _pickDate,
              icon: const Icon(Icons.event_outlined,
                  size: AppIconSize.action),
              label: Text(HolidaysView._dm(_date)),
            ),
            const SizedBox(height: AppSpacing.sm),
            AppTextField(
              key: const Key('holiday-reason'),
              controller: _reason,
              label: AppStrings.holidayReason,
              onChanged: (_) => setState(() {}),
            ),
            SwitchListTile(
              key: const Key('holiday-recurring'),
              contentPadding: EdgeInsets.zero,
              value: _recurring,
              onChanged: (v) => setState(() => _recurring = v),
              title: const Text(
                AppStrings.holidayRecurring,
                style: TextStyle(fontSize: 14, color: AppColors.ink),
              ),
            ),
            AppButton(
              key: const Key('holiday-save'),
              label: AppStrings.holidaySave,
              loading: _submitting,
              onPressed: _reason.text.trim().isEmpty ? null : _submit,
            ),
            const SizedBox(height: AppSpacing.sm),
          ],
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
