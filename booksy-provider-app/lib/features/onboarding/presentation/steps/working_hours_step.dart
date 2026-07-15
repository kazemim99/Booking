import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../domain/entities/onboarding_data.dart';
import '../cubit/onboarding_cubit.dart';
import '../cubit/onboarding_state.dart';
import '../widgets/step_scaffold.dart';

/// Step 5 — working hours. Per-day open toggle, open/close times, plus
/// per-day breaks (add/remove) and a "copy to all days" shortcut — parity with
/// the Vue DayScheduleEditor. At least one open day is required.
class WorkingHoursStep extends StatelessWidget {
  const WorkingHoursStep({super.key});

  // ---- mutation helpers (immutable list rebuilds via the cubit) ----

  void _replaceDay(BuildContext context, DayHours updated) {
    final cubit = context.read<OnboardingCubit>();
    final hours = cubit.state.data.businessHours
        .map((d) => d.dayOfWeek == updated.dayOfWeek ? updated : d)
        .toList();
    cubit.setBusinessHours(hours);
  }

  Future<void> _pickTime(
    BuildContext context,
    DayHours day, {
    required bool isOpenTime,
  }) async {
    final current = isOpenTime ? day.openTime : day.closeTime;
    final picked = await showTimePicker(
      context: context,
      initialTime: TimeOfDay(
        hour: current?.hours ?? (isOpenTime ? 9 : 18),
        minute: current?.minutes ?? 0,
      ),
    );
    if (picked == null || !context.mounted) return;
    final time = ClockTime(picked.hour, picked.minute);
    _replaceDay(
      context,
      day.copyWith(
        openTime: isOpenTime ? time : day.openTime,
        closeTime: isOpenTime ? day.closeTime : time,
      ),
    );
  }

  Future<void> _pickBreakTime(
    BuildContext context,
    DayHours day,
    int index, {
    required bool isStart,
  }) async {
    final br = day.breaks[index];
    final current = isStart ? br.start : br.end;
    final picked = await showTimePicker(
      context: context,
      initialTime: TimeOfDay(hour: current.hours, minute: current.minutes),
    );
    if (picked == null || !context.mounted) return;
    final time = ClockTime(picked.hour, picked.minute);
    final breaks = [...day.breaks];
    breaks[index] = BreakTime(
      isStart ? time : br.start,
      isStart ? br.end : time,
    );
    _replaceDay(context, day.copyWith(breaks: breaks));
  }

  void _addBreak(BuildContext context, DayHours day) {
    // Default to a midday break; the user adjusts and validation checks it sits
    // within business hours before advancing.
    final breaks = [
      ...day.breaks,
      const BreakTime(ClockTime(13, 0), ClockTime(14, 0)),
    ];
    _replaceDay(context, day.copyWith(breaks: breaks));
  }

  void _removeBreak(BuildContext context, DayHours day, int index) {
    final breaks = [...day.breaks]..removeAt(index);
    _replaceDay(context, day.copyWith(breaks: breaks));
  }

  void _copyToAllDays(BuildContext context, DayHours source) {
    final cubit = context.read<OnboardingCubit>();
    final hours = cubit.state.data.businessHours
        .map((d) => d.dayOfWeek == source.dayOfWeek
            ? d
            : d.copyWith(
                isOpen: source.isOpen,
                openTime: source.openTime,
                closeTime: source.closeTime,
                breaks: [...source.breaks],
              ))
        .toList();
    cubit.setBusinessHours(hours);
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(const SnackBar(content: Text(AppStrings.hoursCopied)));
  }

  @override
  Widget build(BuildContext context) {
    final cubit = context.read<OnboardingCubit>();
    return BlocBuilder<OnboardingCubit, OnboardingState>(
      builder: (context, state) {
        final hours = state.data.businessHours;
        return StepScaffold(
          title: AppStrings.hoursTitle,
          subtitle: AppStrings.hoursSubtitle,
          loading: state.isSaving,
          onBack: cubit.back,
          onNext: cubit.next,
          child: Column(
            children: [
              for (final day in hours) _dayCard(context, day),
            ],
          ),
        );
      },
    );
  }

