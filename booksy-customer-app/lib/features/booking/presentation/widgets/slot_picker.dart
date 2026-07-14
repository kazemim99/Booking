import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/jalali_formatter.dart';
import '../../../../core/widgets/widgets.dart';
import '../../domain/entities/booking_entities.dart';

enum SlotPickerStatus { loading, loaded, error }

/// Presentational Jalali day browser + time-slot chips. Driven by whatever
/// bloc owns the data (BookingBloc for new bookings, RescheduleCubit for
/// reschedules) so both flows share identical picker semantics.
class SlotPicker extends StatelessWidget {
  final DateTime selectedDate;
  final ValueChanged<DateTime> onDateSelected;
  final SlotPickerStatus status;
  final List<TimeSlot> slots;
  final TimeSlot? selectedSlot;
  final ValueChanged<TimeSlot> onSlotSelected;
  final VoidCallback onRetry;
  final int daysToShow;

  const SlotPicker({
    super.key,
    required this.selectedDate,
    required this.onDateSelected,
    required this.status,
    required this.slots,
    required this.selectedSlot,
    required this.onSlotSelected,
    required this.onRetry,
    this.daysToShow = 14,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final today = DateTime.now();
    final days = List.generate(
      daysToShow,
      (i) => DateTime(today.year, today.month, today.day + i),
    );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          height: 84,
          child: ListView.separated(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.all(AppSpacing.md),
            itemCount: days.length,
            separatorBuilder: (_, __) => const SizedBox(width: AppSpacing.xs),
            itemBuilder: (context, index) {
              final day = days[index];
              final isSelected = day.year == selectedDate.year &&
                  day.month == selectedDate.month &&
                  day.day == selectedDate.day;
              return Semantics(
                button: true,
                selected: isSelected,
                label: JalaliFormatter.formatDate(day),
                child: InkWell(
                  borderRadius: BorderRadius.circular(AppRadius.md),
                  onTap: () => onDateSelected(day),
                  child: Container(
                    width: 64,
                    padding:
                        const EdgeInsets.symmetric(vertical: AppSpacing.xs),
                    decoration: BoxDecoration(
                      color: isSelected
                          ? theme.colorScheme.primary
                          : theme.colorScheme.surface,
                      borderRadius: BorderRadius.circular(AppRadius.md),
                      border: Border.all(
                        color: isSelected
                            ? theme.colorScheme.primary
                            : theme.colorScheme.outline,
                      ),
                    ),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Text(
                          JalaliFormatter.weekday(day),
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: isSelected
                                ? Colors.white
                                : theme.colorScheme.onSurfaceVariant,
                          ),
                        ),
                        const SizedBox(height: AppSpacing.xxs),
                        Text(
                          JalaliFormatter.formatShortDate(day),
                          style: theme.textTheme.titleSmall?.copyWith(
                            color: isSelected
                                ? Colors.white
                                : theme.colorScheme.onSurface,
                          ),
                          textAlign: TextAlign.center,
                        ),
                      ],
                    ),
                  ),
                ),
              );
            },
          ),
        ),
        Expanded(
          child: switch (status) {
            SlotPickerStatus.loading => Padding(
                padding: const EdgeInsets.all(AppSpacing.md),
                child: SkeletonLoader(
                  child: Wrap(
                    spacing: AppSpacing.xs,
                    runSpacing: AppSpacing.xs,
                    children: List.generate(
                      9,
                      (_) => SkeletonLoader.box(
                        width: 96,
                        height: 40,
                        radius: AppRadius.full,
                      ),
                    ),
                  ),
                ),
              ),
            SlotPickerStatus.error => ErrorState(onRetry: onRetry),
            SlotPickerStatus.loaded => slots.isEmpty
                ? const EmptyState(
                    icon: Icons.event_busy_outlined,
                    title: AppStrings.bookingNoSlots,
                  )
                : SingleChildScrollView(
                    padding: const EdgeInsets.all(AppSpacing.md),
                    child: Wrap(
                      spacing: AppSpacing.xs,
                      runSpacing: AppSpacing.xs,
                      children: slots
                          .map(
                            (slot) => ChoiceChip(
                              label: Text(
                                JalaliFormatter.formatTime(slot.startTime),
                              ),
                              selected: selectedSlot == slot,
                              onSelected: (_) => onSlotSelected(slot),
                            ),
                          )
                          .toList(),
                    ),
                  ),
          },
        ),
      ],
    );
  }
}
