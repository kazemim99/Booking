import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/widgets.dart';
import '../../domain/entities/provider_summary.dart';

/// Wide provider card for the vertically stacked "nearest" list: square
/// thumbnail on the leading side, then business name, category subtitle, the
/// rating / price-band / distance meta line, the address, and an outlined
/// "مشاهده پروفایل" CTA.
///
/// [category] and [addressLine] are accepted but **not currently supplied**:
/// the provider-search payload carries `type` (the `ServiceCategory` enum name)
/// and `city`, yet `ProviderSummary` has no field for either, so the mapper
/// drops them. Both rows stay hidden until that entity gains the fields —
/// nothing is guessed in the meantime.
class NearbyProviderCard extends StatelessWidget {
  final ProviderSummary provider;
  final String? category;
  final String? addressLine;

  const NearbyProviderCard({
    super.key,
    required this.provider,
    this.category,
    this.addressLine,
  });

  static const double _thumbSize = 96;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final meta = ProviderMetaLine(
      rating: provider.rating,
      reviewCount: provider.reviewCount,
      priceBand: PriceBand.fromPrices([provider.startingPrice.toDouble()]),
      distanceKm: provider.distance,
    );

    return AppCard(
      padding: const EdgeInsets.all(AppSpacing.sm),
      semanticLabel: provider.name,
      onTap: () => context.push(Routes.providerDetail(provider.id)),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          ClipRRect(
            borderRadius: BorderRadius.circular(AppRadius.md),
            child: ProviderImage(
              imageUrl: provider.imageUrl,
              width: _thumbSize,
              height: _thumbSize,
            ),
          ),
          const SizedBox(width: AppSpacing.sm),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  provider.name,
                  style: theme.textTheme.titleMedium,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                if (category != null && category!.isNotEmpty)
                  Text(
                    category!,
                    style: theme.textTheme.bodySmall,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                if (meta.hasContent) ...[
                  const SizedBox(height: AppSpacing.xxs),
                  meta,
                ],
                if (addressLine != null && addressLine!.isNotEmpty) ...[
                  const SizedBox(height: AppSpacing.xxs),
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Icon(
                        Icons.place_outlined,
                        size: AppIconSize.sm,
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                      const SizedBox(width: AppSpacing.xxs),
                      Expanded(
                        child: Text(
                          addressLine!,
                          style: theme.textTheme.bodySmall,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ),
                ],
                const SizedBox(height: AppSpacing.xs),
                AppButton.secondary(
                  key: Key('home-nearby-profile-${provider.id}'),
                  label: AppStrings.viewProfile,
                  onPressed: () =>
                      context.push(Routes.providerDetail(provider.id)),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