  Widget _dayCard(BuildContext context, DayHours day) {
    final theme = Theme.of(context);
    return Card(
      key: Key('day-card-${day.dayOfWeek}'),
      margin: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: Padding(
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.md,
          vertical: AppSpacing.sm,
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    AppStrings.weekDays[day.dayOfWeek],
                    style: theme.textTheme.titleMedium,
                  ),
                ),
                Text(day.isOpen ? AppStrings.openLabel : AppStrings.closedLabel,
                    style: theme.textTheme.bodySmall),
                Switch(
                  key: Key('day-toggle-${day.dayOfWeek}'),
                  value: day.isOpen,
                  onChanged: (isOpen) =>
                      _replaceDay(context, day.copyWith(isOpen: isOpen)),
                ),
              ],
            ),
            if (day.isOpen) ...[
              const SizedBox(height: AppSpacing.xs),
              // Open / close range.
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  _timeChip(
                    context,
                    key: Key('open-${day.dayOfWeek}'),
                    label: day.openTime?.label ?? '--:--',
                    onTap: () => _pickTime(context, day, isOpenTime: true),
                  ),
                  const Padding(
                    padding: EdgeInsets.symmetric(horizontal: AppSpacing.sm),
                    child: Text('تا'),
                  ),
                  _timeChip(
                    context,
                    key: Key('close-${day.dayOfWeek}'),
                    label: day.closeTime?.label ?? '--:--',
                    onTap: () => _pickTime(context, day, isOpenTime: false),
                  ),
                ],
              ),
              const Divider(height: AppSpacing.lg),
              _breaksSection(context, day),
              Align(
                alignment: Alignment.centerLeft,
                child: TextButton.icon(
                  key: Key('copy-${day.dayOfWeek}'),
                  onPressed: () => _copyToAllDays(context, day),
                  icon: const Icon(Icons.copy_all_outlined, size: 18),
                  label: const Text(AppStrings.copyToAllDays),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _breaksSection(BuildContext context, DayHours day) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            Expanded(
              child: Text(AppStrings.breaksLabel,
                  style: theme.textTheme.bodyMedium),
            ),
            TextButton.icon(
              key: Key('add-break-${day.dayOfWeek}'),
              onPressed: () => _addBreak(context, day),
              icon: const Icon(Icons.add, size: 18),
              label: const Text(AppStrings.addBreak),
            ),
          ],
        ),
        if (day.breaks.isEmpty)
          Align(
            alignment: Alignment.centerRight,
            child: Text(
              AppStrings.noBreaks,
              style: theme.textTheme.bodySmall
                  ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
          )
        else
          for (int i = 0; i < day.breaks.length; i++)
            Padding(
              key: Key('break-${day.dayOfWeek}-$i'),
              padding: const EdgeInsets.only(bottom: AppSpacing.xs),
              child: Row(
                children: [
                  _timeChip(
                    context,
                    label: day.breaks[i].start.label,
                    onTap: () =>
                        _pickBreakTime(context, day, i, isStart: true),
                  ),
                  const Padding(
                    padding: EdgeInsets.symmetric(horizontal: AppSpacing.xs),
                    child: Text('تا'),
                  ),
                  _timeChip(
                    context,
                    label: day.breaks[i].end.label,
                    onTap: () =>
                        _pickBreakTime(context, day, i, isStart: false),
                  ),
                  const Spacer(),
                  IconButton(
                    key: Key('remove-break-${day.dayOfWeek}-$i'),
                    tooltip: AppStrings.removeBreak,
                    visualDensity: VisualDensity.compact,
                    constraints: const BoxConstraints(),
                    padding: const EdgeInsets.all(AppSpacing.xs),
                    icon: Icon(Icons.delete_outline,
                        color: theme.colorScheme.error),
                    onPressed: () => _removeBreak(context, day, i),
                  ),
                ],
              ),
            ),
      ],
    );
  }

  Widget _timeChip(
    BuildContext context, {
    Key? key,
    required String label,
    required VoidCallback onTap,
  }) {
    // NB: the app's OutlinedButton theme uses Size.fromHeight (== infinite
    // width, for full-width buttons). In a Row that forces infinite width and
    // crashes layout — so pin a finite min size for these inline chips.
    return OutlinedButton(
      key: key,
      onPressed: onTap,
      style: OutlinedButton.styleFrom(
        minimumSize: const Size(0, 44),
        padding: const EdgeInsets.symmetric(horizontal: AppSpacing.sm),
        tapTargetSize: MaterialTapTargetSize.shrinkWrap,
      ),
      child: Text(label),
    );
  }
}
