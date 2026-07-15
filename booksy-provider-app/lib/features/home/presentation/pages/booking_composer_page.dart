import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/app_button.dart';
import '../../../../core/widgets/app_error_state.dart';
import '../../../../core/widgets/app_snackbar.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../../domain/entities/composer_models.dart';
import '../cubit/composer_cubit.dart';

/// One-screen booking composer (spec: provider-booking-composer):
/// service → staff → date → available slot → optional walk-in client + notes.
/// Pops with `true` when the booking is created so the caller refreshes Home.
class BookingComposerPage extends StatelessWidget {
  /// Pre-sets the composed day (calendar-initiated creation); null = today.
  final DateTime? initialDate;

  const BookingComposerPage({super.key, this.initialDate});

  @override
  Widget build(BuildContext context) {
    return BlocProvider<ComposerCubit>(
      create: (_) => getIt<ComposerCubit>(param1: initialDate)..load(),
      child: const ComposerView(),
    );
  }
}

/// Separated from [BookingComposerPage] so tests can pump it with a fake cubit.
class ComposerView extends StatefulWidget {
  const ComposerView({super.key});

  @override
  State<ComposerView> createState() => _ComposerViewState();
}

class _ComposerViewState extends State<ComposerView> {
  final _clientName = TextEditingController();
  final _clientPhone = TextEditingController();
  final _notes = TextEditingController();

  @override
  void dispose() {
    _clientName.dispose();
    _clientPhone.dispose();
    _notes.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return BlocConsumer<ComposerCubit, ComposerState>(
      listenWhen: (prev, next) =>
          prev.submitted != next.submitted || prev.error != next.error,
      listener: (context, state) {
        if (state.submitted) {
          Navigator.of(context).pop(true);
        } else if (state.error != null &&
            state.status == ComposerStatus.ready) {
          AppSnackbar.error(context, state.error!);
        }
      },
      builder: (context, state) {
        final cubit = context.read<ComposerCubit>();
        return Scaffold(
          backgroundColor: Colors.white,
          appBar: AppBar(
            backgroundColor: Colors.white,
            surfaceTintColor: Colors.transparent,
            elevation: 0,
            title: const Text(
              AppStrings.composerTitle,
              style: TextStyle(
                fontSize: 17,
                fontWeight: FontWeight.w700,
                color: AppColors.ink,
              ),
            ),
          ),
          body: switch (state.status) {
            ComposerStatus.loading =>
              const Center(child: CircularProgressIndicator()),
            ComposerStatus.failed => AppErrorState(
                message: state.error ?? AppStrings.homeLoadError,
                onRetry: cubit.load,
              ),
            ComposerStatus.ready => _form(context, state, cubit),
          },
          bottomNavigationBar: state.status == ComposerStatus.ready
              ? SafeArea(
                  child: Padding(
                    padding: const EdgeInsets.all(AppSpacing.md),
                    child: AppButton(
                      key: const Key('composer-submit'),
                      label: AppStrings.composerSubmit,
                      loading: state.submitting,
                      onPressed: state.canSubmit
                          ? () => cubit.submit(
                                clientName: _clientName.text,
                                clientPhone: _clientPhone.text,
                                notes: _notes.text,
                              )
                          : null,
                    ),
                  ),
                )
              : null,
        );
      },
    );
  }

  Widget _form(BuildContext context, ComposerState state, ComposerCubit cubit) {
    return ListView(
      padding: const EdgeInsets.all(AppSpacing.md),
      children: [
        _sectionLabel(AppStrings.composerServiceLabel),
        _pickerField(
          key: const Key('composer-service-field'),
          hint: AppStrings.composerPickService,
          value: state.service?.name,
          onTap: () => _pickService(context, state, cubit),
        ),
        const SizedBox(height: AppSpacing.md),
        _sectionLabel(AppStrings.composerStaffLabel),
        _pickerField(
          key: const Key('composer-staff-field'),
          hint: AppStrings.composerPickStaff,
          value: state.staff?.name,
          onTap: () => _pickStaff(context, state, cubit),
        ),
        const SizedBox(height: AppSpacing.md),
        _sectionLabel(AppStrings.composerDateLabel),
        _DateStrip(selected: state.date, onSelect: cubit.selectDate),
        const SizedBox(height: AppSpacing.md),
        _sectionLabel(AppStrings.composerSlotsLabel),
        _slots(state, cubit),
        const SizedBox(height: AppSpacing.lg),
        AppTextField(
          key: const Key('composer-client-name'),
          controller: _clientName,
          label: AppStrings.composerClientName,
        ),
        const SizedBox(height: AppSpacing.md),
        AppTextField(
          key: const Key('composer-client-phone'),
          controller: _clientPhone,
          label: AppStrings.composerClientPhone,
          keyboardType: TextInputType.phone,
        ),
        const SizedBox(height: AppSpacing.md),
        AppTextField(
          key: const Key('composer-notes'),
          controller: _notes,
          label: AppStrings.composerNotes,
        ),
        const SizedBox(height: AppSpacing.xl),
      ],
    );
  }

