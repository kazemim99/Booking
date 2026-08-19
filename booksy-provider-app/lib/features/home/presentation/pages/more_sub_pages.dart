import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/app_button.dart';
import '../../../../core/widgets/app_card.dart';
import '../../../../core/widgets/app_empty_state.dart';
import '../../../../core/widgets/app_error_state.dart';
import '../../../../core/widgets/app_page_scaffold.dart';
import '../../../../core/widgets/app_snackbar.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../../../onboarding/domain/entities/onboarding_data.dart'
    show BreakTime, ClockTime, DayHours;
import '../../domain/entities/composer_models.dart';
import '../../domain/entities/more_models.dart';
import '../cubit/more_cubits.dart';

/// Green "+ add" link row pinned at the top of a populated list — the
/// ColiRide list-add affordance. The chrome icon stays for muscle memory,
/// but this is the discoverable path (spec ruling after visual QA: the tiny
/// header icon was easy to miss on first use).
class _AddLinkRow extends StatelessWidget {
  final String label;
  final VoidCallback onTap;

  const _AddLinkRow({super.key, required this.label, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: AlignmentDirectional.centerStart,
      child: TextButton.icon(
        onPressed: onTap,
        icon: const Icon(Icons.add_circle,
            size: AppIconSize.action, color: AppColors.success),
        label: Text(label),
        style: TextButton.styleFrom(
          foregroundColor: AppColors.success,
          padding: const EdgeInsets.symmetric(
              horizontal: AppSpacing.xs, vertical: AppSpacing.sm),
          textStyle:
              const TextStyle(fontSize: 14, fontWeight: FontWeight.bold),
        ),
      ),
    );
  }
}

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
    return AppPageScaffold(
      title: title,
      actions: actions,
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
        actions: [
          IconButton(
            key: const Key('service-add'),
            tooltip: AppStrings.serviceAdd,
            // Green add affordance on the blue chrome (ColiRide sub-page
            // pattern): "add" reads as the positive accent, not brand blue,
            // which would vanish against the blue header.
            icon: const Icon(Icons.add_circle, color: AppColors.success),
            onPressed: () => _ServiceFormSheet.show(
                context, context.read<ServicesCubit>()),
          ),
        ],
        bodyBuilder: (context, services) => services.isEmpty
            ? AppEmptyState.add(
                icon: Icons.design_services_outlined,
                message: AppStrings.servicesEmpty,
                actionLabel: '+ ${AppStrings.serviceAdd}',
                onAction: () => _ServiceFormSheet.show(
                    context, context.read<ServicesCubit>()),
              )
            : ListView.separated(
                padding: const EdgeInsets.all(AppSpacing.md),
                itemCount: services.length + 1,
                separatorBuilder: (_, _) =>
                    const Divider(color: AppColors.divider, height: 1),
                itemBuilder: (context, i) {
                  if (i == 0) {
                    return _AddLinkRow(
                      key: const Key('service-add-row'),
                      label: AppStrings.serviceAdd,
                      onTap: () => _ServiceFormSheet.show(
                          context, context.read<ServicesCubit>()),
                    );
                  }
                  final s = services[i - 1];
                  return ListTile(
                    key: Key('service-row-${s.id}'),
                    contentPadding: EdgeInsets.zero,
                    onTap: () => _ServiceFormSheet.show(
                        context, context.read<ServicesCubit>(),
                        initial: s),
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
                    trailing: IconButton(
                      key: Key('service-remove-${s.id}'),
                      icon: const Icon(Icons.delete_outline,
                          size: AppIconSize.action, color: AppColors.danger),
                      onPressed: () => _confirmRemoveService(
                          context, context.read<ServicesCubit>(), s),
                    ),
                  );
                },
              ),
      ),
    );
  }

  Future<void> _confirmRemoveService(
    BuildContext context,
    ServicesCubit cubit,
    ComposerService service,
  ) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text(AppStrings.serviceRemoveConfirmTitle),
        content: Text(AppStrings.serviceRemoveConfirmBody(service.name)),
        actions: [
          TextButton(
            key: const Key('service-remove-cancel'),
            onPressed: () => Navigator.pop(dialogContext, false),
            child: const Text(AppStrings.cancel),
          ),
          TextButton(
            key: const Key('service-remove-confirm'),
            onPressed: () => Navigator.pop(dialogContext, true),
            style: TextButton.styleFrom(foregroundColor: AppColors.danger),
            child: const Text(AppStrings.staffRemoveConfirm),
          ),
        ],
      ),
    );
    if (confirmed != true || !context.mounted) return;
    final failure = await cubit.removeService(service.id);
    if (!context.mounted) return;
    if (failure == null) {
      AppSnackbar.success(context, AppStrings.serviceRemoved);
    } else {
      AppSnackbar.error(context, failure.message);
    }
  }
}

