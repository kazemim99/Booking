import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../home/domain/entities/provider_summary.dart';

/// Provider result row used across discovery surfaces (explore search, nearby,
/// area). Shows image, name, rating, and distance (when the response carries
/// it); taps through to the provider detail route.
class ProviderResultCard extends StatelessWidget {
  final ProviderSummary provider;

  const ProviderResultCard({super.key, required this.provider});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return AppCard(
      padding: EdgeInsets.zero,
      semanticLabel: provider.name,
      onTap: () => context.push(Routes.providerDetail(provider.id)),
      child: Row(
        children: [
          SizedBox(
            width: 96,
            height: 96,
            child: provider.imageUrl != null
                ? CachedNetworkImage(
                    imageUrl: provider.imageUrl!,
                    fit: BoxFit.cover,
                    placeholder: (_, __) => SkeletonLoader(
                      child: SkeletonLoader.box(height: 96, radius: 0),
                    ),
                    errorWidget: (_, __, ___) => _placeholder(theme),
                  )
                : _placeholder(theme),
          ),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.all(AppSpacing.sm),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    provider.name,
                    style: theme.textTheme.titleSmall,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  const SizedBox(height: AppSpacing.xxs),
                  Row(
                    children: [
                      const Icon(Icons.star, size: 14, color: Colors.amber),
                      const SizedBox(width: AppSpacing.xxs),
                      Text(
                        provider.rating.toStringAsFixed(1),
                        style: theme.textTheme.bodySmall,
                      ),
                      const SizedBox(width: AppSpacing.xxs),
                      Text(
                        '(${provider.reviewCount})',
                        style: theme.textTheme.bodySmall,
                      ),
                    ],
                  ),
                  if (provider.distance != null) ...[
                    const SizedBox(height: AppSpacing.xxs),
                    Row(
                      children: [
                        Icon(
                          Icons.location_on_outlined,
                          size: 14,
                          color: theme.colorScheme.onSurfaceVariant,
                        ),
                        const SizedBox(width: AppSpacing.xxs),
                        Text(
                          '${provider.distance!.toStringAsFixed(1)} کیلومتر',
                          style: theme.textTheme.bodySmall,
                        ),
                      ],
                    ),
                  ],
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _placeholder(ThemeData theme) => Container(
        color: theme.colorScheme.surfaceContainerHighest,
        child: Icon(
          Icons.storefront_outlined,
          size: 28,
          color: theme.colorScheme.onSurfaceVariant,
        ),
      );
}
