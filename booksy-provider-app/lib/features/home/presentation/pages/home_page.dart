import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/api/config/api_constants.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/widgets/app_error_state.dart';
import '../../../../core/widgets/app_snackbar.dart';
import '../../../auth/presentation/bloc/auth_bloc.dart';
import '../../../auth/presentation/bloc/auth_event.dart';
import '../../../auth/presentation/bloc/auth_state.dart';
import '../../domain/composition/home_widget_registry.dart';
import '../../domain/entities/home_booking.dart';
import '../../domain/entities/home_context.dart';
import '../../domain/entities/home_enums.dart';
import '../cubit/home_cubit.dart';
import '../widgets/action_queue.dart';
import '../widgets/activation_checklist.dart';
import '../widgets/get_discovered.dart';
import '../widgets/home_minor_zones.dart';
import '../widgets/now_next.dart';
import '../widgets/provider_nav_bar.dart';
import '../widgets/status_banner_rail.dart';
import '../widgets/today_agenda.dart';

/// The Provider Home ("Today" workspace) — a thin orchestrator.
///
/// Renders `HomeWidgetRegistry.compose(ctx)` top-to-bottom and routes widget
/// intents (confirm/decline/complete/no-show/share) to the cubit; it owns no
/// zone business logic and no per-state layout (design ID6).
class HomePage extends StatelessWidget {
  const HomePage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider<HomeCubit>(
      create: (_) => getIt<HomeCubit>()..load(),
      child: const HomeView(),
    );
  }
}

/// Separated from [HomePage] so tests can pump it with a fake cubit.
class HomeView extends StatelessWidget {
  final HomeWidgetRegistry registry;

