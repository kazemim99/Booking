import 'dart:math';

import 'package:flutter/material.dart';

import '../../../config/theme/app_tokens.dart';
import '../../../core/constants/app_strings.dart';
import '../../../core/utils/persian_digits.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/widgets/app_snackbar.dart';
import '../../../core/widgets/app_text_field.dart';
import '../domain/promotion.dart';
import 'promotions_cubit.dart';

/// Creates or edits one of the salon's promotions. The common path — a title, a percentage, save — is three fields;
/// everything else has a sensible default and sits behind «چه زمانی؟» and «شرایط بیشتر». The server has the last
/// word on every rule; its Persian reason is shown and the form keeps its input.
class PromotionFormSheet extends StatefulWidget {
  final PromotionsCubit cubit;
  final Promotion? initial;

  const PromotionFormSheet({super.key, required this.cubit, this.initial});

  static Future<void> show(BuildContext context, PromotionsCubit cubit, {Promotion? initial}) {
    return showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.bottomSheet)),
      ),
      builder: (_) => Padding(
        padding: EdgeInsets.only(bottom: MediaQuery.of(context).viewInsets.bottom),
        child: PromotionFormSheet(cubit: cubit, initial: initial),
      ),
    );
  }

  @override
  State<PromotionFormSheet> createState() => _PromotionFormSheetState();
}

/// Validity presets in days; null = no end. Index matches [AppStrings.promotionDurations].
const _durationDays = <int?>[null, 7, 14, 30, 90];

class _PromotionFormSheetState extends State<PromotionFormSheet> {
  late PromotionDraft _draft =
      widget.initial == null ? const PromotionDraft() : PromotionDraft.fromPromotion(widget.initial!);

  late final _title = TextEditingController(text: _draft.title);
  late final _code = TextEditingController(text: _draft.code);
  late final _value = TextEditingController(text: _fmt(_draft.value));
  late final _cap = TextEditingController(text: _fmt(_draft.maxDiscountAmount));
  late final _minimum = TextEditingController(text: _fmt(_draft.minimumSubtotal));
  late final _totalLimit = TextEditingController(text: _draft.totalUsageLimit?.toString() ?? '');
  late final _perCustomer = TextEditingController(text: _draft.perCustomerLimit?.toString() ?? '');

  /// -1 keeps the promotion's current end (edit only); otherwise an index into [_durationDays].
  late int _duration = widget.initial == null ? 0 : (widget.initial!.endsAt == null ? 0 : -1);
  bool _submitting = false;
  String? _error;

  bool get _codeLocked => (widget.initial?.uses ?? 0) > 0;

  static String _fmt(double? v) => v == null ? '' : PriceText.format(v);

  @override
  void dispose() {
    for (final c in [_title, _code, _value, _cap, _minimum, _totalLimit, _perCustomer]) {
      c.dispose();
    }
    super.dispose();
  }

  PromotionDraft _collect() {
    final now = DateTime.now().toUtc();
    final days = _duration >= 0 ? _durationDays[_duration] : null;
    return _draft.copyWith(
      title: _title.text,
      code: _code.text,
      value: () => PriceText.parse(_value.text),
      maxDiscountAmount: () => _draft.kind == DiscountKind.percentage ? PriceText.parse(_cap.text) : null,
      minimumSubtotal: () => PriceText.parse(_minimum.text),
      totalUsageLimit: () => PriceText.parse(_totalLimit.text)?.toInt(),
      perCustomerLimit: () => PriceText.parse(_perCustomer.text)?.toInt(),
      startsAt: () => widget.initial?.startsAt ?? now,
      endsAt: () => _duration == -1 ? widget.initial?.endsAt : (days == null ? null : now.add(Duration(days: days))),
    );
  }

  Future<void> _submit() async {
    final draft = _collect();
    final problem = draft.validate(DateTime.now().toUtc());
    if (problem != null) {
      setState(() => _error = problem);
      return;
    }
    setState(() {
      _submitting = true;
      _error = null;
    });
    final failure = await widget.cubit.save(draft, editingId: widget.initial?.id);
    if (!mounted) return;
    if (failure == null) {
      Navigator.pop(context);
      AppSnackbar.success(context, AppStrings.promotionsSaved);
    } else {
      setState(() {
        _submitting = false;
        _error = failure;
      });
    }
  }

