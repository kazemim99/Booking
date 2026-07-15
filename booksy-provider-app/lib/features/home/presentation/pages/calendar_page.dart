import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/widgets/app_empty_state.dart';
import '../../../../core/widgets/app_error_state.dart';
import '../../../../core/widgets/app_snackbar.dart';
import '../../domain/entities/home_booking.dart';
import '../cubit/calendar_cubit.dart';
import '../widgets/provider_nav_bar.dart';

/// The Calendar tab (spec: provider-calendar): RTL week strip + selected-day
/// timeline, booking action sheet, and calendar-initiated creation.
class CalendarPage extends StatelessWidget {
  const CalendarPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider<CalendarCubit>(
      create: (_) => getIt<CalendarCubit>()..load(),
      child: const CalendarView(),
    );
  }
}

/// Separated from [CalendarPage] so tests can pump it with a fake cubit.
class CalendarView extends StatelessWidget {
  const CalendarView({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<CalendarCubit, CalendarState>(
      builder: (context, state) {
        final cubit = context.read<CalendarCubit>();
        return Scaffold(
          backgroundColor: Colors.white,
          appBar: AppBar(
            backgroundColor: Colors.white,
            surfaceTintColor: Colors.transparent,
            elevation: 0,
            automaticallyImplyLeading: false,
            title: const Text(
              AppStrings.calendarTitle,
              style: TextStyle(
                fontSize: 17,
                fontWeight: FontWeight.w700,
                color: AppColors.ink,
              ),
            ),
            actions: [
              TextButton(
                key: const Key('calendar-today'),
                onPressed: cubit.jumpToToday,
                child: const Text(AppStrings.calendarToday),
              ),
            ],
          ),
          body: switch (state.status) {
            CalendarStatus.loading => const Center(
                child: CircularProgressIndicator(),
              ),
            CalendarStatus.failed => AppErrorState(
                message: state.error ?? AppStrings.homeLoadError,
                onRetry: cubit.load,
              ),
            CalendarStatus.ready => Column(
                children: [
                  _WeekHeader(state: state, cubit: cubit),
                  _WeekStrip(state: state, cubit: cubit),
                  if (state.stale)
                    Padding(
                      padding: const EdgeInsets.symmetric(
                          vertical: AppSpacing.xs),
                      child: Text(
                        AppStrings.homeStaleBanner,
                        key: const Key('calendar-stale'),
                        style: const TextStyle(
                            fontSize: 12, color: AppColors.muted),
                      ),
                    ),
                  const Divider(color: AppColors.divider, height: 1),
                  Expanded(child: _DayTimeline(state: state, cubit: cubit)),
                ],
              ),
          },
          floatingActionButton: FloatingActionButton(
            key: const Key('calendar-create-action'),
            tooltip: AppStrings.homeCreateTitle,
            onPressed: () => _openComposer(context, state.selectedDay),
            child: const Icon(Icons.add),
          ),
          floatingActionButtonLocation:
              FloatingActionButtonLocation.centerDocked,
          bottomNavigationBar: const ProviderNavBar(active: NavTab.calendar),
        );
      },
    );
  }

  /// Opens the composer pre-set to [day]; refreshes on created-and-returned.
  static Future<void> _openComposer(BuildContext context, DateTime day) async {
    final cubit = context.read<CalendarCubit>();
    final created = await context.push<bool>(Routes.newBookingOn(day));
    if (created == true && context.mounted) {
      cubit.refresh();
      AppSnackbar.success(context, AppStrings.composerCreated);
    }
  }
}

class _WeekHeader extends StatelessWidget {
  final CalendarState state;
  final CalendarCubit cubit;

  const _WeekHeader({required this.state, required this.cubit});

  String _dm(DateTime d) => '${d.day}/${d.month}';

  @override
  Widget build(BuildContext context) {
    final weekEnd = state.weekStart.add(const Duration(days: 6));
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.sm),
      child: Row(
        children: [
          IconButton(
            key: const Key('calendar-prev-week'),
            tooltip: AppStrings.calendarPrevWeek,
            icon: const Icon(Icons.chevron_right,
                size: AppIconSize.md, color: AppColors.muted),
            onPressed: cubit.previousWeek,
          ),
          Expanded(
            child: Text(
              AppStrings.calendarWeekOf(_dm(state.weekStart), _dm(weekEnd)),
              textAlign: TextAlign.center,
              style: const TextStyle(fontSize: 13, color: AppColors.muted),
            ),
          ),
          IconButton(
            key: const Key('calendar-next-week'),
            tooltip: AppStrings.calendarNextWeek,
            icon: const Icon(Icons.chevron_left,
                size: AppIconSize.md, color: AppColors.muted),
            onPressed: cubit.nextWeek,
          ),
        ],
      ),
    );
  }
}

