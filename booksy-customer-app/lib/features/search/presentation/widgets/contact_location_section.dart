import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/jalali_formatter.dart';

/// "تماس و موقعیت": the phone row and the address row.
///
/// [phoneNumber] is currently always absent. The provider-details payload does
/// carry `contactInfo.primaryPhone`, but `_parseProvider` in the booking
/// repository does not map it and `ProviderDetail` has no field for it, so the
/// phone row simply does not render. Once the entity carries a phone, pass it
/// here and the row appears — nothing else changes, and no placeholder number
/// is ever shown in the meantime.
///
/// Renders nothing at all when neither a phone nor an address is known.
class ContactLocationSection extends StatelessWidget {
  final String? phoneNumber;
  final String? address;

  const ContactLocationSection({
    super.key,
    this.phoneNumber,
    this.address,
  });

  bool get hasContent =>
      (phoneNumber != null && phoneNumber!.isNotEmpty) ||
      (address != null && address!.isNotEmpty);

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    if (!hasContent) return const SizedBox.shrink();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          AppStrings.contactAndLocationTitle,
          style: theme.textTheme.titleLarge,
        ),
        const SizedBox(height: AppSpacing.xs),
        if (phoneNumber != null && phoneNumber!.isNotEmpty)
          _Row(
            key: const Key('provider-phone-row'),
            icon: Icons.phone_outlined,
            semanticLabel: AppStrings.providerPhoneLabel,
            // Persian digits: the number is read, not dialled from here.
            value: JalaliFormatter.toPersianDigits(phoneNumber!),
          ),
        if (address != null && address!.isNotEmpty)
          _Row(
            key: const Key('provider-address-row'),
            icon: Icons.place_outlined,
            semanticLabel: AppStrings.providerAddressLabel,
            value: address!,
          ),
      ],
    );
  }
}

class _Row extends StatelessWidget {
  final IconData icon;
  final String value;
  final String semanticLabel;

  const _Row({
    super.key,
    required this.icon,
    required this.value,
    required this.semanticLabel,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.xxs),
      child: Semantics(
        label: '$semanticLabel: $value',
        container: true,
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(
              icon,
              size: AppIconSize.action,
              color: theme.colorScheme.onSurfaceVariant,
            ),
            const SizedBox(width: AppSpacing.xs),
            Expanded(
              child: Text(value, style: theme.textTheme.bodyMedium),
            ),
          ],
        ),
      ),
    );
  }
}
