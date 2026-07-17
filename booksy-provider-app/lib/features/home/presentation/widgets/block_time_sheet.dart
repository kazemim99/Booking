import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/widgets/app_button.dart';
import '../../../../core/widgets/app_snackbar.dart';
import '../../../../core/widgets/app_text_field.dart';

/// Creates a per-date availability exception (spec: provider-block-time).
typedef BlockTimeSubmit = Future<Failure?> Function({
  required DateTime date,
  String? openTime,
  String? closeTime,
  required String reason,
});

/// «مسدود کردن زمان» — all-day closed, or modified hours for one date.
/// Shared by the Home and Calendar ⊕ menus (design D1).
class BlockTimeSheet extends StatefulWidget {
  final DateTime initialDate;
  final BlockTimeSubmit onSubmit;

  const BlockTimeSheet({
    super.key,
    required this.initialDate,
    required this.onSubmit,
  });

  static Future<void> show(
    BuildContext context, {
    required DateTime initialDate,
    required BlockTimeSubmit onSubmit,
  }) {
    return showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(
          top: Radius.circular(AppRadius.bottomSheet),
        ),
      ),
      builder: (_) => Padding(
        padding:
            EdgeInsets.only(bottom: MediaQuery.of(context).viewInsets.bottom),
        child: BlockTimeSheet(initialDate: initialDate, onSubmit: onSubmit),
      ),
    );
  }

  @override
  State<BlockTimeSheet> createState() => _BlockTimeSheetState();
}

class _BlockTimeSheetState extends State<BlockTimeSheet> {
  final _reason = TextEditingController();
  late DateTime _date = widget.initialDate;
  bool _allDay = true;
  TimeOfDay? _open;
  TimeOfDay? _close;
  bool _submitting = false;

  @override
  void dispose() {
    _reason.dispose();
    super.dispose();
  }

  bool get _canSubmit =>
      _reason.text.trim().isNotEmpty &&
      (_allDay || (_open != null && _close != null)) &&
      !_submitting;

  static String _hhmm(TimeOfDay t) =>
      '${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')}';

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: DateTime(now.year, now.month, now.day),
      lastDate: now.add(const Duration(days: 365)),
    );
    if (picked != null) setState(() => _date = picked);
  }

  Future<void> _pickTime(bool isOpen) async {
    final picked = await showTimePicker(
      context: context,
      initialTime: (isOpen ? _open : _close) ??
          TimeOfDay(hour: isOpen ? 9 : 18, minute: 0),
    );
    if (picked == null) return;
    setState(() => isOpen ? _open = picked : _close = picked);
  }

  Future<void> _submit() async {
    setState(() => _submitting = true);
    final failure = await widget.onSubmit(
      date: _date,
      openTime: _allDay ? null : _hhmm(_open!),
      closeTime: _allDay ? null : _hhmm(_close!),
      reason: _reason.text.trim(),
    );
    if (!mounted) return;
    if (failure == null) {
      Navigator.pop(context);
      AppSnackbar.success(context, AppStrings.blockTimeCreated);
    } else {
      setState(() => _submitting = false);
      AppSnackbar.error(context, failure.message);
    }
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              AppStrings.blockTimeTitle,
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w700,
                color: AppColors.ink,
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            OutlinedButton.icon(
              key: const Key('block-date'),
              onPressed: _pickDate,
              icon: const Icon(Icons.event_outlined, size: AppIconSize.action),
              label: Text('${_date.day}/${_date.month}/${_date.year}'),
            ),
            SwitchListTile(
              key: const Key('block-all-day'),
              contentPadding: EdgeInsets.zero,
              value: _allDay,
              onChanged: (v) => setState(() => _allDay = v),
              title: const Text(
                AppStrings.blockTimeAllDay,
                style: TextStyle(fontSize: 14, color: AppColors.ink),
              ),
            ),
            if (!_allDay)
              Row(
                children: [
                  const Text(AppStrings.blockTimeFrom,
                      style:
                          TextStyle(fontSize: 13, color: AppColors.muted)),
                  TextButton(
                    key: const Key('block-open'),
                    onPressed: () => _pickTime(true),
                    child: Text(_open == null ? '—' : _hhmm(_open!)),
                  ),
                  const Text(AppStrings.blockTimeTo,
                      style:
                          TextStyle(fontSize: 13, color: AppColors.muted)),
                  TextButton(
                    key: const Key('block-close'),
                    onPressed: () => _pickTime(false),
                    child: Text(_close == null ? '—' : _hhmm(_close!)),
                  ),
                ],
              ),
            AppTextField(
              key: const Key('block-reason'),
              controller: _reason,
              label: AppStrings.blockTimeReason,
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: AppSpacing.md),
            AppButton(
              key: const Key('block-submit'),
              label: AppStrings.blockTimeSubmit,
              loading: _submitting,
              onPressed: _canSubmit ? _submit : null,
            ),
            const SizedBox(height: AppSpacing.sm),
          ],
        ),
      ),
    );
  }
}
