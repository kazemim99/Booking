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
import '../../../../core/widgets/app_page_scaffold.dart';
import '../../../../core/widgets/app_snackbar.dart';
import '../../domain/entities/home_booking.dart';
import '../cubit/calendar_cubit.dart';
import '../widgets/block_time_sheet.dart';
import '../widgets/booking_card.dart';
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
        // Week navigation belongs to the chrome, not the sheet: it stays put
        // on the blue while the day's timeline scrolls underneath it.
        final onChrome = state.status == CalendarStatus.ready;
        return AppPageScaffold(
          automaticallyImplyLeading: false,
          title: AppStrings.calendarTitle,
          actions: [
            TextButton(
              key: const Key('calendar-today'),
              onPressed: cubit.jumpToToday,
              style: TextButton.styleFrom(foregroundColor: Colors.white),
              child: const Text(AppStrings.calendarToday),
            ),
          ],
          chromeFooter: onChrome
              ? Column(
                  children: [
                    _WeekHeader(state: state, cubit: cubit),
                    _WeekStrip(state: state, cubit: cubit),
                    const SizedBox(height: AppSpacing.sm),
                  ],
                )
              : null,
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
                  Expanded(child: _DayTimeline(state: state, cubit: cubit)),
                ],
              ),
          },
          bottomNavigationBar: ProviderNavBar(
            active: NavTab.calendar,
            createKey: const Key('calendar-create-action'),
            onCreate: () => _showCreateSheet(context, state.selectedDay),
          ),
        );
      },
    );
  }

  /// The ⊕ menu, pre-dated to the calendar's selected [day]
  /// (spec: provider-block-time — calendar-initiated creation).
  static void _showCreateSheet(BuildContext context, DateTime day) {
    final cubit = context.read<CalendarCubit>();
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
          children: [
            ListTile(
              key: const Key('create-appointment'),
              leading:
                  const Icon(Icons.event_outlined, color: AppColors.primary),
              title: const Text(AppStrings.homeCreateAppointment),
              onTap: () {
                Navigator.pop(sheetContext);
                _openComposer(context, day);
              },
            ),
            ListTile(
              key: const Key('create-block-time'),
              leading:
                  const Icon(Icons.block_outlined, color: AppColors.primary),
              title: const Text(AppStrings.homeCreateBlockTime),
              onTap: () {
                Navigator.pop(sheetContext);
                BlockTimeSheet.show(
                  context,
                  initialDate: day,
                  onSubmit: cubit.blockTime,
                );
              },
            ),
            const SizedBox(height: AppSpacing.sm),
          ],
        ),
      ),
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
                size: AppIconSize.md, color: Colors.white),
            onPressed: cubit.previousWeek,
          ),
          Expanded(
            child: Text(
              AppStrings.calendarWeekOf(_dm(state.weekStart), _dm(weekEnd)),
              textAlign: TextAlign.center,
              style: const TextStyle(fontSize: 13, color: Colors.white70),
            ),
          ),
          IconButton(
            key: const Key('calendar-next-week'),
            tooltip: AppStrings.calendarNextWeek,
            icon: const Icon(Icons.chevron_left,
                size: AppIconSize.md, color: Colors.white),
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
          // On the blue chrome the selected day inverts to a white pill;
          // the rest stay translucent so the chrome reads as one surface.
          decoration: BoxDecoration(
            color: selected ? Colors.white : Colors.white24,
            borderRadius: BorderRadius.circular(AppRadius.md),
          ),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text(
                AppStrings.weekDays[day.weekday % 7],
                style: TextStyle(
                  fontSize: 11,
                  color: selected ? AppColors.primary : Colors.white70,
                ),
              ),
              Text(
                '${day.day}',
                style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w700,
                  color: selected ? AppColors.primary : Colors.white,
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
                              : Colors.white38,
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
      return AppEmptyState.add(
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
        return BookingCard(
          key: Key('calendar-booking-${b.id}'),
          booking: b,
          onTap: () => _showBookingSheet(context, b),
        );
      },
    );
  }

  static String _statusLabel(HomeBookingStatus s) => switch (s) {
        HomeBookingStatus.pending => AppStrings.homeStatusPending,
        HomeBookingStatus.completed => AppStrings.homeStatusDone,
        HomeBookingStatus.noShow => AppStrings.homeStatusNoShow,
        HomeBookingStatus.cancelled => AppStrings.homeStatusCancelled,
        HomeBookingStatus.confirmed => AppStrings.homeStatusConfirmed,
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