  Future<void> _pickTime({required bool start}) async {
    final current = (start ? _draft.dailyStartTime : _draft.dailyEndTime) ?? (start ? '10:00' : '14:00');
    final parts = current.split(':');
    final picked = await showTimePicker(
      context: context,
      initialTime: TimeOfDay(hour: int.parse(parts[0]), minute: int.parse(parts[1])),
      builder: (context, child) => MediaQuery(
        data: MediaQuery.of(context).copyWith(alwaysUse24HourFormat: true),
        child: child!,
      ),
    );
    if (picked == null) return;
    final text = '${picked.hour.toString().padLeft(2, '0')}:${picked.minute.toString().padLeft(2, '0')}';
    setState(() => _draft = start
        ? _draft.copyWith(dailyStartTime: () => text)
        : _draft.copyWith(dailyEndTime: () => text));
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final services = widget.cubit.state.services;
    final isPercent = _draft.kind == DiscountKind.percentage;
    final preview = describeBenefit(
      _draft.kind,
      PriceText.parse(_value.text) ?? 0,
      isPercent ? PriceText.parse(_cap.text) : null,
    );

    return SafeArea(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              widget.initial == null ? AppStrings.promotionsNew : AppStrings.promotionsEdit,
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700, color: AppColors.ink),
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              key: const Key('promotion-title'),
              controller: _title,
              label: AppStrings.promotionFormTitle,
              hint: AppStrings.promotionFormTitleHint,
              isRequired: true,
              maxLength: 80,
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: AppSpacing.sm),

