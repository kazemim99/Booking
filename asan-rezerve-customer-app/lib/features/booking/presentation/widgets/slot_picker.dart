import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../../../../config/theme/app_colors.dart';
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

  /// The salon's own answer for an empty day; the generic line is used when there is none.
  final String? emptyReason;

  /// The first day of the strip; the device's today when null. Injectable so the strip is deterministic in tests.
  final DateTime? today;

  /// [DateTime.weekday] values the salon is shut. Those days are drawn muted and cannot be picked. Empty (the
  /// default) leaves every day pickable.
  final Set<int> closedWeekdays;

  /// Offered on a day with no free time as «نزدیک‌ترین روز با وقت خالی»; no button when null.
  final VoidCallback? onFindNextFreeDay;

  /// A short line under the day strip saying why this day is shown (the app chose it); nothing when null.
  final String? notice;

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
    this.emptyReason,
    this.today,
    this.closedWeekdays = const {},
    this.onFindNextFreeDay,
    this.notice,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final today = this.today ?? DateTime.now();
    final days = List.generate(
      daysToShow,
      (i) => DateTime(today.year, today.month, today.day + i),
    );

    // The strip is as tall as its two lines at the reader's text size (a fixed height overflowed at 1.3x), and never
    // shorter than a touch target.
    // Each painter is disposed once measured: the picker rebuilds on every change of the bloc driving it.
    final scaler = MediaQuery.textScalerOf(context);
    double lineHeight(TextStyle? style) {
      final painter = TextPainter(
        text: TextSpan(text: 'ی۲', style: style),
        textDirection: TextDirection.rtl,
        textScaler: scaler,
        maxLines: 1,
      )..layout();
      try {
        return painter.height;
      } finally {
        painter.dispose();
      }
    }

    final chipHeight = math.max(
      AppTouchTarget.min,
      lineHeight(theme.textTheme.bodySmall) +
          AppSpacing.xxs +
          lineHeight(theme.textTheme.titleSmall) +
          2 * AppSpacing.xs +
          2,
    );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          height: chipHeight + 2 * AppSpacing.sm,
          child: ListView.separated(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(
              horizontal: AppSpacing.md,
              vertical: AppSpacing.sm,
            ),
            itemCount: days.length,
            separatorBuilder: (_, __) => const SizedBox(width: AppSpacing.xs),
            itemBuilder: (context, index) {
              final day = days[index];
              final isSelected = day.year == selectedDate.year &&
                  day.month == selectedDate.month &&
                  day.day == selectedDate.day;
              final isClosed = closedWeekdays.contains(day.weekday);
              final chip = Container(
                constraints: const BoxConstraints(minWidth: 64),
                padding: const EdgeInsets.symmetric(
                  horizontal: AppSpacing.xs,
                  vertical: AppSpacing.xs,
                ),
                decoration: BoxDecoration(
                  color: isSelected
                      ? theme.colorScheme.primary
                      : isClosed
                          ? AppColors.surfaceSoft
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
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: AppSpacing.xxs),
                    Text(
                      isClosed && !isSelected
                          ? AppStrings.bookingDayClosed
                          : JalaliFormatter.formatShortDate(day),
                      style: theme.textTheme.titleSmall?.copyWith(
                        color: isSelected
                            ? Colors.white
                            : theme.colorScheme.onSurface,
                      ),
                      textAlign: TextAlign.center,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                ),
              );
              return Semantics(
                key: ValueKey(
                  'slot-picker-day-${day.year}-${day.month}-${day.day}',
                ),
                container: true,
                button: !isClosed,
                enabled: !isClosed,
                selected: isSelected,
                label: isClosed
                    ? '${JalaliFormatter.formatDate(day)}، ${AppStrings.bookingDayClosed}'
                    : JalaliFormatter.formatDate(day),
                excludeSemantics: true,
                onTap: isClosed ? null : () => onDateSelected(day),
                child: isClosed
                    // The salon is shut every such weekday: muted and not tappable, so the customer never lands on
                    // a day that cannot have times.
                    ? Opacity(opacity: 0.55, child: chip)
                    : InkWell(
                        borderRadius: BorderRadius.circular(AppRadius.md),
                        onTap: () => onDateSelected(day),
                        child: chip,
                      ),
              );
            },
          ),
        ),
        if (notice != null)
          Padding(
            padding: const EdgeInsetsDirectional.fromSTEB(
              AppSpacing.md,
              0,
              AppSpacing.md,
              AppSpacing.xs,
            ),
            child: Container(
              key: const Key('slot-picker-notice'),
              padding: const EdgeInsets.all(AppSpacing.sm),
              decoration: BoxDecoration(
                color: AppColors.infoTint,
                borderRadius: BorderRadius.circular(AppRadius.md),
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Icon(
                    Icons.info_outline,
                    size: AppIconSize.sm,
                    color: AppColors.infoText,
                  ),
                  const SizedBox(width: AppSpacing.xs),
                  Expanded(
                    child: Text(
                      notice!,
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: AppColors.infoText,
                      ),
                      textAlign: TextAlign.start,
                    ),
                  ),
                ],
              ),
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
                // Scrollable so a long reason, the button and a large text size never overflow a small screen.
                ? LayoutBuilder(
                    builder: (context, constraints) => SingleChildScrollView(
                      child: ConstrainedBox(
                        constraints:
                            BoxConstraints(minHeight: constraints.maxHeight),
                        child: EmptyState(
                          icon: Icons.event_busy_outlined,
                          title: emptyReason ?? AppStrings.bookingNoSlots,
                          ctaLabel: onFindNextFreeDay == null
                              ? null
                              : AppStrings.bookingFindNextFreeDay,
                          onCta: onFindNextFreeDay,
                        ),
                      ),
                    ),
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
