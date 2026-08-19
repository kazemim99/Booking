import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../home/domain/entities/provider_summary.dart';

/// A provider card in the map's bottom carousel: cover image, business name,
/// the rating / price-band / distance meta line, and a "مشاهده پروفایل" button.
///
/// The rating and price band are rendered by [ProviderMetaLine], which drops
/// each part it has no real value for — seeded providers have a zero rating and
/// no priced services, and "★ ۰.۰" would read as a *bad* salon rather than an
/// unrated one. Distance, by contrast, is real: `/Providers/by-location`
/// computes it server-side.
class MapProviderCard extends StatelessWidget {
  final ProviderSummary provider;

  /// Whether this card is the one the map pin is highlighting.
  final bool selected;

  /// Tapping the card body (not the CTA) selects it on the map.
  final VoidCallback? onTap;

  const MapProviderCard({
    super.key,
    required this.provider,
    this.selected = false,
    this.onTap,
  });

  static const double _coverSize = 84;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final meta = ProviderMetaLine(
      rating: provider.rating,
      reviewCount: provider.reviewCount,
      priceBand: PriceBand.fromPrices([provider.startingPrice.toDouble()]),
      distanceKm: provider.distance,
    );

    return Semantics(
      selected: selected,
      child: Material(
        color: theme.colorScheme.surface,
        elevation: selected ? AppElevation.high : AppElevation.medium,
        borderRadius: BorderRadius.circular(AppRadius.card),
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: onTap,
          child: Container(
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(AppRadius.card),
              border: Border.all(
                color: selected
                    ? theme.colorScheme.primary
                    : theme.dividerColor,
                width: selected ? 2 : 1,
              ),
            ),
            padding: const EdgeInsets.all(AppSpacing.sm),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                ClipRRect(
                  borderRadius: BorderRadius.circular(AppRadius.md),
                  child: ProviderImage(
                    imageUrl: provider.imageUrl,
                    width: _coverSize,
                    height: _coverSize,
                  ),
                ),
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        provider.name,
                        style: theme.textTheme.titleMedium,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                      if (meta.hasContent) ...[
                        const SizedBox(height: AppSpacing.xxs),
                        meta,
                      ],
                      if (provider.addressLine != null &&
                          provider.addressLine!.isNotEmpty) ...[
                        const SizedBox(height: AppSpacing.xxs),
                        Text(
                          provider.addressLine!,
                          style: theme.textTheme.bodySmall,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ],
                      const SizedBox(height: AppSpacing.xs),
                      AppButton.secondary(
                        key: Key('map-card-profile-${provider.id}'),
                        label: AppStrings.viewProfile,
                        onPressed: () =>
                            context.push(Routes.providerDetail(provider.id)),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
