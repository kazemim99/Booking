import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/constants/app_strings.dart';
import '../../domain/entities/onboarding_data.dart';
import '../cubit/onboarding_cubit.dart';
import '../cubit/onboarding_state.dart';
import '../widgets/step_scaffold.dart';

/// Step 5 — working hours. Per-day open toggle + open/close time pickers.
/// At least one open day is required.
class WorkingHoursStep extends StatelessWidget {
  const WorkingHoursStep({super.key});

  Future<void> _pickTime(
    BuildContext context,
    DayHours day, {
    required bool isOpenTime,
  }) async {
    final current = isOpenTime ? day.openTime : day.closeTime;
    final picked = await showTimePicker(
      context: context,
      initialTime: TimeOfDay(
        hour: current?.hours ?? 9,
        minute: current?.minutes ?? 0,
      ),
    );
    if (picked == null || !context.mounted) return;

    final cubit = context.read<OnboardingCubit>();
    final time = ClockTime(picked.hour, picked.minute);
    final updated = cubit.state.data.businessHours
        .map((d) => d.dayOfWeek != day.dayOfWeek
            ? d
            : d.copyWith(
                openTime: isOpenTime ? time : d.openTime,
                closeTime: isOpenTime ? d.closeTime : time,
              ))
        .toList();
    cubit.setBusinessHours(updated);
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
            children: hours.map((day) {
              return Card(
                child: Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 12,
                    vertical: 4,
                  ),
                  child: Row(
                    children: [
                      Expanded(
                        flex: 2,
                        child: Text(AppStrings.weekDays[day.dayOfWeek]),
                      ),
                      Switch(
                        key: Key('day-toggle-${day.dayOfWeek}'),
                        value: day.isOpen,
                        onChanged: (isOpen) {
                          final updated = hours
                              .map((d) => d.dayOfWeek == day.dayOfWeek
                                  ? d.copyWith(isOpen: isOpen)
                                  : d)
                              .toList();
                          cubit.setBusinessHours(updated);
                        },
                      ),
                      if (day.isOpen)
                        Expanded(
                          flex: 3,
                          child: Row(
                            mainAxisAlignment: MainAxisAlignment.end,
                            children: [
                              TextButton(
                                onPressed: () =>
                                    _pickTime(context, day, isOpenTime: true),
                                child: Text(day.openTime?.label ?? '--:--'),
                              ),
                              const Text('–'),
                              TextButton(
                                onPressed: () =>
                                    _pickTime(context, day, isOpenTime: false),
                                child: Text(day.closeTime?.label ?? '--:--'),
                              ),
                            ],
                          ),
                        )
                      else
                        const Expanded(
                          flex: 3,
                          child: Align(
                            alignment: Alignment.centerLeft,
                            child: Text(AppStrings.closedLabel),
                          ),
                        ),
                    ],
                  ),
                ),
              );
            }).toList(),
          ),
        );
      },
    );
  }
}