            _Label(AppStrings.promotionFormHow),
            SegmentedButton<PromotionActivation>(
              key: const Key('promotion-activation'),
              segments: const [
                ButtonSegment(
                    value: PromotionActivation.automatic,
                    label: Text(AppStrings.promotionFormHowAutomatic),
                    icon: Icon(Icons.bolt_outlined)),
                ButtonSegment(
                    value: PromotionActivation.code,
                    label: Text(AppStrings.promotionFormHowCode),
                    icon: Icon(Icons.confirmation_number_outlined)),
              ],
              selected: {_draft.activation},
              onSelectionChanged: _codeLocked
                  ? null
                  : (s) => setState(() => _draft = _draft.copyWith(activation: s.first)),
            ),
            _Hint(_draft.activation == PromotionActivation.automatic
                ? AppStrings.promotionFormHowAutomaticHint
                : AppStrings.promotionFormHowCodeHint),
            if (_draft.activation == PromotionActivation.code) ...[
              const SizedBox(height: AppSpacing.sm),
              Row(
                children: [
                  Expanded(
                    child: AppTextField(
                      key: const Key('promotion-code'),
                      controller: _code,
                      label: AppStrings.promotionFormCode,
                      isRequired: true,
                      readOnly: _codeLocked,
                      contentDirection: TextDirection.ltr,
                      maxLength: 20,
                      onChanged: (_) => setState(() {}),
                    ),
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  TextButton(
                    key: const Key('promotion-code-generate'),
                    onPressed: _codeLocked
                        ? null
                        : () => setState(() => _code.text = generatePromotionCode(Random().nextInt)),
                    child: const Text(AppStrings.promotionFormCodeGenerate),
                  ),
                ],
              ),
              if (_codeLocked) const _Hint(AppStrings.promotionFormCodeLocked),
            ],
            const SizedBox(height: AppSpacing.md),

            _Label(AppStrings.promotionFormKind),
            SegmentedButton<DiscountKind>(
              key: const Key('promotion-kind'),
              segments: const [
                ButtonSegment(value: DiscountKind.percentage, label: Text(AppStrings.promotionFormPercent)),
                ButtonSegment(value: DiscountKind.fixedAmount, label: Text(AppStrings.promotionFormFixed)),
              ],
              selected: {_draft.kind},
              onSelectionChanged: (s) => setState(() => _draft = _draft.copyWith(kind: s.first)),
            ),
            const SizedBox(height: AppSpacing.sm),
            Row(
              children: [
                Expanded(
                  child: AppTextField(
                    key: const Key('promotion-value'),
                    controller: _value,
                    label: isPercent ? AppStrings.promotionFormPercentValue : AppStrings.promotionFormAmountValue,
                    isRequired: true,
                    keyboardType: TextInputType.number,
                    inputFormatters: const [ThousandsSeparatorInputFormatter()],
                    onChanged: (_) => setState(() {}),
                  ),
                ),
                if (isPercent) ...[
                  const SizedBox(width: AppSpacing.sm),
                  Expanded(
                    child: AppTextField(
                      key: const Key('promotion-cap'),
                      controller: _cap,
                      label: AppStrings.promotionFormCap,
                      keyboardType: TextInputType.number,
                      inputFormatters: const [ThousandsSeparatorInputFormatter()],
                      onChanged: (_) => setState(() {}),
                    ),
                  ),
                ],
              ],
            ),

            if (services.isNotEmpty) ...[
              const SizedBox(height: AppSpacing.md),
              _Label(AppStrings.promotionFormServices),
              Wrap(
                spacing: AppSpacing.xs,
                runSpacing: AppSpacing.xs,
                children: [
                  FilterChip(
                    key: const Key('promotion-all-services'),
                    label: const Text(AppStrings.promotionsAllServices),
                    selected: _draft.serviceIds.isEmpty,
                    onSelected: (_) => setState(() => _draft = _draft.copyWith(serviceIds: const [])),
                  ),
                  for (final s in services)
                    FilterChip(
                      label: Text(s.name),
                      selected: _draft.serviceIds.contains(s.id),
                      onSelected: (on) => setState(() => _draft = _draft.copyWith(
                          serviceIds: on
                              ? [..._draft.serviceIds, s.id]
                              : _draft.serviceIds.where((id) => id != s.id).toList())),
                    ),
                ],
              ),
            ],

            const SizedBox(height: AppSpacing.sm),
            ExpansionTile(
              key: const Key('promotion-when'),
              tilePadding: EdgeInsets.zero,
              title: const Text(AppStrings.promotionFormWhen, style: TextStyle(fontWeight: FontWeight.w700)),
              initiallyExpanded: _draft.daysOfWeek.isNotEmpty || _draft.dailyStartTime != null,
              childrenPadding: const EdgeInsets.only(bottom: AppSpacing.sm),
              expandedCrossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Wrap(
                  spacing: AppSpacing.xs,
                  children: [
                    for (final day in persianWeek)
                      FilterChip(
                        key: Key('promotion-day-$day'),
                        label: Text(AppStrings.weekdayShort[day]!),
                        selected: _draft.daysOfWeek.contains(day),
                        showCheckmark: false,
                        onSelected: (on) => setState(() => _draft = _draft.copyWith(
                            daysOfWeek: on
                                ? [..._draft.daysOfWeek, day]
                                : _draft.daysOfWeek.where((d) => d != day).toList())),
                      ),
                  ],
                ),
                const _Hint(AppStrings.promotionFormDaysHint),
                SwitchListTile(
                  key: const Key('promotion-hours-toggle'),
                  contentPadding: EdgeInsets.zero,
                  title: const Text(AppStrings.promotionFormHoursToggle),
                  value: _draft.dailyStartTime != null,
                  onChanged: (on) => setState(() => _draft = on
                      ? _draft.copyWith(dailyStartTime: () => '10:00', dailyEndTime: () => '14:00')
                      : _draft.copyWith(dailyStartTime: () => null, dailyEndTime: () => null)),
                ),
                if (_draft.dailyStartTime != null)
                  Row(
                    children: [
                      const Text(AppStrings.promotionFormFrom),
                      TextButton(
                          onPressed: () => _pickTime(start: true),
                          child: Text(PersianDigits.toPersian(_draft.dailyStartTime!))),
                      const Text(AppStrings.promotionFormTo),
                      TextButton(
                          onPressed: () => _pickTime(start: false),
                          child: Text(PersianDigits.toPersian(_draft.dailyEndTime!))),
                    ],
                  ),
                const SizedBox(height: AppSpacing.sm),
                _Label(AppStrings.promotionFormDuration),
                Wrap(
                  spacing: AppSpacing.xs,
                  runSpacing: AppSpacing.xs,
                  children: [
                    if (widget.initial?.endsAt != null)
                      ChoiceChip(
                        label: const Text(AppStrings.promotionFormKeepEnd),
                        selected: _duration == -1,
                        onSelected: (_) => setState(() => _duration = -1),
                      ),
                    for (var i = 0; i < _durationDays.length; i++)
                      ChoiceChip(
                        key: Key('promotion-duration-$i'),
                        label: Text(AppStrings.promotionDurations[i]),
                        selected: _duration == i,
                        onSelected: (_) => setState(() => _duration = i),
                      ),
                  ],
                ),
              ],
            ),
            ExpansionTile(
              key: const Key('promotion-more'),
              tilePadding: EdgeInsets.zero,
              title: const Text(AppStrings.promotionFormMore, style: TextStyle(fontWeight: FontWeight.w700)),
              initiallyExpanded: _draft.newCustomersOnly ||
                  _draft.minimumSubtotal != null ||
                  _draft.totalUsageLimit != null ||
                  _draft.perCustomerLimit != null,
              childrenPadding: const EdgeInsets.only(bottom: AppSpacing.sm),
              children: [
                SwitchListTile(
                  key: const Key('promotion-new-customers'),
                  contentPadding: EdgeInsets.zero,
                  title: const Text(AppStrings.promotionFormNewCustomers),
                  value: _draft.newCustomersOnly,
                  onChanged: (on) => setState(() => _draft = _draft.copyWith(newCustomersOnly: on)),
                ),
                AppTextField(
                  key: const Key('promotion-minimum'),
                  controller: _minimum,
                  label: AppStrings.promotionFormMinimum,
                  keyboardType: TextInputType.number,
                  inputFormatters: const [ThousandsSeparatorInputFormatter()],
                ),
                const SizedBox(height: AppSpacing.sm),
                Row(
                  children: [
                    Expanded(
                      child: AppTextField(
                        key: const Key('promotion-total-limit'),
                        controller: _totalLimit,
                        label: AppStrings.promotionFormTotalLimit,
                        hint: AppStrings.promotionFormUnlimited,
                        keyboardType: TextInputType.number,
                      ),
                    ),
                    const SizedBox(width: AppSpacing.sm),
                    Expanded(
                      child: AppTextField(
                        key: const Key('promotion-per-customer'),
                        controller: _perCustomer,
                        label: AppStrings.promotionFormPerCustomer,
                        hint: AppStrings.promotionFormUnlimited,
                        keyboardType: TextInputType.number,
                      ),
                    ),
                  ],
                ),
              ],
            ),

