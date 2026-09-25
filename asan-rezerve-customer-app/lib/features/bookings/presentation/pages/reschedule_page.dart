import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/utils/jalali_formatter.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../booking/domain/business_days.dart';
import '../../../booking/presentation/widgets/slot_picker.dart';
import '../../domain/entities/booking_summary.dart';
import '../bloc/reschedule_cubit.dart';

/// Reschedule screen: the shared slot picker scoped to the booking's
/// provider/service/staff. Pops with the new start time on success so the
/// appointments list can update the card in place.
class ReschedulePage extends StatelessWidget {
  final BookingSummary booking;

  /// The clock the day strip starts from; the device clock unless a test fixes it.
  final DateTime Function()? now;

  const ReschedulePage({super.key, required this.booking, this.now});

  /// Height the header and slot picker need at 1x text; scaled with the
  /// text before it is compared with the screen.
  static const double _minBodyHeight = 460;

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => RescheduleCubit(
        bookingRepository: getIt(),
        bookingsRepository: getIt(),
        booking: booking,
        now: now,
      ),
      child: BlocConsumer<RescheduleCubit, RescheduleState>(
        listenWhen: (prev, next) => prev.status != next.status,
        listener: (context, state) {
          if (state.status == RescheduleStatus.success) {
            AppSnackbar.success(context, AppStrings.rescheduleSuccess);
            // Pushed with Navigator.push, on the root navigator, by both callers (list and detail).
            Navigator.of(context).pop(state.selectedSlot!.startTime);
          } else if (state.status == RescheduleStatus.failure) {
            AppSnackbar.error(
              context,
              state.errorMessage ?? AppStrings.genericError,
            );
          }
        },
        builder: (context, state) {
          final cubit = context.read<RescheduleCubit>();
          return Scaffold(
            appBar: AppBar(
              title: const Text(AppStrings.rescheduleBooking),
            ),
            bottomNavigationBar: SafeArea(
              child: Padding(
                padding: const EdgeInsets.all(AppSpacing.md),
                child: AppButton(
                  key: const Key('reschedule-submit'),
                  label: AppStrings.rescheduleBooking,
                  loading: state.status == RescheduleStatus.submitting,
                  onPressed: state.selectedSlot != null ? cubit.submit : null,
                ),
              ),
            ),
            // On a short screen with large text the header, day strip and an
            // empty day's reason do not fit: the page scrolls instead of the
            // slot area being squeezed below what it needs.
            body: LayoutBuilder(
              builder: (context, constraints) => SingleChildScrollView(
                child: SizedBox(
                  height: math.max(
                    constraints.maxHeight,
                    MediaQuery.textScalerOf(context).scale(_minBodyHeight),
                  ),
                  child: Column(
                    children: [
                      Padding(
                        padding: const EdgeInsets.fromLTRB(
                          AppSpacing.md,
                          AppSpacing.md,
                          AppSpacing.md,
                          0,
                        ),
                        child: AppCard(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                booking.serviceName,
                                style: Theme.of(context).textTheme.titleSmall,
                              ),
                              const SizedBox(height: AppSpacing.xxs),
                              Text(
                                booking.providerName,
                                style: Theme.of(context).textTheme.bodyMedium,
                              ),
                              const SizedBox(height: AppSpacing.xxs),
                              Text(
                                AppStrings.rescheduleCurrentTime(
                                  JalaliFormatter.formatDateTime(
                                      booking.startTime),
                                ),
                                style: Theme.of(context).textTheme.bodyMedium,
                              ),
                            ],
                          ),
                        ),
                      ),
                      Expanded(
                        child: SlotPicker(
                          // The cubit's day, not the device clock's: the strip and the times asked for agree.
                          today: cubit.today,
                          selectedDate: state.selectedDate,
                          onDateSelected: cubit.loadSlots,
                          status: switch (state.status) {
                            RescheduleStatus.loadingSlots =>
                              SlotPickerStatus.loading,
                            RescheduleStatus.slotsError =>
                              SlotPickerStatus.error,
                            _ => SlotPickerStatus.loaded,
                          },
                          slots: state.slots,
                          selectedSlot: state.selectedSlot,
                          onSlotSelected: cubit.selectSlot,
                          onRetry: () => cubit.loadSlots(state.selectedDate),
                          // The booking flow's window: today plus the salon's days.
                          daysToShow: state.maxAdvanceBookingDays + 1,
                          // As in the booking flow: the salon's closed weekdays cannot be picked.
                          closedWeekdays: BusinessDays.closedWeekdays(state.businessHours),
                          emptyReason: state.slotsReason,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          );
        },
      ),
    );
  }
}
