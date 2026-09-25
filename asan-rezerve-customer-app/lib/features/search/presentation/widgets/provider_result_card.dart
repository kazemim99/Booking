import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/widgets/forward_chevron.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../home/domain/entities/provider_summary.dart';

/// Provider result row on explore: image, name and the same [ProviderMetaLine]
/// every other provider card uses — rating (or «هنوز نظری ندارد»), the
/// starting price when known, free times when the availability summary sent
/// them, and distance when the response carried one. Taps through to the
/// provider detail route.
class ProviderResultCard extends StatelessWidget {
  final ProviderSummary provider;

  /// Today, for reading the free-slot day as «امروز»/«فردا». Injected by tests.
  final DateTime? now;

  const ProviderResultCard({super.key, required this.provider, this.now});

  static const double _imageSize = 96;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final meta = ProviderMetaLine(
      rating: provider.rating,
      reviewCount: provider.reviewCount,
      startingPrice: provider.startingPrice,
      distanceKm: provider.distance,
      nextFreeDate: provider.nextFreeDate,
      freeSlotCount: provider.freeSlotCount,
      now: now,
    );

    return AppCard(
      padding: EdgeInsets.zero,
      semanticLabel: provider.name,
      onTap: () => context.push(Routes.providerDetail(provider.id)),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          // One widget owns how a salon photo loads (it uses the browser's own
          // image loading on web, where the cache manager cannot work).
          ProviderImage(
            imageUrl: provider.imageUrl,
            width: _imageSize,
            height: _imageSize,
          ),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.all(AppSpacing.sm),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    provider.name,
                    style: theme.textTheme.titleSmall,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  if (meta.hasContent) ...[
                    const SizedBox(height: AppSpacing.xxs),
                    meta,
                  ],
                ],
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsetsDirectional.only(end: AppSpacing.xs),
            child: ForwardChevron(color: theme.colorScheme.onSurfaceVariant),
          ),
        ],
      ),
    );
  }
}
