import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/app_button.dart';
import '../../../../core/widgets/app_error_state.dart';
import '../../../../core/widgets/app_page_scaffold.dart';
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

  /// Pre-fill the walk-in client fields (book-again from the Clients tab).
  final String? initialClientName;
  final String? initialClientPhone;

  const BookingComposerPage({
    super.key,
    this.initialDate,
    this.initialClientName,
    this.initialClientPhone,
  });

  @override
  Widget build(BuildContext context) {
    return BlocProvider<ComposerCubit>(
      create: (_) => getIt<ComposerCubit>(param1: initialDate)..load(),
      child: ComposerView(
        initialClientName: initialClientName,
        initialClientPhone: initialClientPhone,
      ),
    );
  }
}

/// Separated from [BookingComposerPage] so tests can pump it with a fake cubit.
class ComposerView extends StatefulWidget {
  final String? initialClientName;
  final String? initialClientPhone;

  const ComposerView({
    super.key,
    this.initialClientName,
    this.initialClientPhone,
  });

  @override
  State<ComposerView> createState() => _ComposerViewState();
}

class _ComposerViewState extends State<ComposerView> {
  late final _clientName =
      TextEditingController(text: widget.initialClientName ?? '');
  late final _clientPhone =
      TextEditingController(text: widget.initialClientPhone ?? '');
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
        return AppPageScaffold(
          title: AppStrings.composerTitle,
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
        // No staff ⇒ the backend can never generate a slot for any date.
        // Say it once, up front, with the fix one tap away.
        if (state.catalog.hasNoStaff) ...[
          const _NoStaffNotice(),
          const SizedBox(height: AppSpacing.md),
        ],
        _sectionLabel(AppStrings.composerServiceLabel),
        _pickerField(
          key: const Key('composer-service-field'),
          hint: AppStrings.composerServicesHint,
          value: state.services.isEmpty
              ? null
              : state.services.map((s) => s.name).join('، '),
          onTap: () => _pickServices(context, state, cubit),
        ),
        if (state.services.isNotEmpty) ...[
          const SizedBox(height: AppSpacing.xs),
          Text(
            AppStrings.composerServicesSummary(
              state.totalDurationMinutes,
              _formatPrice(state.totalPrice),
            ),
            key: const Key('composer-services-summary'),
            style: const TextStyle(fontSize: 13, color: AppColors.subtitle),
          ),
        ],
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
          // Prefer the server's reason ("no staff added yet", "closed", …):
          // without it every empty day reads as "fully booked", hiding a
          // setup problem the provider is the only one who can fix.
          final reason = state.slotsUnavailableReason;
          if (reason == null) {
            return const Text(
              AppStrings.composerNoSlots,
              key: Key('composer-no-slots'),
              style: TextStyle(fontSize: 13, color: AppColors.muted),
            );
          }
          return Row(
            key: const Key('composer-no-slots-reason'),
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Icon(Icons.info_outline,
                  size: AppIconSize.sm, color: AppColors.warning),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: Text(
                  reason,
                  style: const TextStyle(fontSize: 13, color: AppColors.ink),
                ),
              ),
            ],
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

  static String _formatPrice(double amount) {
    final whole = amount.round().toString();
    final b = StringBuffer();
    for (var i = 0; i < whole.length; i++) {
      if (i > 0 && (whole.length - i) % 3 == 0) b.write(',');
      b.write(whole[i]);
    }
    return b.toString();
  }

  /// Multi-select sheet: tap services to add/remove them from the visit.
  /// Selections apply live (slots re-fetch against the combined duration),
  /// so the sheet is a simple checklist with a Done button to dismiss.
  void _pickServices(
      BuildContext context, ComposerState state, ComposerCubit cubit) {
    showModalBottomSheet<void>(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(
          top: Radius.circular(AppRadius.bottomSheet),
        ),
      ),
      builder: (sheetContext) => SafeArea(
        // Rebuild the checklist as selections change, from the live cubit.
        child: BlocBuilder<ComposerCubit, ComposerState>(
          bloc: cubit,
          builder: (context, s) => Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Padding(
                padding: const EdgeInsets.all(AppSpacing.md),
                child: Text(
                  AppStrings.composerServicesHint,
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
                    for (final svc in s.catalog.services)
                      CheckboxListTile(
                        key: Key('pick-service-${svc.id}'),
                        value: s.services.any((x) => x.id == svc.id),
                        onChanged: (_) => cubit.toggleService(svc),
                        activeColor: AppColors.success,
                        title: Text(
                          svc.durationMinutes > 0
                              ? AppStrings.composerServiceMeta(
                                  svc.name, svc.durationMinutes)
                              : svc.name,
                        ),
                      ),
                  ],
                ),
              ),
              Padding(
                padding: const EdgeInsets.all(AppSpacing.md),
                child: AppButton(
                  key: const Key('composer-services-done'),
                  label: AppStrings.composerDone,
                  size: AppButtonSize.medium,
                  onPressed: () => Navigator.pop(sheetContext),
                ),
              ),
            ],
          ),
        ),
      ),
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

/// Up-front notice that the business has no staff yet. Slot generation
/// requires at least one staff member server-side, so without one every date
/// looks "fully booked" — this names the real cause and links to the fix.
class _NoStaffNotice extends StatelessWidget {
  const _NoStaffNotice();

  @override
  Widget build(BuildContext context) {
    return Container(
      key: const Key('composer-no-staff'),
      padding: const EdgeInsets.all(AppSpacing.card),
      decoration: BoxDecoration(
        color: AppColors.successSoft,
        borderRadius: BorderRadius.circular(AppRadius.card),
        border: Border.all(color: AppColors.border),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Icon(Icons.groups_outlined,
              size: AppIconSize.md, color: AppColors.primary),
          const SizedBox(width: AppSpacing.card),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  AppStrings.composerNoStaffTitle,
                  style: TextStyle(
                    color: AppColors.ink,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: AppSpacing.xs),
                const Text(
                  AppStrings.composerNoStaffBody,
                  style: TextStyle(fontSize: 13, color: AppColors.subtitle),
                ),
                const SizedBox(height: AppSpacing.sm),
                AppButton.secondary(
                  key: const Key('composer-add-staff'),
                  label: AppStrings.composerNoStaffCta,
                  size: AppButtonSize.small,
                  onPressed: () => context.push(Routes.moreStaff),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
