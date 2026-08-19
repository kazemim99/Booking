import 'package:flutter/material.dart';

import '../../../../config/theme/app_text_styles.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/jalali_formatter.dart';
import '../../../booking/domain/entities/booking_entities.dart';

/// "خدمات": the provider's services laid out in a compact two-column grid of
/// name + price, so a salon with a dozen services reads at a glance instead of
/// becoming a screen-long column.
///
/// It falls back to a single column on narrow screens and at large font scales,
/// where two columns would clip the service names.
class ServicesGrid extends StatelessWidget {
  final List<ServiceItem> services;

  const ServicesGrid({super.key, required this.services});

  /// Below this content width, two columns stop fitting a service name.
  static const double _twoColumnMinWidth = 320;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (services.isEmpty) {
      return Text(
        AppStrings.noServicesYet,
        key: const Key('provider-services-empty'),
        style: theme.textTheme.bodyMedium,
      );
    }

    return LayoutBuilder(
      builder: (context, constraints) {
        final scale = MediaQuery.textScalerOf(context).scale(14) / 14;
        final twoColumns =
            constraints.maxWidth >= _twoColumnMinWidth && scale <= 1.2;
        final cellWidth = twoColumns
            ? (constraints.maxWidth - AppSpacing.sm) / 2
            : constraints.maxWidth;

        return Wrap(
          spacing: AppSpacing.sm,
          runSpacing: AppSpacing.sm,
          children: [
            for (final service in services)
              SizedBox(
                width: cellWidth,
                child: _ServiceCell(service: service),
              ),
          ],
        );
      },
    );
  }
}

class _ServiceCell extends StatelessWidget {
  final ServiceItem service;

  const _ServiceCell({required this.service});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final hasPrice = service.price > 0;
    final price = JalaliFormatter.toPersianDigits(
      '${service.price.toStringAsFixed(0)} ${service.currency}'.trim(),
    );
    final duration = JalaliFormatter.toPersianDigits(
      '${service.durationMinutes} دقیقه',
    );

    return Semantics(
      label: hasPrice ? '${service.name}، $price' : service.name,
      container: true,
      child: Container(
        padding: const EdgeInsets.all(AppSpacing.sm),
        decoration: BoxDecoration(
          color: theme.colorScheme.surfaceContainerHighest,
          borderRadius: BorderRadius.circular(AppRadius.md),
          border: Border.all(color: theme.dividerColor),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              service.name,
              style: theme.textTheme.titleSmall,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
            ),
            const SizedBox(height: AppSpacing.xxs),
            Row(
              children: [
                // A zero price means "not priced yet", not "free".
                if (hasPrice)
                  Expanded(
                    child: Text(
                      price,
                      style: theme.textTheme.bodyMedium?.copyWith(
                        color: theme.colorScheme.primary,
                        fontWeight: AppTextStyles.semibold,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  )
                else
                  const Spacer(),
                if (service.durationMinutes > 0)
                  Text(duration, style: theme.textTheme.bodySmall),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
