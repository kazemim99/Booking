import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/app_card.dart';
import '../../../../core/widgets/app_empty_state.dart';
import '../../../../core/widgets/app_error_state.dart';
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

  const _MoreSubScaffold({
    super.key,
    required this.title,
    required this.state,
    required this.onRetry,
    required this.bodyBuilder,
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
        bodyBuilder: (context, staff) => staff.isEmpty
            ? const AppEmptyState(
                icon: Icons.people_outline,
                message: AppStrings.staffEmpty,
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
                  );
                },
              ),
      ),
    );
  }
}