class _WeekStrip extends StatelessWidget {
  final CalendarState state;
  final CalendarCubit cubit;

  const _WeekStrip({required this.state, required this.cubit});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: AppSpacing.xs,
      ),
      child: Row(
        children: [
          for (var i = 0; i < 7; i++)
            _dayCell(state.weekStart.add(Duration(days: i))),
        ],
      ),
    );
  }

  Widget _dayCell(DateTime day) {
    final selected = day == state.selectedDay;
    final count = state.countFor(day);
    return Expanded(
      child: InkWell(
        key: Key('calendar-day-${day.day}-${day.month}'),
        onTap: () => cubit.selectDay(day),
        borderRadius: BorderRadius.circular(AppRadius.md),
        child: Container(
          constraints: const BoxConstraints(minHeight: 62),
          margin: const EdgeInsets.symmetric(horizontal: 2),
          padding: const EdgeInsets.symmetric(vertical: AppSpacing.xs),
          decoration: BoxDecoration(
            color: selected ? AppColors.primarySoft : null,
            borderRadius: BorderRadius.circular(AppRadius.md),
            border: Border.all(
              color: selected ? AppColors.primary : AppColors.border,
            ),
          ),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text(
                AppStrings.weekDays[day.weekday % 7],
                style: TextStyle(
                  fontSize: 11,
                  color: selected ? AppColors.primary : AppColors.muted,
                ),
              ),
              Text(
                '${day.day}',
                style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w700,
                  color: selected ? AppColors.primary : AppColors.ink,
                ),
              ),
              SizedBox(
                height: 14,
                child: count > 0
                    ? Container(
                        padding:
                            const EdgeInsets.symmetric(horizontal: 5),
                        decoration: BoxDecoration(
                          color: selected
                              ? AppColors.primary
                              : AppColors.icon,
                          borderRadius: BorderRadius.circular(AppRadius.lg),
                        ),
                        child: Text(
                          '$count',
                          style: const TextStyle(
                              fontSize: 10, color: Colors.white),
                        ),
                      )
                    : null,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _DayTimeline extends StatelessWidget {
  final CalendarState state;
  final CalendarCubit cubit;

  const _DayTimeline({required this.state, required this.cubit});

  static String _time(DateTime? s) => s == null
      ? '—'
      : '${s.hour.toString().padLeft(2, '0')}:${s.minute.toString().padLeft(2, '0')}';

  @override
  Widget build(BuildContext context) {
    final bookings = state.selectedDayBookings;
    if (bookings.isEmpty) {
      return AppEmptyState(
        icon: Icons.event_available_outlined,
        message: AppStrings.calendarEmptyDay,
        actionLabel: '+ ${AppStrings.homeAddAppointment}',
        onAction: () =>
            CalendarView._openComposer(context, state.selectedDay),
      );
    }

    return ListView.separated(
      key: const Key('calendar-timeline'),
      padding: const EdgeInsets.all(AppSpacing.md),
      itemCount: bookings.length,
      separatorBuilder: (_, _) => const SizedBox(height: AppSpacing.sm),
      itemBuilder: (context, i) {
        final b = bookings[i];
        return InkWell(
          key: Key('calendar-booking-${b.id}'),
          onTap: () => _showBookingSheet(context, b),
          borderRadius: BorderRadius.circular(AppRadius.card),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              SizedBox(
                width: 48,
                child: Text(
                  _time(b.start),
                  style: TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w600,
                    color: b.isDone ? AppColors.muted : AppColors.ink,
                  ),
                ),
              ),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: Container(
                  padding: const EdgeInsets.all(AppSpacing.card),
                  decoration: BoxDecoration(
                    color: b.status == HomeBookingStatus.pending
                        ? AppColors.primarySoft
                        : Colors.white,
                    borderRadius: BorderRadius.circular(AppRadius.card),
                    border: Border.all(color: AppColors.border),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        [b.clientName, b.serviceName]
                            .where((s) => s.isNotEmpty)
                            .join(' · '),
                        style: TextStyle(
                          fontSize: 14,
                          color:
                              b.isDone ? AppColors.muted : AppColors.ink,
                        ),
                      ),
                      const SizedBox(height: AppSpacing.xs),
                      Text(
                        _statusLabel(b.status),
                        style: TextStyle(
                          fontSize: 12,
                          color: _statusColor(b.status),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  static String _statusLabel(HomeBookingStatus s) => switch (s) {
        HomeBookingStatus.pending => AppStrings.homeStatusPending,
        HomeBookingStatus.completed => AppStrings.homeStatusDone,
        HomeBookingStatus.noShow => AppStrings.homeStatusNoShow,
        HomeBookingStatus.cancelled => AppStrings.homeDeclined,
        HomeBookingStatus.confirmed => AppStrings.homeConfirm,
      };

  static Color _statusColor(HomeBookingStatus s) => switch (s) {
        HomeBookingStatus.pending => AppColors.primary,
        HomeBookingStatus.completed => AppColors.success,
        HomeBookingStatus.noShow ||
        HomeBookingStatus.cancelled =>
          AppColors.muted,
        HomeBookingStatus.confirmed => AppColors.ink,
      };

  void _showBookingSheet(BuildContext context, HomeBooking booking) {
    final cubit = this.cubit;
    showModalBottomSheet<void>(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(
          top: Radius.circular(AppRadius.bottomSheet),
        ),
      ),
      builder: (sheetContext) {
        Future<void> run(
          Future<Failure?> action,
          String successMessage,
        ) async {
          Navigator.pop(sheetContext);
          final failure = await action;
          if (!context.mounted) return;
          if (failure == null) {
            AppSnackbar.success(context, successMessage);
          } else {
            AppSnackbar.error(context, failure.message);
          }
        }

        return SafeArea(
          child: Padding(
            padding: const EdgeInsets.all(AppSpacing.md),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    const Expanded(
                      child: Text(
                        AppStrings.bookingSheetTitle,
                        style: TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.w700,
                          color: AppColors.ink,
                        ),
                      ),
                    ),
                    Text(
                      _time(booking.start),
                      style: const TextStyle(
                        fontSize: 17,
                        fontWeight: FontWeight.w700,
                        color: AppColors.ink,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: AppSpacing.sm),
                Text(
                  [booking.clientName, booking.serviceName]
                      .where((s) => s.isNotEmpty)
                      .join(' · '),
                  style:
                      const TextStyle(fontSize: 15, color: AppColors.ink),
                ),
                const SizedBox(height: AppSpacing.xs),
                Text(
                  _statusLabel(booking.status),
                  style: TextStyle(
                    fontSize: 13,
                    color: _statusColor(booking.status),
                  ),
                ),
                const SizedBox(height: AppSpacing.md),
                // Status-appropriate actions; width-constrained (footgun).
                if (booking.status == HomeBookingStatus.pending)
                  Row(
                    children: [
                      Expanded(
                        child: OutlinedButton(
                          key: const Key('sheet-decline'),
                          style: OutlinedButton.styleFrom(
                            foregroundColor: AppColors.danger,
                            side:
                                const BorderSide(color: AppColors.danger),
                          ),
                          onPressed: () => run(
                            cubit.declineBooking(booking.id,
                                reason: AppStrings.homeDeclineReason),
                            AppStrings.homeDeclined,
                          ),
                          child: const Text(AppStrings.homeDecline),
                        ),
                      ),
                      const SizedBox(width: AppSpacing.sm),
                      Expanded(
                        child: FilledButton(
                          key: const Key('sheet-confirm'),
                          onPressed: () => run(
                            cubit.confirmBooking(booking.id),
                            AppStrings.homeConfirmed,
                          ),
                          child: const Text(AppStrings.homeConfirm),
                        ),
                      ),
                    ],
                  )
                else if (booking.status == HomeBookingStatus.confirmed)
                  Row(
                    children: [
                      Expanded(
                        child: FilledButton.icon(
                          key: const Key('sheet-complete'),
                          onPressed: () => run(
                            cubit.completeBooking(booking.id),
                            AppStrings.homeCompleted,
                          ),
                          icon: const Icon(Icons.check,
                              size: AppIconSize.action),
                          label: const Text(AppStrings.homeActionComplete),
                        ),
                      ),
                      const SizedBox(width: AppSpacing.sm),
                      Expanded(
                        child: OutlinedButton.icon(
                          key: const Key('sheet-noshow'),
                          onPressed: () => run(
                            cubit.markNoShow(booking.id),
                            AppStrings.homeNoShowMarked,
                          ),
                          icon: const Icon(Icons.person_off,
                              size: AppIconSize.action),
                          label: const Text(AppStrings.homeActionNoShow),
                        ),
                      ),
                    ],
                  ),
                const SizedBox(height: AppSpacing.sm),
              ],
            ),
          ),
        );
      },
    );
  }
}