  Widget _sectionLabel(String text) => Padding(
        padding: const EdgeInsets.only(bottom: AppSpacing.sm),
        child: Text(
          text,
          style: const TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.w600,
            color: AppColors.ink,
          ),
        ),
      );

  Widget _pickerField({
    required Key key,
    required String hint,
    required String? value,
    required VoidCallback onTap,
  }) {
    return InkWell(
      key: key,
      onTap: onTap,
      borderRadius: BorderRadius.circular(AppRadius.field),
      child: Container(
        constraints: const BoxConstraints(minHeight: 48),
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.card,
          vertical: AppSpacing.sm,
        ),
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(AppRadius.field),
          border: Border.all(
            color: AppColors.border,
            width: AppDimens.inputBorderWidth,
          ),
        ),
        child: Row(
          children: [
            Expanded(
              child: Text(
                value ?? hint,
                style: TextStyle(
                  fontSize: 15,
                  color: value == null ? AppColors.hint : AppColors.ink,
                ),
              ),
            ),
            const Icon(Icons.expand_more,
                size: AppIconSize.action, color: AppColors.muted),
          ],
        ),
      ),
    );
  }

  Widget _slots(ComposerState state, ComposerCubit cubit) {
    switch (state.slotsStatus) {
      case SlotsStatus.idle:
        return const Text(
          AppStrings.composerPickService,
          style: TextStyle(fontSize: 13, color: AppColors.muted),
        );
      case SlotsStatus.loading:
        return const Padding(
          padding: EdgeInsets.symmetric(vertical: AppSpacing.md),
          child: Center(child: CircularProgressIndicator(strokeWidth: 2)),
        );
      case SlotsStatus.failed:
        return Row(
          children: [
            const Expanded(
              child: Text(
                AppStrings.composerSlotsError,
                style: TextStyle(fontSize: 13, color: AppColors.muted),
              ),
            ),
            TextButton(
              key: const Key('composer-slots-retry'),
              onPressed: cubit.retrySlots,
              child: const Text(AppStrings.retry),
            ),
          ],
        );
      case SlotsStatus.ready:
        if (state.slots.isEmpty) {
          return const Text(
            AppStrings.composerNoSlots,
            key: Key('composer-no-slots'),
            style: TextStyle(fontSize: 13, color: AppColors.muted),
          );
        }
        return Wrap(
          spacing: AppSpacing.sm,
          runSpacing: AppSpacing.sm,
          children: [
            for (final slot in state.slots)
              ChoiceChip(
                key: Key(
                    'slot-${slot.hour.toString().padLeft(2, '0')}${slot.minute.toString().padLeft(2, '0')}'),
                label: Text(
                  '${slot.hour.toString().padLeft(2, '0')}:${slot.minute.toString().padLeft(2, '0')}',
                ),
                selected: state.slot == slot,
                onSelected: (_) => cubit.selectSlot(slot),
                selectedColor: AppColors.primarySoft,
                labelStyle: TextStyle(
                  color:
                      state.slot == slot ? AppColors.primary : AppColors.ink,
                ),
              ),
          ],
        );
    }
  }

  void _pickService(
      BuildContext context, ComposerState state, ComposerCubit cubit) {
    _showPickerSheet<ComposerService>(
      context: context,
      title: AppStrings.composerPickService,
      options: state.catalog.services,
      labelOf: (s) => s.durationMinutes > 0
          ? AppStrings.composerServiceMeta(s.name, s.durationMinutes)
          : s.name,
      keyOf: (s) => 'pick-service-${s.id}',
      onPicked: cubit.selectService,
    );
  }

  void _pickStaff(
      BuildContext context, ComposerState state, ComposerCubit cubit) {
    _showPickerSheet<ComposerStaff>(
      context: context,
      title: AppStrings.composerPickStaff,
      options: state.catalog.staff,
      labelOf: (s) => s.name,
      keyOf: (s) => 'pick-staff-${s.id}',
      onPicked: cubit.selectStaff,
    );
  }

  void _showPickerSheet<T>({
    required BuildContext context,
    required String title,
    required List<T> options,
    required String Function(T) labelOf,
    required String Function(T) keyOf,
    required void Function(T) onPicked,
  }) {
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
              child: Text(
                title,
                style: const TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                  color: AppColors.ink,
                ),
              ),
            ),
            Flexible(
              child: ListView(
                shrinkWrap: true,
                children: [
                  for (final option in options)
                    ListTile(
                      key: Key(keyOf(option)),
                      title: Text(labelOf(option)),
                      onTap: () {
                        Navigator.pop(sheetContext);
                        onPicked(option);
                      },
                    ),
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.sm),
          ],
        ),
      ),
    );
  }
}

/// Horizontal 14-day date strip: امروز، فردا، then weekday names.
/// (Jalali day numbers need a calendar package — flagged follow-up; weekday
/// labels are unambiguous for a two-week horizon.)
class _DateStrip extends StatelessWidget {
  final DateTime selected;
  final void Function(DateTime) onSelect;

  const _DateStrip({required this.selected, required this.onSelect});

  @override
  Widget build(BuildContext context) {
    final today = DateTime.now();
    final start = DateTime(today.year, today.month, today.day);

    return SizedBox(
      height: 44,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        itemCount: 14,
        separatorBuilder: (_, _) => const SizedBox(width: AppSpacing.sm),
        itemBuilder: (context, i) {
          final day = start.add(Duration(days: i));
          final label = switch (i) {
            0 => AppStrings.composerToday,
            1 => AppStrings.composerTomorrow,
            // DateTime.weekday: Mon=1..Sun=7; AppStrings.weekDays starts at
            // یکشنبه (Sunday).
            _ => AppStrings.weekDays[day.weekday % 7],
          };
          final isSelected = day == selected;
          return ChoiceChip(
            key: Key('date-$i'),
            label: Text(i >= 2 ? '$label ${day.day}/${day.month}' : label),
            selected: isSelected,
            onSelected: (_) => onSelect(day),
            selectedColor: AppColors.primarySoft,
            labelStyle: TextStyle(
              color: isSelected ? AppColors.primary : AppColors.ink,
            ),
          );
        },
      ),
    );
  }
}