            const SizedBox(height: AppSpacing.sm),
            Container(
              key: const Key('promotion-preview'),
              padding: const EdgeInsets.all(AppSpacing.card),
              decoration: BoxDecoration(
                color: AppColors.successSoft,
                borderRadius: BorderRadius.circular(AppRadius.md),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(AppStrings.promotionFormPreview, style: theme.textTheme.bodySmall),
                  const SizedBox(height: AppSpacing.xs),
                  Text(preview,
                      style: theme.textTheme.titleSmall?.copyWith(color: AppColors.success, fontWeight: FontWeight.w700)),
                  if (_title.text.trim().isNotEmpty) Text(_title.text.trim(), style: theme.textTheme.bodyMedium),
                ],
              ),
            ),
            const _Hint(AppStrings.promotionFormFundingNote),
            if (_error != null) ...[
              const SizedBox(height: AppSpacing.sm),
              Text(_error!, key: const Key('promotion-error'), style: const TextStyle(color: AppColors.danger)),
            ],
            const SizedBox(height: AppSpacing.md),
            AppButton(
              key: const Key('promotion-save'),
              label: AppStrings.promotionFormSave,
              loading: _submitting,
              onPressed: _submitting || _title.text.trim().isEmpty ? null : _submit,
            ),
          ],
        ),
      ),
    );
  }
}

class _Label extends StatelessWidget {
  final String text;
  const _Label(this.text);

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: AppSpacing.xs),
        child: Text(text, style: const TextStyle(fontWeight: FontWeight.w700, color: AppColors.ink)),
      );
}

class _Hint extends StatelessWidget {
  final String text;
  const _Hint(this.text);

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(top: AppSpacing.xs),
        child: Text(text, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: AppColors.subtitle)),
      );
}