/// Add/edit form for a service (spec: provider-service-crud).
class _ServiceFormSheet extends StatefulWidget {
  final ServicesCubit cubit;
  final ComposerService? initial;

  const _ServiceFormSheet({required this.cubit, this.initial});

  static Future<void> show(BuildContext context, ServicesCubit cubit,
      {ComposerService? initial}) {
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
        child: _ServiceFormSheet(cubit: cubit, initial: initial),
      ),
    );
  }

  @override
  State<_ServiceFormSheet> createState() => _ServiceFormSheetState();
}

class _ServiceFormSheetState extends State<_ServiceFormSheet> {
  late final _name = TextEditingController(text: widget.initial?.name ?? '');
  late final _duration = TextEditingController(
      text: widget.initial == null ? '' : '${widget.initial!.durationMinutes}');
  late final _price = TextEditingController(
      text: widget.initial == null
          ? ''
          : widget.initial!.price.toStringAsFixed(0));
  late final _description =
      TextEditingController(text: widget.initial?.description ?? '');
  bool _submitting = false;

  @override
  void dispose() {
    _name.dispose();
    _duration.dispose();
    _price.dispose();
    _description.dispose();
    super.dispose();
  }

  int get _minutes => int.tryParse(_duration.text.trim()) ?? 0;
  double get _priceValue => double.tryParse(_price.text.trim()) ?? 0;

  bool get _canSubmit =>
      _name.text.trim().isNotEmpty &&
      _minutes > 0 &&
      _priceValue > 0 &&
      !_submitting;

  Future<void> _submit() async {
    setState(() => _submitting = true);
    final cubit = widget.cubit;
    final failure = widget.initial == null
        ? await cubit.addService(
            name: _name.text.trim(),
            durationMinutes: _minutes,
            price: _priceValue,
            description: _description.text.trim(),
          )
        : await cubit.updateService(
            widget.initial!.id,
            name: _name.text.trim(),
            durationMinutes: _minutes,
            price: _priceValue,
            description: _description.text.trim(),
          );
    if (!mounted) return;
    if (failure == null) {
      Navigator.pop(context);
      AppSnackbar.success(
          context,
          widget.initial == null
              ? AppStrings.serviceAdded
              : AppStrings.serviceUpdated);
    } else {
      // Failure preserves the form's input (spec).
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
              widget.initial == null
                  ? AppStrings.serviceAdd
                  : AppStrings.serviceEdit,
              style: const TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w700,
                color: AppColors.ink,
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              key: const Key('service-name'),
              controller: _name,
              label: AppStrings.serviceName,
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: AppSpacing.sm),
            Row(
              children: [
                Expanded(
                  child: AppTextField(
                    key: const Key('service-duration'),
                    controller: _duration,
                    label: AppStrings.serviceDuration,
                    keyboardType: TextInputType.number,
                    onChanged: (_) => setState(() {}),
                  ),
                ),
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: AppTextField(
                    key: const Key('service-price'),
                    controller: _price,
                    label: AppStrings.servicePrice,
                    keyboardType: TextInputType.number,
                    onChanged: (_) => setState(() {}),
                  ),
                ),
              ],
            ),
            const SizedBox(height: AppSpacing.sm),
            AppTextField(
              key: const Key('service-description'),
              controller: _description,
              label: AppStrings.serviceDescription,
            ),
            const SizedBox(height: AppSpacing.md),
            AppButton(
              key: const Key('service-save'),
              label: AppStrings.serviceSave,
              loading: _submitting,
              onPressed: _canSubmit ? _submit : null,
            ),
            const SizedBox(height: AppSpacing.sm),
          ],
        ),
      ),
    );
  }
}

