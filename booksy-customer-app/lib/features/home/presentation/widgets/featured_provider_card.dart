import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/widgets.dart';
import '../../domain/entities/provider_summary.dart';

/// Featured provider card for the horizontal home rail: cover image on top,
/// business name, the one-line meta summary, and a filled "رزرو" CTA.
///
/// The meta line self-censors: rating is hidden while the provider is unrated,
/// the price band is hidden because `startingPrice` is not populated by the
/// search payload, and distance is hidden unless the response carried one. The
/// card therefore degrades to image + name + CTA on today's data instead of
/// showing zeros.
class FeaturedProviderCard extends StatelessWidget {
  final ProviderSummary provider;

  /// Fixed rail width. Height is intrinsic so the card grows with font scale.
  static const double width = 208;
  static const double _imageHeight = 108;

  const FeaturedProviderCard({super.key, required this.provider});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final meta = ProviderMetaLine(
      rating: provider.rating,
      reviewCount: provider.reviewCount,
      priceBand: PriceBand.fromPrices([provider.startingPrice.toDouble()]),
      distanceKm: provider.distance,
    );

    return SizedBox(
      width: width,
      child: AppCard(
        padding: EdgeInsets.zero,
        semanticLabel: provider.name,
        onTap: () => context.push(Routes.providerDetail(provider.id)),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            ProviderImage(
              imageUrl: provider.imageUrl,
              width: double.infinity,
              height: _imageHeight,
              placeholderIconSize: AppIconSize.md,
            ),
            Padding(
              padding: const EdgeInsets.all(AppSpacing.sm),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
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
                  const SizedBox(height: AppSpacing.xs),
                  AppButton(
                    key: Key('home-featured-book-${provider.id}'),
                    label: AppStrings.bookNowShort,
                    onPressed: () =>
                        context.push(Routes.bookingFlow(provider.id)),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
