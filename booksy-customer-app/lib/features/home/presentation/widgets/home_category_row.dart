import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../search/presentation/widgets/service_categories.dart';
import '../../domain/entities/category.dart';

/// Horizontal row of category tiles: a rounded square holding a line icon with
/// the Persian label underneath, ending in a "more" tile.
///
/// Tapping a tile deep-links to explore filtered by that category
/// (`/explore?category=<ServiceCategory>`); the "more" tile opens explore
/// unfiltered so every remaining category is one tap away. The categories come
/// from [kServiceCategories], the same list explore filters with, so a tile can
/// never point at a filter explore would reject.
///
/// [available] is what the catalogue reports, with its provider counts: a tile
/// for a category no salon offers only leads to an empty page, so those are
/// left out. When the catalogue says nothing yet — still loading, or the call
/// failed — the full row is shown, because an empty strip reads as "this app
/// has no categories".
class HomeCategoryRow extends StatelessWidget {
  final List<Category> available;

  const HomeCategoryRow({super.key, this.available = const []});

  /// The catalogue's slug for a `ServiceCategory` name: `NailSalon` -> `nail-salon`.
  static String slugOf(String apiValue) {
    final buffer = StringBuffer();
    for (var i = 0; i < apiValue.length; i++) {
      final char = apiValue[i];
      final isUpper = char.toUpperCase() == char && char.toLowerCase() != char;
      if (isUpper && i > 0) buffer.write('-');
      buffer.write(char.toLowerCase());
    }
    return buffer.toString();
  }

  static const double _tileSize = 60;
  static const double _itemWidth = 76;

  @override
  Widget build(BuildContext context) {
    final offered = available
        .where((c) => c.providerCount > 0)
        .map((c) => c.id.toLowerCase())
        .toSet();
    final bookable = offered.isEmpty
        ? kServiceCategories
        : kServiceCategories
            .where((c) => offered.contains(slugOf(c.apiValue)))
            .toList();
    final categories = (bookable.isEmpty ? kServiceCategories : bookable)
        .take(kHomeCategoryTileCount)
        .toList();

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
