import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/persian_text.dart';
import '../../../../core/utils/phone_number.dart';
import '../../../../core/widgets/app_empty_state.dart';
import '../../../../core/widgets/app_loading.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../../domain/entities/saved_customer.dart';

/// Picks one customer from the salon's book (the composer's "choose from my
/// customers"). Null when dismissed.
Future<SavedCustomer?> showCustomerPicker(
  BuildContext context,
  Future<List<SavedCustomer>> Function() load,
) {
  return showModalBottomSheet<SavedCustomer>(
    context: context,
    isScrollControlled: true,
    shape: const RoundedRectangleBorder(
      borderRadius:
          BorderRadius.vertical(top: Radius.circular(AppRadius.bottomSheet)),
    ),
    builder: (_) => FractionallySizedBox(
      heightFactor: 0.8,
      child: CustomerPickerSheet(load: load),
    ),
  );
}

class CustomerPickerSheet extends StatefulWidget {
  final Future<List<SavedCustomer>> Function() load;

  const CustomerPickerSheet({super.key, required this.load});

  @override
  State<CustomerPickerSheet> createState() => _CustomerPickerSheetState();
}

class _CustomerPickerSheetState extends State<CustomerPickerSheet> {
  List<SavedCustomer>? _customers;
  String _query = '';

  @override
  void initState() {
    super.initState();
    widget.load().then((c) {
      if (mounted) setState(() => _customers = c);
    });
  }

  List<SavedCustomer> get _filtered {
    final all = _customers ?? const <SavedCustomer>[];
    if (_query.trim().isEmpty) return all;
    return all
        .where((c) =>
            PersianText.contains(c.fullName, _query) ||
            PersianText.contains(PhoneNumber.normalize(c.phone), _query))
        .toList();
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              AppStrings.customerPickerTitle,
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w700,
                color: AppColors.ink,
              ),
            ),
            const SizedBox(height: AppSpacing.sm),
            AppTextField(
              key: const Key('customer-picker-search'),
              hint: AppStrings.clientsSearchHint,
              prefixIcon: Icons.search,
              onChanged: (q) => setState(() => _query = q),
            ),
            const SizedBox(height: AppSpacing.sm),
            Expanded(child: _body()),
          ],
        ),
      ),
    );
  }

  Widget _body() {
    if (_customers == null) return const AppLoading.page();
    if (_customers!.isEmpty) {
      return const AppEmptyState(
        icon: Icons.people_outline,
        message: AppStrings.clientsEmptyTitle,
      );
    }
    final customers = _filtered;
    if (customers.isEmpty) {
      return const AppEmptyState(
        icon: Icons.search_off,
        message: AppStrings.clientsSearchEmpty,
      );
    }
    return ListView.separated(
      itemCount: customers.length,
      separatorBuilder: (_, _) =>
          const Divider(color: AppColors.divider, height: 1),
      itemBuilder: (context, i) {
        final c = customers[i];
        return ListTile(
          key: Key('customer-pick-${c.id}'),
          contentPadding: EdgeInsets.zero,
          title: Text(c.fullName),
          subtitle: Text(
            PhoneNumber.display(c.phone),
            textDirection: TextDirection.ltr,
            textAlign: TextAlign.right,
          ),
          onTap: () => Navigator.of(context).pop(c),
        );
      },
    );
  }
}