  const HomeView({super.key, this.registry = const HomeWidgetRegistry()});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<HomeCubit, HomeContext>(
      builder: (context, ctx) {
        return Scaffold(
          backgroundColor: Colors.white,
          appBar: _appBar(context),
          body: switch (ctx.system) {
            SystemState.loading => const HomeSkeleton(),
            SystemState.error => AppErrorState(
                message: AppStrings.homeLoadError,
                onRetry: () => context.read<HomeCubit>().refresh(),
              ),
            _ => RefreshIndicator(
                onRefresh: () => context.read<HomeCubit>().refresh(),
                child: ListView(
                  key: const Key('home-zone-list'),
                  padding: const EdgeInsets.all(AppSpacing.md),
                  children: [
                    for (final id in registry.compose(ctx))
                      Padding(
                        padding:
                            const EdgeInsets.only(bottom: AppSpacing.md),
                        child: _zone(context, ctx, id),
                      ),
                    // Clearance for the docked create action.
                    const SizedBox(height: AppSpacing.xl),
                  ],
                ),
              ),
          },
          floatingActionButton: ctx.system == SystemState.error
              ? null
              : FloatingActionButton(
                  key: const Key('home-create-action'),
                  tooltip: AppStrings.homeCreateTitle,
                  onPressed: () => _showCreateSheet(context),
                  child: const Icon(Icons.add),
                ),
          floatingActionButtonLocation:
              FloatingActionButtonLocation.centerDocked,
          bottomNavigationBar: const ProviderNavBar(active: NavTab.home),
        );
      },
    );
  }

  // ==================== chrome ====================

  PreferredSizeWidget _appBar(BuildContext context) {
    final authState = context.watch<AuthBloc>().state;
    final session = authState is Authenticated ? authState.session : null;
    final name = session?.user.displayName ?? '';

    final hour = DateTime.now().hour;
    final greeting = hour < 12
        ? AppStrings.homeGreetingMorning
        : hour < 17
            ? AppStrings.homeGreetingAfternoon
            : AppStrings.homeGreetingEvening;

    return AppBar(
      backgroundColor: Colors.white,
      surfaceTintColor: Colors.transparent,
      elevation: 0,
      titleSpacing: AppSpacing.md,
      automaticallyImplyLeading: false,
      title: Row(
        children: [
          InkWell(
            key: const Key('home-avatar'),
            onTap: () => _showAccountSheet(context),
            customBorder: const CircleBorder(),
            child: CircleAvatar(
              radius: 20,
              backgroundColor: AppColors.primarySoft,
              child: Text(
                name.isNotEmpty ? name.characters.first : '؟',
                style: const TextStyle(
                  fontSize: 16,
                  color: AppColors.primary,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ),
          const SizedBox(width: AppSpacing.sm),
          Expanded(
            child: Text(
              name.isEmpty ? greeting : '$greeting، $name',
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                fontSize: 17,
                fontWeight: FontWeight.w600,
                color: AppColors.ink,
              ),
            ),
          ),
        ],
      ),
      actions: [
        IconButton(
          key: const Key('home-bell'),
          tooltip: AppStrings.homeCreateTitle,
          icon: const Icon(Icons.notifications_none, color: AppColors.ink),
          onPressed: () => AppSnackbar.info(context, AppStrings.comingSoon),
        ),
      ],
    );
  }

  // ==================== zone mapping ====================

  Widget _zone(BuildContext context, HomeContext ctx, HomeWidgetId id) {
    final cubit = context.read<HomeCubit>();
    switch (id) {
      case HomeWidgetId.statusBannerRail:
        return StatusBannerRail(
          banners: ctx.banners,
          onSupportTap: () => AppSnackbar.info(context, AppStrings.comingSoon),
        );
      case HomeWidgetId.activationChecklist:
        return ActivationChecklist(
          onItemTap: (key) => key == 'share'
              ? _shareLink(context)
              : AppSnackbar.info(context, AppStrings.comingSoon),
        );
      case HomeWidgetId.getDiscovered:
        return GetDiscovered(
          completenessPct: ctx.completenessPct,
          onShare: () => _shareLink(context),
          onAddWalkIn: () => _openComposer(context),
        );
      case HomeWidgetId.nowNext:
        final next = _nextBooking(ctx);
        if (next == null) return const SizedBox.shrink();
        final start = next.start;
        return NowNext(
          booking: next,
          inProgress: start != null && start.isBefore(DateTime.now()),
          onComplete: (id) => _run(context, cubit.completeBooking(id),
              AppStrings.homeCompleted),
          onNoShow: (id) =>
              _run(context, cubit.markNoShow(id), AppStrings.homeNoShowMarked),
          onCall: (b) => _copy(context, b.clientPhone, b.clientPhone),
        );
      case HomeWidgetId.endOfDaySummary:
        return EndOfDaySummary(completedCount: ctx.todayApptCount);
      case HomeWidgetId.actionQueue:
        return ActionQueue(
          requests: ctx.todayBookings
              .where((b) => b.status == HomeBookingStatus.pending)
              .toList(),
          onConfirm: (id) => _run(
              context, cubit.confirmBooking(id), AppStrings.homeConfirmed),
          onDecline: (id) => _run(
            context,
            cubit.declineBooking(id, reason: AppStrings.homeDeclineReason),
            AppStrings.homeDeclined,
          ),
        );
      case HomeWidgetId.todayAgenda:
        return TodayAgenda(
          bookings: ctx.todayBookings,
          tomorrowApptCount: ctx.tomorrowApptCount,
          onAddAppointment: () => _openComposer(context),
          onComplete: (id) => _run(context, cubit.completeBooking(id),
              AppStrings.homeCompleted),
          onNoShow: (id) =>
              _run(context, cubit.markNoShow(id), AppStrings.homeNoShowMarked),
        );
      case HomeWidgetId.comingUpPeek:
        return ComingUpPeek(
          tomorrowApptCount: ctx.tomorrowApptCount,
          onTap: () => AppSnackbar.info(context, AppStrings.comingSoon),
        );
      case HomeWidgetId.businessAlerts:
      case HomeWidgetId.setupNudges:
        // No data sources yet (visibility rules keep them hidden).
        return const SizedBox.shrink();
    }
  }

  HomeBooking? _nextBooking(HomeContext ctx) {
    for (final b in ctx.todayBookings) {
      if (!b.isDone && b.status != HomeBookingStatus.cancelled) return b;
    }
    return null;
  }

  // ==================== intent handlers ====================

  Future<void> _run(
    BuildContext context,
    Future<Failure?> action,
    String successMessage,
  ) async {
    final failure = await action;
    if (!context.mounted) return;
    if (failure == null) {
      AppSnackbar.success(context, successMessage);
    } else {
      AppSnackbar.error(context, failure.message);
    }
  }

  void _shareLink(BuildContext context) {
    final authState = context.read<AuthBloc>().state;
    final providerId =
        authState is Authenticated ? authState.session.providerId : null;
    if (providerId == null) {
      AppSnackbar.error(context, AppStrings.genericError);
      return;
    }
    _copy(context, ApiConstants.publicProviderUrl(providerId),
        AppStrings.linkCopied);
  }

  /// Opens the booking composer; a `true` result means a booking was created,
  /// so the Home refreshes and confirms.
  Future<void> _openComposer(BuildContext context) async {
    final cubit = context.read<HomeCubit>();
    final created = await context.push<bool>(Routes.newBooking);
    if (created == true && context.mounted) {
      cubit.refresh();
      AppSnackbar.success(context, AppStrings.composerCreated);
    }
  }

  void _copy(BuildContext context, String text, String message) {
    Clipboard.setData(ClipboardData(text: text));
    AppSnackbar.info(context, message);
  }

  void _showCreateSheet(BuildContext context) {
    showModalBottomSheet<void>(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(
          top: Radius.circular(AppRadius.bottomSheet),
        ),
      ),
      builder: (sheetContext) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Padding(
              padding: EdgeInsets.all(AppSpacing.md),
              child: Text(
                AppStrings.homeCreateTitle,
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                  color: AppColors.ink,
                ),
              ),
            ),
            ListTile(
              key: const Key('create-appointment'),
              leading: const Icon(Icons.event_outlined,
                  color: AppColors.primary),
              title: const Text(AppStrings.homeCreateAppointment),
              onTap: () {
                Navigator.pop(sheetContext);
                _openComposer(context);
              },
            ),
            ListTile(
              key: const Key('create-block-time'),
              leading: const Icon(Icons.block_outlined,
                  color: AppColors.primary),
              title: const Text(AppStrings.homeCreateBlockTime),
              onTap: () {
                Navigator.pop(sheetContext);
                AppSnackbar.info(context, AppStrings.comingSoon);
              },
            ),
            const SizedBox(height: AppSpacing.sm),
          ],
        ),
      ),
    );
  }

  void _showAccountSheet(BuildContext context) {
    final authState = context.read<AuthBloc>().state;
    final session = authState is Authenticated ? authState.session : null;

    showModalBottomSheet<void>(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(
          top: Radius.circular(AppRadius.bottomSheet),
        ),
      ),
      builder: (sheetContext) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Padding(
              padding: const EdgeInsets.all(AppSpacing.md),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    session?.user.displayName ?? AppStrings.homeAccountTitle,
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w700,
                      color: AppColors.ink,
                    ),
                  ),
                  if (session?.providerStatus != null)
                    Text(
                      AppStrings.providerStatusLabel(
                          session!.providerStatus!.wireName),
                      style: const TextStyle(
                          fontSize: 13, color: AppColors.muted),
                    ),
                ],
              ),
            ),
            const Divider(color: AppColors.divider, height: 1),
            ListTile(
              key: const Key('account-logout'),
              leading: const Icon(Icons.logout, color: AppColors.danger),
              title: const Text(
                AppStrings.logout,
                style: TextStyle(color: AppColors.danger),
              ),
              onTap: () {
                Navigator.pop(sheetContext);
                context.read<AuthBloc>().add(const LogoutRequested());
              },
            ),
            const SizedBox(height: AppSpacing.sm),
          ],
        ),
      ),
    );
  }
}