/// More → تیم (read-only).
/// More → سالن‌های من — the person's organization memberships (multi-salon, S6).
class MyMembershipsPage extends StatelessWidget {
  const MyMembershipsPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider<MembershipsCubit>(
      create: (_) => getIt<MembershipsCubit>()..load(),
      child: const MyMembershipsView(),
    );
  }
}

/// Separated from [MyMembershipsPage] so tests can pump it with a fake cubit.
class MyMembershipsView extends StatelessWidget {
  const MyMembershipsView({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<MembershipsCubit, MoreState<List<ProviderMembership>>>(
      builder: (context, state) =>
          _MoreSubScaffold<List<ProviderMembership>>(
        title: AppStrings.moreMemberships,
        state: state,
        onRetry: context.read<MembershipsCubit>().load,
        bodyBuilder: (context, memberships) => memberships.isEmpty
            ? const Center(
                child: Padding(
                  padding: EdgeInsets.all(AppSpacing.lg),
                  child: Text(
                    AppStrings.membershipsEmpty,
                    style: TextStyle(color: AppColors.muted),
                    textAlign: TextAlign.center,
                  ),
                ),
              )
            : ListView.separated(
                padding: const EdgeInsets.all(AppSpacing.md),
                itemCount: memberships.length,
                separatorBuilder: (_, _) =>
                    const Divider(color: AppColors.divider, height: 1),
                itemBuilder: (context, i) {
                  final m = memberships[i];
                  // Only an owner/manager membership can become the active
                  // workspace: the management screens are owner-authorized
                  // server-side, so switching into a staff-only membership
                  // would fail. A dedicated staff workspace is a separate step.
                  final canSwitch = m.isOwner && m.isActive;
                  return ListTile(
                    key: Key('membership-row-${m.membershipId}'),
                    contentPadding: EdgeInsets.zero,
                    enabled: canSwitch,
                    onTap: canSwitch ? () => _switchTo(context, m) : null,
                    leading: CircleAvatar(
                      backgroundColor: AppColors.primarySoft,
                      child: Text(
                        m.organizationName.isNotEmpty
                            ? m.organizationName.characters.first
                            : '؟',
                        style: const TextStyle(
                          color: AppColors.primary,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                    title: Text(
                      m.organizationName,
                      style: TextStyle(
                        fontSize: 15,
                        color: m.isActive ? AppColors.ink : AppColors.muted,
                      ),
                    ),
                    subtitle: Text(
                      [
                        if (m.isOwner) AppStrings.membershipOwner,
                        if (m.providesServices)
                          AppStrings.membershipProvidesServices,
                        if (!m.isActive) m.status,
                      ].join(' · '),
                      style:
                          const TextStyle(fontSize: 12, color: AppColors.muted),
                    ),
                    trailing: canSwitch
                        ? const Icon(Icons.swap_horiz,
                            size: AppIconSize.action, color: AppColors.primary)
                        : null,
                  );
                },
              ),
      ),
    );
  }

  /// Re-scopes the session to [m]'s organization, then returns to the dashboard
  /// so every provider-scoped screen reloads against the newly active salon.
  Future<void> _switchTo(BuildContext context, ProviderMembership m) async {
    final cubit = context.read<MembershipsCubit>();
    final failure = await cubit.switchTo(m.organizationId);
    if (!context.mounted) return;
    if (failure == null) {
      AppSnackbar.success(context, AppStrings.membershipSwitched(m.organizationName));
      context.go(Routes.dashboard);
    } else {
      AppSnackbar.error(context, failure.message);
    }
  }
}

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
/// Lists the organization's members from the membership model; "add" is invite
/// by phone, "remove" terminates the membership. Owners can't be removed here.
class StaffView extends StatelessWidget {
  const StaffView({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<StaffCubit, MoreState<List<OrgMember>>>(
      builder: (context, state) => _MoreSubScaffold<List<OrgMember>>(
        title: AppStrings.moreStaff,
        state: state,
        onRetry: context.read<StaffCubit>().load,
        actions: [
          IconButton(
            key: const Key('staff-invite'),
            tooltip: AppStrings.staffInvite,
            // Green add affordance on the blue chrome (ColiRide sub-page
            // pattern): "add" reads as the positive accent, not brand blue,
            // which would vanish against the blue header.
            //
            // This one was missed while its siblings (service-add, holiday-add)
            // got the accent: AppColors.primary is 0xFF3777BF against an
            // 0xFF3777C0 header — one step apart in the blue channel — so the
            // only way to invite a team member was an invisible icon, and the
            // feature read as unbuilt.
            icon: const Icon(Icons.person_add_alt_1, color: AppColors.success),
            onPressed: () =>
                InviteStaffSheet.show(context, context.read<StaffCubit>()),
          ),
        ],
        bodyBuilder: (context, members) => members.isEmpty
            ? AppEmptyState.add(
                icon: Icons.people_outline,
                message: AppStrings.staffEmpty,
                actionLabel: '+ ${AppStrings.staffInvite}',
                onAction: () =>
                    InviteStaffSheet.show(context, context.read<StaffCubit>()),
              )
            : ListView.separated(
                padding: const EdgeInsets.all(AppSpacing.md),
                // +1 for the leading add row, as Services and Holidays do.
                itemCount: members.length + 1,
                separatorBuilder: (_, _) =>
                    const Divider(color: AppColors.divider, height: 1),
                itemBuilder: (context, i) {
                  // The discoverable path to inviting someone. The chrome icon
                  // stays for muscle memory, but the spec ruling after visual
                  // QA was that the tiny header icon is easy to miss on first
                  // use — which is exactly how this screen read while the icon
                  // was also painted brand-blue on the blue header.
                  if (i == 0) {
                    return _AddLinkRow(
                      key: const Key('staff-invite-row'),
                      label: AppStrings.staffInvite,
                      onTap: () => InviteStaffSheet.show(
                          context, context.read<StaffCubit>()),
                    );
                  }
                  final m = members[i - 1];
                  final display = m.name.isNotEmpty ? m.name : (m.phone ?? '؟');
                  return ListTile(
                    key: Key('member-row-${m.membershipId}'),
                    contentPadding: EdgeInsets.zero,
                    leading: CircleAvatar(
                      backgroundColor: AppColors.primarySoft,
                      child: Text(
                        display.isNotEmpty ? display.characters.first : '؟',
                        style: const TextStyle(
                          color: AppColors.primary,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                    title: Text(
                      display,
                      style: TextStyle(
                        fontSize: 15,
                        color: m.isActive ? AppColors.ink : AppColors.muted,
                      ),
                    ),
                    subtitle: Text(
                      _subtitle(m),
                      style: const TextStyle(fontSize: 12, color: AppColors.muted),
                    ),
                    trailing: m.isOwner
                        ? const _OwnerBadge()
                        : IconButton(
                            key: Key('member-remove-${m.membershipId}'),
                            tooltip: AppStrings.staffRemove,
                            icon: const Icon(Icons.person_remove_outlined,
                                size: AppIconSize.action, color: AppColors.danger),
                            onPressed: () => _confirmRemove(context, m),
                          ),
                  );
                },
              ),
      ),
    );
  }

  static String _subtitle(OrgMember m) {
    final parts = <String>[
      if (m.isOwner) AppStrings.membershipOwner,
      if (m.providesServices) AppStrings.membershipProvidesServices,
      if (!m.isActive)
        (m.status == 'Invited'
            ? AppStrings.staffInvitePending
            : AppStrings.staffInactive),
    ];
    return parts.join(' · ');
  }

  Future<void> _confirmRemove(BuildContext context, OrgMember m) async {
    final cubit = context.read<StaffCubit>();
    final label = m.name.isNotEmpty ? m.name : (m.phone ?? '');
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text(AppStrings.staffRemoveConfirmTitle),
        content: Text(AppStrings.staffRemoveConfirmBody(label)),
        actions: [
          TextButton(
            key: const Key('member-remove-cancel'),
            onPressed: () => Navigator.pop(dialogContext, false),
            child: const Text(AppStrings.cancel),
          ),
          TextButton(
            key: const Key('member-remove-confirm'),
            onPressed: () => Navigator.pop(dialogContext, true),
            style: TextButton.styleFrom(foregroundColor: AppColors.danger),
            child: const Text(AppStrings.staffRemoveConfirm),
          ),
        ],
      ),
    );
    if (confirmed != true || !context.mounted) return;

    final failure = await cubit.removeMember(m.membershipId);
    if (!context.mounted) return;
    if (failure == null) {
      AppSnackbar.success(context, AppStrings.staffRemoved);
    } else {
      AppSnackbar.error(context, failure.message);
    }
  }
}

class _OwnerBadge extends StatelessWidget {
  const _OwnerBadge();

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(
        color: AppColors.primarySoft,
        borderRadius: BorderRadius.circular(8),
      ),
      child: const Text(
        AppStrings.membershipOwner,
        style: TextStyle(
            fontSize: 11, color: AppColors.primary, fontWeight: FontWeight.w600),
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

  /// Two sequential pickers (start, then end) append a break to [day]
  /// (spec: provider-break-editing); end must follow start.
  Future<void> _addBreak(
      BuildContext context, BusinessHoursCubit cubit, DayHours day) async {
    final start = await showTimePicker(
      context: context,
      initialTime: const TimeOfDay(hour: 13, minute: 0),
    );
    if (start == null || !context.mounted) return;
    final end = await showTimePicker(
      context: context,
      initialTime: TimeOfDay(hour: (start.hour + 1) % 24, minute: start.minute),
    );
    if (end == null || !context.mounted) return;
    final startMinutes = start.hour * 60 + start.minute;
    final endMinutes = end.hour * 60 + end.minute;
    if (endMinutes <= startMinutes) {
      AppSnackbar.error(context, AppStrings.hoursBreakInvalid);
      return;
    }
    cubit.addBreak(
      day.dayOfWeek,
      BreakTime(
        ClockTime(start.hour, start.minute),
        ClockTime(end.hour, end.minute),
      ),
    );
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
          if (day.isOpen)
            Align(
              alignment: AlignmentDirectional.centerStart,
              child: Padding(
                padding: const EdgeInsets.only(bottom: AppSpacing.xs),
                child: Wrap(
                  spacing: AppSpacing.xs,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    for (var i = 0; i < day.breaks.length; i++)
                      Chip(
                        key: Key(
                            'hours-break-${day.dayOfWeek}-${day.breaks[i].start.label}'),
                        label: Text(
                          '${AppStrings.hoursBreak} ${day.breaks[i].start.label}–${day.breaks[i].end.label}',
                          style: const TextStyle(fontSize: 11),
                        ),
                        onDeleted: () =>
                            cubit.removeBreak(day.dayOfWeek, i),
                        deleteIconColor: AppColors.muted,
                        deleteButtonTooltipMessage: AppStrings.cancel,
                        backgroundColor: AppColors.surfaceSoft,
                        side: const BorderSide(color: AppColors.border),
                        visualDensity: VisualDensity.compact,
                      ),
                    ActionChip(
                      key: Key('hours-add-break-${day.dayOfWeek}'),
                      avatar: const Icon(Icons.add,
                          size: AppIconSize.sm, color: AppColors.primary),
                      label: Text(
                        AppStrings.hoursAddBreak,
                        style: const TextStyle(
                            fontSize: 11, color: AppColors.primary),
                      ),
                      onPressed: () => _addBreak(context, cubit, day),
                      backgroundColor: Colors.white,
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
    return MultiBlocProvider(
      providers: [
        BlocProvider<HolidaysCubit>(
          create: (_) => getIt<HolidaysCubit>()..load(),
        ),
        BlocProvider<ExceptionsCubit>(
          create: (_) => getIt<ExceptionsCubit>()..load(),
        ),
      ],
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
            // Green add affordance on the blue chrome (ColiRide sub-page
            // pattern): "add" reads as the positive accent, not brand blue,
            // which would vanish against the blue header.
            icon: const Icon(Icons.add_circle, color: AppColors.success),
            onPressed: () =>
                _HolidayFormSheet.show(context, context.read<HolidaysCubit>()),
          ),
        ],
        bodyBuilder: (context, holidays) => ListView(
          padding: const EdgeInsets.all(AppSpacing.md),
          children: [
            if (holidays.isEmpty)
              SizedBox(
                height: 220,
                child: AppEmptyState.add(
                  icon: Icons.beach_access_outlined,
                  message: AppStrings.holidaysEmpty,
                  actionLabel: '+ ${AppStrings.holidayAdd}',
                  onAction: () => _HolidayFormSheet.show(
                      context, context.read<HolidaysCubit>()),
                ),
              )
            else ...[
              _AddLinkRow(
                key: const Key('holiday-add-row'),
                label: AppStrings.holidayAdd,
                onTap: () => _HolidayFormSheet.show(
                    context, context.read<HolidaysCubit>()),
              ),
              for (final h in holidays) ...[
                _holidayTile(context, h),
                const Divider(color: AppColors.divider, height: 1),
              ],
            ],
            const SizedBox(height: AppSpacing.lg),
            const _ExceptionsSection(),
          ],
        ),
      ),
    );
  }

  Widget _holidayTile(BuildContext context, ProviderHoliday h) => ListTile(
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

/// «ساعات استثنائی» — per-date availability exceptions beneath the days-off
/// list (spec: provider-block-time).
class _ExceptionsSection extends StatelessWidget {
  const _ExceptionsSection();

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<ExceptionsCubit,
        MoreState<List<AvailabilityException>>>(
      builder: (context, state) {
        final cubit = context.read<ExceptionsCubit>();
        final exceptions = state.data ?? const [];
        if (state.status == MoreStatus.ready && exceptions.isEmpty) {
          return const SizedBox.shrink();
        }
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              AppStrings.exceptionsSection,
              style: TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w700,
                color: AppColors.muted,
              ),
            ),
            const SizedBox(height: AppSpacing.xs),
            switch (state.status) {
              MoreStatus.loading => const Padding(
                  padding: EdgeInsets.all(AppSpacing.sm),
                  child: Center(
                      child: CircularProgressIndicator(strokeWidth: 2)),
                ),
              MoreStatus.failed => Row(
                  children: [
                    Expanded(
                      child: Text(
                        state.error ?? AppStrings.homeLoadError,
                        style: const TextStyle(
                            fontSize: 13, color: AppColors.muted),
                      ),
                    ),
                    TextButton(
                      key: const Key('exceptions-retry'),
                      onPressed: cubit.load,
                      child: const Text(AppStrings.retry),
                    ),
                  ],
                ),
              MoreStatus.ready => Column(
                  children: [
                    for (final e in exceptions)
                      ListTile(
                        key: Key('exception-row-${e.id}'),
                        contentPadding: EdgeInsets.zero,
                        leading: const Icon(Icons.block_outlined,
                            color: AppColors.primary),
                        title: Text(
                          e.reason,
                          style: const TextStyle(
                              fontSize: 15, color: AppColors.ink),
                        ),
                        subtitle: Text(
                          [
                            '${e.date.day}/${e.date.month}/${e.date.year}',
                            if (e.isClosed)
                              AppStrings.exceptionClosedAllDay
                            else
                              '${e.openTime ?? ''}–${e.closeTime ?? ''}',
                          ].join(' · '),
                          style: const TextStyle(
                              fontSize: 12, color: AppColors.muted),
                        ),
                        trailing: IconButton(
                          key: Key('exception-remove-${e.id}'),
                          icon: const Icon(Icons.delete_outline,
                              size: AppIconSize.action,
                              color: AppColors.danger),
                          onPressed: () => _confirmRemove(context, cubit, e),
                        ),
                      ),
                  ],
                ),
            },
          ],
        );
      },
    );
  }

  Future<void> _confirmRemove(
    BuildContext context,
    ExceptionsCubit cubit,
    AvailabilityException exception,
  ) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text(AppStrings.exceptionRemoveConfirmTitle),
        content: Text(AppStrings.exceptionRemoveConfirmBody(
            '${exception.date.day}/${exception.date.month}')),
        actions: [
          TextButton(
            key: const Key('exception-remove-cancel'),
            onPressed: () => Navigator.pop(dialogContext, false),
            child: const Text(AppStrings.cancel),
          ),
          TextButton(
            key: const Key('exception-remove-confirm'),
            onPressed: () => Navigator.pop(dialogContext, true),
            style: TextButton.styleFrom(foregroundColor: AppColors.danger),
            child: const Text(AppStrings.staffRemoveConfirm),
          ),
        ],
      ),
    );
    if (confirmed != true || !context.mounted) return;
    final failure = await cubit.removeException(exception.id);
    if (!context.mounted) return;
    if (failure == null) {
      AppSnackbar.success(context, AppStrings.exceptionRemoved);
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

/// Invite a person by phone to join the team (spec: organization-membership).
/// The backend reuses an existing account by phone (never a duplicate) and
/// rejects inviting yourself or an existing member.
class InviteStaffSheet extends StatefulWidget {
  final StaffCubit cubit;
  const InviteStaffSheet({super.key, required this.cubit});

  static Future<void> show(BuildContext context, StaffCubit cubit) {
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
        child: InviteStaffSheet(cubit: cubit),
      ),
    );
  }

  @override
  State<InviteStaffSheet> createState() => _InviteStaffSheetState();
}

class _InviteStaffSheetState extends State<InviteStaffSheet> {
  final _phone = TextEditingController();
  final _name = TextEditingController();
  bool _submitting = false;

  // Iranian mobile, same rule as the login screen.
  bool get _phoneValid => RegExp(r'^09\d{9}$').hasMatch(_phone.text.trim());

  @override
  void dispose() {
    _phone.dispose();
    _name.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() => _submitting = true);
    final failure = await widget.cubit.inviteStaff(
      phoneNumber: _phone.text.trim(),
      inviteeName: _name.text.trim().isEmpty ? null : _name.text.trim(),
    );
    if (!mounted) return;
    if (failure == null) {
      Navigator.pop(context);
      AppSnackbar.success(context, AppStrings.staffInviteSent);
    } else {
      // Preserve the entered values for retry (e.g. self-invite → fix the number).
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
              AppStrings.staffInvite,
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w700,
                color: AppColors.ink,
              ),
            ),
            const SizedBox(height: 4),
            const Text(
              AppStrings.staffInviteHint,
              style: TextStyle(fontSize: 12, color: AppColors.muted),
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              key: const Key('invite-phone'),
              controller: _phone,
              label: AppStrings.staffPhone,
              keyboardType: TextInputType.phone,
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: AppSpacing.sm),
            AppTextField(
              key: const Key('invite-name'),
              controller: _name,
              label: AppStrings.staffInviteNameOptional,
            ),
            const SizedBox(height: AppSpacing.md),
            AppButton(
              key: const Key('invite-send'),
              label: AppStrings.staffInviteSend,
              loading: _submitting,
              onPressed: _phoneValid ? _submit : null,
            ),
          ],
        ),
      ),
    );
  }
}
