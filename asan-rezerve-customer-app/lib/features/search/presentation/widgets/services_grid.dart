import 'package:flutter/material.dart';

import '../../../../config/theme/app_colors.dart';
import '../../../../config/theme/app_text_styles.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/forward_chevron.dart';
import '../../../../core/utils/jalali_formatter.dart';
import '../../../booking/domain/entities/booking_entities.dart';
import '../../../booking/domain/entities/promotion_entities.dart';
import '../../../../core/utils/price_formatter.dart';

/// "خدمات": the provider's services laid out in a compact two-column grid of
/// name + price, so a salon with a dozen services reads at a glance instead of
/// becoming a screen-long column.
///
/// Each cell is a button: tapping a service starts booking it
/// ([onServiceTap]), so the cards no longer look tappable while doing nothing
/// (UX review #3). A price is never cut off — it has a line of its own and
/// wraps rather than losing its currency to an ellipsis (#15).
///
/// It falls back to a single column on narrow screens and at large font scales,
/// where two columns would clip the service names.
class ServicesGrid extends StatelessWidget {
  final List<ServiceItem> services;

  /// Called with the tapped service; the cells are inert without it.
  final ValueChanged<ServiceItem>? onServiceTap;

  /// The salon's automatic offers; a service they reach shows a badge, and its discounted price when certain.
  final List<PublicOffer> offers;

  const ServicesGrid({super.key, required this.services, this.onServiceTap, this.offers = const []});

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
                child: _ServiceCell(
                  service: service,
                  offer: offers.isEmpty ? null : offerForService(service.id, service.price, offers),
                  onTap: onServiceTap == null
                      ? null
                      : () => onServiceTap!(service),
                ),
              ),
          ],
        );
      },
    );
  }
}

class _ServiceCell extends StatelessWidget {
  final ServiceItem service;
  final ServiceOffer? offer;
  final VoidCallback? onTap;

  const _ServiceCell({required this.service, this.offer, this.onTap});

  /// The smallest comfortable touch target.
  static const double _minTouchTarget = 48;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final hasPrice = service.price > 0;
    final hasDuration = service.durationMinutes > 0;
    final price = JalaliFormatter.toPersianDigits(
      PriceFormatter.format(service.price.round()),
    );
    final duration = JalaliFormatter.toPersianDigits(
      AppStrings.serviceDurationMinutes('${service.durationMinutes}'),
    );
    final radius = BorderRadius.circular(AppRadius.md);

    final discounted = offer?.discountedPrice;
    final label = [
      service.name,
      if (offer != null) offer!.offer.badge,
      if (hasPrice && discounted != null)
        JalaliFormatter.toPersianDigits(PriceFormatter.format(discounted))
      else if (hasPrice)
        price,
      if (hasDuration) duration,
      if (onTap != null) AppStrings.serviceBookAction,
    ].join('، ');

    return Semantics(
      key: Key('provider-service-${service.id}'),
      label: label,
      button: onTap != null,
      container: true,
      excludeSemantics: true,
      onTap: onTap,
      child: Material(
        color: theme.colorScheme.surfaceContainerHighest,
        shape: RoundedRectangleBorder(
          borderRadius: radius,
          side: BorderSide(color: theme.dividerColor),
        ),
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: onTap,
          borderRadius: radius,
          child: ConstrainedBox(
            constraints: const BoxConstraints(minHeight: _minTouchTarget),
            child: Padding(
              padding: const EdgeInsetsDirectional.all(AppSpacing.sm),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    service.name,
                    style: theme.textTheme.titleSmall,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                  if (offer != null) ...[
                    const SizedBox(height: AppSpacing.xxs),
                    Text(
                      offer!.offer.condition == null
                          ? offer!.offer.badge
                          : '${offer!.offer.badge} · ${offer!.offer.condition}',
                      key: Key('service-offer-${service.id}'),
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: AppColors.success,
                        fontWeight: AppTextStyles.semibold,
                      ),
                    ),
                  ],
                  // A zero price means "not priced yet", not "free".
                  if (hasPrice) ...[
                    const SizedBox(height: AppSpacing.xxs),
                    // Money never truncates: its own line, free to wrap. A discounted price is shown only when
                    // nothing about the day, time or customer can change it; the list price is then struck through.
                    if (discounted != null)
                      Text(
                        price,
                        key: Key('service-price-was-${service.id}'),
                        style: theme.textTheme.bodySmall?.copyWith(
                          decoration: TextDecoration.lineThrough,
                          color: theme.colorScheme.onSurfaceVariant,
                        ),
                      ),
                    Text(
                      discounted == null
                          ? price
                          : JalaliFormatter.toPersianDigits(PriceFormatter.format(discounted)),
                      style: theme.textTheme.bodyMedium?.copyWith(
                        color: discounted == null ? theme.colorScheme.primary : AppColors.success,
                        fontWeight: AppTextStyles.semibold,
                      ),
                    ),
                  ],
                  const SizedBox(height: AppSpacing.xxs),
                  Row(
                    children: [
                      Expanded(
                        child: hasDuration
                            ? Text(duration, style: theme.textTheme.bodySmall)
                            : const SizedBox.shrink(),
                      ),
                      if (onTap != null) ...[
                        Flexible(
                          child: Text(
                            AppStrings.serviceBookAction,
                            style: theme.textTheme.labelMedium?.copyWith(
                              color: theme.colorScheme.primary,
                              fontWeight: AppTextStyles.semibold,
                            ),
                            textAlign: TextAlign.end,
                          ),
                        ),
                        ForwardChevron(
                          size: 18,
                          color: theme.colorScheme.primary,
                        ),
                      ],
                    ],
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
