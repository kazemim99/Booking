import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/jalali_formatter.dart';
import 'provider_location_card.dart';

/// "تماس و موقعیت": the phone row, the address row, then the map with its
/// directions action.
///
/// One section for where the salon is (QA recording 2026-09-23 #7): the map used
/// to be a section of its own further down, «موقعیت روی نقشه», which repeated the
/// address above it. The map needs coordinates and the address row an address;
/// either is enough for the section to show.
///
/// [phoneNumber] is currently always absent. The provider-details payload does
/// carry `contactInfo.primaryPhone`, but `_parseProvider` in the booking
/// repository does not map it and `ProviderDetail` has no field for it, so the
/// phone row simply does not render. Once the entity carries a phone, pass it
/// here and the row appears — nothing else changes, and no placeholder number
/// is ever shown in the meantime.
///
/// Renders nothing at all when neither a phone, an address nor coordinates are
/// known.
class ContactLocationSection extends StatelessWidget {
  final String? phoneNumber;
  final String? address;

  /// The salon's name, for the map.
  final String businessName;
  final double? latitude;
  final double? longitude;

  /// Injected so a test can see which directions link was chosen.
  final Future<void> Function(String url)? openUrl;

  const ContactLocationSection({
    super.key,
    this.phoneNumber,
    this.address,
    this.businessName = '',
    this.latitude,
    this.longitude,
    this.openUrl,
  });

  bool get _hasPhone => phoneNumber != null && phoneNumber!.isNotEmpty;
  bool get _hasAddress => address != null && address!.isNotEmpty;
  bool get _hasPin => latitude != null && longitude != null;

  bool get hasContent => _hasPhone || _hasAddress || _hasPin;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    if (!hasContent) return const SizedBox.shrink();

    return Column(
      key: const Key('provider-contact-location'),
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          AppStrings.contactAndLocationTitle,
          style: theme.textTheme.titleLarge,
        ),
        const SizedBox(height: AppSpacing.xs),
        if (_hasPhone)
          _Row(
            key: const Key('provider-phone-row'),
            icon: Icons.phone_outlined,
            semanticLabel: AppStrings.providerPhoneLabel,
            // Persian digits: the number is read, not dialled from here.
            value: JalaliFormatter.toPersianDigits(phoneNumber!),
          ),
        if (_hasAddress)
          _Row(
            key: const Key('provider-address-row'),
            icon: Icons.place_outlined,
            semanticLabel: AppStrings.providerAddressLabel,
            value: address!,
          ),
        if (_hasPin) ...[
          const SizedBox(height: AppSpacing.sm),
          ProviderLocationCard(
            businessName: businessName,
            latitude: latitude,
            longitude: longitude,
            openUrl: openUrl,
          ),
        ],
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
