import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/persian_digits.dart';
import '../../../../core/utils/phone_number.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../../domain/entities/saved_customer.dart';

/// Add or edit one customer of the salon's book. Returns the draft, or null
/// when cancelled. First name and mobile are required (red * and the inline
/// "required" line under the field — the app-wide required-field rule).
Future<CustomerDraft?> showCustomerForm(
  BuildContext context, {
  CustomerDraft? initial,
  String title = AppStrings.customerAdd,
  bool askPhone = true,
  String? hint,
}) {
  return showDialog<CustomerDraft>(
    context: context,
    builder: (_) => CustomerFormDialog(
      initial: initial,
      title: title,
      askPhone: askPhone,
      hint: hint,
    ),
  );
}

class CustomerFormDialog extends StatefulWidget {
  final CustomerDraft? initial;
  final String title;

  /// False when the form is only about a name (a person naming themselves).
  final bool askPhone;
  final String? hint;

  const CustomerFormDialog({
    super.key,
    this.initial,
    this.title = AppStrings.customerAdd,
    this.askPhone = true,
    this.hint,
  });

  @override
  State<CustomerFormDialog> createState() => _CustomerFormDialogState();
}

class _CustomerFormDialogState extends State<CustomerFormDialog> {
  late final _firstName =
      TextEditingController(text: widget.initial?.firstName ?? '');
  late final _lastName =
      TextEditingController(text: widget.initial?.lastName ?? '');
  late final _phone = TextEditingController(
      text: widget.initial == null
          ? ''
          : PhoneNumber.normalize(widget.initial!.phone));
  late final _notes = TextEditingController(text: widget.initial?.notes ?? '');

  final Set<String> _touched = {};
  bool _submitted = false;

  @override
  void dispose() {
    _firstName.dispose();
    _lastName.dispose();
    _phone.dispose();
    _notes.dispose();
    super.dispose();
  }

  String? _validate(String field) => switch (field) {
        'firstName' =>
          _firstName.text.trim().isEmpty ? AppStrings.fieldRequired : null,
        'phone' when !widget.askPhone => null,
        'phone' => _phone.text.trim().isEmpty
            ? AppStrings.fieldRequired
            : PhoneNumber.isValid(_phone.text)
                ? null
                : AppStrings.customerPhoneInvalid,
        _ => null,
      };

  String? _errorFor(String field) =>
      (_submitted || _touched.contains(field)) ? _validate(field) : null;

  void _submit() {
    if (['firstName', 'phone'].any((f) => _validate(f) != null)) {
      setState(() => _submitted = true);
      return;
    }
    Navigator.of(context).pop(CustomerDraft(
      firstName: _firstName.text.trim(),
      lastName: _lastName.text.trim(),
      phone: widget.askPhone ? PhoneNumber.normalize(_phone.text) : '',
      notes: _notes.text.trim().isEmpty ? null : _notes.text.trim(),
    ));
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text(widget.title),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (widget.hint != null) ...[
              Text(widget.hint!,
                  style: const TextStyle(fontSize: 13, color: AppColors.muted)),
              const SizedBox(height: AppSpacing.sm),
            ],
            AppTextField(
              key: const Key('customer-first-name'),
              controller: _firstName,
              label: AppStrings.customerFirstName,
              isRequired: true,
              errorText: _errorFor('firstName'),
              onBlur: () => setState(() => _touched.add('firstName')),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: AppSpacing.sm),
            AppTextField(
              key: const Key('customer-last-name'),
              controller: _lastName,
              label: AppStrings.customerLastName,
            ),
            if (widget.askPhone) ...[
            const SizedBox(height: AppSpacing.sm),
            AppTextField(
              key: const Key('customer-phone'),
              controller: _phone,
              label: AppStrings.customerPhone,
              hint: '09123456789',
              isRequired: true,
              keyboardType: TextInputType.phone,
              contentDirection: TextDirection.ltr,
              inputFormatters: const [DigitsOnlyInputFormatter()],
              maxLength: 11,
              errorText: _errorFor('phone'),
              onBlur: () => setState(() => _touched.add('phone')),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: AppSpacing.sm),
            AppTextField(
              key: const Key('customer-notes'),
              controller: _notes,
              label: AppStrings.customerNotes,
              maxLines: 2,
            ),
            ],
          ],
        ),
      ),
      // One row: this app's buttons are full-width by theme.
      actions: [
        Row(
          children: [
            Expanded(
              child: OutlinedButton(
                onPressed: () => Navigator.of(context).pop(),
                child: const Text(AppStrings.cancel),
              ),
            ),
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: FilledButton(
                key: const Key('customer-save'),
                onPressed: _submit,
                child: const Text(AppStrings.save),
              ),
            ),
          ],
        ),
      ],
    );
  }
}
