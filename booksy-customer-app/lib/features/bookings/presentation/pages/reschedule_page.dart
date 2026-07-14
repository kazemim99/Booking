import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../booking/presentation/widgets/slot_picker.dart';
import '../../domain/entities/booking_summary.dart';
import '../bloc/reschedule_cubit.dart';

/// Reschedule screen: the shared slot picker scoped to the booking's
/// provider/service/staff. Pops with the new start time on success so the
/// appointments list can update the card in place.
class ReschedulePage extends StatelessWidget {
  final BookingSummary booking;

  const ReschedulePage({super.key, required this.booking});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => RescheduleCubit(
        bookingRepository: getIt(),
        bookingsRepository: getIt(),
        booking: booking,
      ),
      child: BlocConsumer<RescheduleCubit, RescheduleState>(
        listener: (context, state) {
          if (state.status == RescheduleStatus.success) {
            AppSnackbar.success(context, AppStrings.rescheduleSuccess);
            context.pop(state.selectedSlot!.startTime);
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
                  label: AppStrings.rescheduleBooking,
                  loading: state.status == RescheduleStatus.submitting,
                  onPressed:
                      state.selectedSlot != null ? cubit.submit : null,
                ),
              ),
            ),
            body: Column(
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
                      ],
                    ),
                  ),
                ),
                Expanded(
                  child: SlotPicker(
                    selectedDate: state.selectedDate,
                    onDateSelected: cubit.loadSlots,
                    status: switch (state.status) {
                      RescheduleStatus.loadingSlots =>
                        SlotPickerStatus.loading,
                      RescheduleStatus.slotsError => SlotPickerStatus.error,
                      _ => SlotPickerStatus.loaded,
                    },
                    slots: state.slots,
                    selectedSlot: state.selectedSlot,
                    onSlotSelected: cubit.selectSlot,
                    onRetry: () => cubit.loadSlots(state.selectedDate),
                  ),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}
