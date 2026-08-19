import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import 'service_categories.dart';

/// The horizontal category tile row, in a *selectable* form.
///
/// Visually the same row the home screen shows (rounded tile + line glyph +
/// Persian label), but home's [HomeCategoryRow] navigates on tap, whereas this
/// one filters in place. Both read [kServiceCategories], so the Persian labels
/// and the `ServiceCategory` enum values they map to are defined once and the
/// two rows can never drift apart.
class CategoryFilterRow extends StatelessWidget {
  /// Selected category as the API enum name; `null` means "all".
  final String? selected;
  final ValueChanged<String?> onSelected;

  /// Prefix for the widget keys (`<prefix>-category-<apiValue>`), so more than
  /// one row can coexist in a test.
  final String keyPrefix;

  const CategoryFilterRow({
    super.key,
    required this.selected,
    required this.onSelected,
    this.keyPrefix = 'map',
  });

  static const double _tileSize = 52;
  static const double _itemWidth = 68;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _CategoryTile(
            key: Key('$keyPrefix-category-all'),
            icon: Icons.grid_view_outlined,
            label: AppStrings.allCategories,
            selected: selected == null,
            onTap: () => onSelected(null),
          ),
          for (final category in kServiceCategories) ...[
            const SizedBox(width: AppSpacing.sm),
            _CategoryTile(
              key: Key('$keyPrefix-category-${category.apiValue}'),
              icon: category.icon,
              label: category.label,
              selected: selected == category.apiValue,
              // Re-tapping the active tile clears the filter, so there is
              // always a way back to "all" without hunting for it.
              onTap: () => onSelected(
                selected == category.apiValue ? null : category.apiValue,
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _CategoryTile extends StatelessWidget {
  final IconData icon;
  final String label;
  final bool selected;
  final VoidCallback onTap;

  const _CategoryTile({
    super.key,
    required this.icon,
    required this.label,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final background = selected
        ? theme.colorScheme.primary
        : theme.colorScheme.surfaceContainerHighest;
    final foreground =
        selected ? theme.colorScheme.onPrimary : theme.colorScheme.primary;

    return Semantics(
      button: true,
      selected: selected,
      label: label,
      child: SizedBox(
        width: CategoryFilterRow._itemWidth,
        child: InkWell(
          borderRadius: BorderRadius.circular(AppRadius.md),
          onTap: onTap,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: CategoryFilterRow._tileSize,
                height: CategoryFilterRow._tileSize,
                decoration: BoxDecoration(
                  color: background,
                  borderRadius: BorderRadius.circular(AppRadius.lg),
                  border: Border.all(
                    color: selected
                        ? theme.colorScheme.primary
                        : theme.dividerColor,
                  ),
                ),
                child: Icon(icon, size: AppIconSize.md, color: foreground),
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
