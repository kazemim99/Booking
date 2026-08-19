import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../search/presentation/widgets/service_categories.dart';

/// Horizontal row of category tiles: a rounded square holding a line icon with
/// the Persian label underneath, ending in a "more" tile.
///
/// Tapping a tile deep-links to explore filtered by that category
/// (`/explore?category=<ServiceCategory>`); the "more" tile opens explore
/// unfiltered so every remaining category is one tap away. The categories come
/// from [kServiceCategories], the same list explore filters with, so a tile can
/// never point at a filter explore would reject.
class HomeCategoryRow extends StatelessWidget {
  const HomeCategoryRow({super.key});

  static const double _tileSize = 60;
  static const double _itemWidth = 76;

  @override
  Widget build(BuildContext context) {
    final categories = kServiceCategories.take(kHomeCategoryTileCount).toList();

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
      child: Row(
        children: [
          for (final category in categories) ...[
            _CategoryTile(
              key: Key('home-category-${category.apiValue}'),
              icon: category.icon,
              label: category.label,
              onTap: () => context.go(
                '${Routes.explore}?category='
                '${Uri.encodeComponent(category.apiValue)}',
              ),
            ),
            const SizedBox(width: AppSpacing.sm),
          ],
          _CategoryTile(
            key: const Key('home-category-more'),
            icon: Icons.more_horiz,
            label: AppStrings.categoryMore,
            onTap: () => context.go(Routes.explore),
          ),
        ],
      ),
    );
  }
}

class _CategoryTile extends StatelessWidget {
  final IconData icon;
  final String label;
  final VoidCallback onTap;

  const _CategoryTile({
    super.key,
    required this.icon,
    required this.label,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Semantics(
      button: true,
      label: label,
      child: SizedBox(
        width: HomeCategoryRow._itemWidth,
        child: InkWell(
          borderRadius: BorderRadius.circular(AppRadius.md),
          onTap: onTap,
          child: Column(
            children: [
              Container(
                width: HomeCategoryRow._tileSize,
                height: HomeCategoryRow._tileSize,
                decoration: BoxDecoration(
                  color: theme.colorScheme.surfaceContainerHighest,
                  borderRadius: BorderRadius.circular(AppRadius.lg),
                  border: Border.all(color: theme.dividerColor),
                ),
                child: Icon(
                  icon,
                  size: AppIconSize.md,
                  color: theme.colorScheme.primary,
                ),
              ),
              const SizedBox(height: AppSpacing.xxs),
              Text(
                label,
                style: theme.textTheme.bodySmall,
                textAlign: TextAlign.center,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
