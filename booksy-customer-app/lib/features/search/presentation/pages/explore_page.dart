import 'dart:async';

import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../home/domain/entities/provider_summary.dart';
import '../bloc/search_bloc.dart';

/// Explore: debounced search-as-you-type over providers with category
/// filter chips. Stale in-flight results never overwrite newer ones
/// (guarded in SearchBloc).
class ExplorePage extends StatefulWidget {
  const ExplorePage({super.key});

  @override
  State<ExplorePage> createState() => _ExplorePageState();
}

class _ExplorePageState extends State<ExplorePage> {
  static const _debounce = Duration(milliseconds: 350);

  // Static category list until a categories-for-search endpoint exists.
  static const List<String> _categories = [
    'آرایشگاه مردانه',
    'سالن زیبایی',
    'ماساژ',
    'سلامت و اسپا',
    'ناخن',
    'مراقبت از پوست',
  ];

  late final SearchBloc _bloc;
  final _searchController = TextEditingController();
  Timer? _debounceTimer;
  bool _initializedFromRoute = false;

  @override
  void initState() {
    super.initState();
    _bloc = getIt<SearchBloc>();
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_initializedFromRoute) return;
    _initializedFromRoute = true;
    // Deep link support: /explore?category=<name> (from home chips).
    final category =
        GoRouterState.of(context).uri.queryParameters['category'];
    _bloc.add(SearchCategoryChanged(
      category != null && _categories.contains(category) ? category : null,
    ));
  }

  @override
  void dispose() {
    _debounceTimer?.cancel();
    _searchController.dispose();
    _bloc.close();
    super.dispose();
  }

  void _onQueryChanged(String value) {
    _debounceTimer?.cancel();
    _debounceTimer = Timer(_debounce, () {
      _bloc.add(SearchQueryChanged(value));
    });
  }

  Future<void> _onRefresh() {
    _bloc.add(const SearchRetried());
    return _bloc.stream
        .firstWhere((s) => s.status != SearchStatus.loading)
        .then((_) {});
  }

  @override
  Widget build(BuildContext context) {
    return BlocProvider.value(
      value: _bloc,
      child: Scaffold(
        body: SafeArea(
          child: Column(
            children: [
              Padding(
                padding: const EdgeInsets.fromLTRB(
                  AppSpacing.md,
                  AppSpacing.md,
                  AppSpacing.md,
                  AppSpacing.sm,
                ),
                child: AppTextField(
                  controller: _searchController,
                  hint: AppStrings.searchPlaceholder,
                  prefixIcon: Icons.search,
                  keyboardType: TextInputType.text,
                  onChanged: _onQueryChanged,
                ),
              ),
              SizedBox(
                height: 44,
                child: BlocBuilder<SearchBloc, SearchState>(
                  buildWhen: (prev, next) => prev.category != next.category,
                  builder: (context, state) {
                    return ListView.separated(
                      scrollDirection: Axis.horizontal,
                      padding: const EdgeInsets.symmetric(
                        horizontal: AppSpacing.md,
                      ),
                      itemCount: _categories.length + 1,
                      separatorBuilder: (_, __) =>
                          const SizedBox(width: AppSpacing.xs),
                      itemBuilder: (context, index) {
                        if (index == 0) {
                          return ChoiceChip(
                            label: const Text(AppStrings.allCategories),
                            selected: state.category == null,
                            onSelected: (_) => _bloc
                                .add(const SearchCategoryChanged(null)),
                          );
                        }
                        final category = _categories[index - 1];
                        return ChoiceChip(
                          label: Text(category),
                          selected: state.category == category,
                          onSelected: (_) =>
                              _bloc.add(SearchCategoryChanged(category)),
                        );
                      },
                    );
                  },
                ),
              ),
              const SizedBox(height: AppSpacing.xs),
              Expanded(
                child: BlocBuilder<SearchBloc, SearchState>(
                  builder: (context, state) {
                    return StateSwitcher(
                      status: switch (state.status) {
                        SearchStatus.loading => ViewStatus.loading,
                        SearchStatus.loaded => ViewStatus.content,
                        SearchStatus.empty => ViewStatus.empty,
                        SearchStatus.error => ViewStatus.error,
                      },
                      skeleton: Padding(
                        padding: const EdgeInsets.symmetric(
                          horizontal: AppSpacing.md,
                        ),
                        child: SkeletonLoader.list(items: 4, itemHeight: 104),
                      ),
                      errorMessage: state.errorMessage,
                      onRetry: () => _bloc.add(const SearchRetried()),
                      empty: EmptyState(
                        icon: Icons.search_off,
                        title: AppStrings.noResultsTitle,
                        subtitle: AppStrings.noResultsSubtitle,
                        ctaLabel: AppStrings.clearFilters,
                        onCta: () {
                          _searchController.clear();
                          _bloc.add(const SearchCleared());
                        },
                      ),
                      contentBuilder: (context) => RefreshIndicator(
                        onRefresh: _onRefresh,
                        child: ListView.separated(
                          physics: const AlwaysScrollableScrollPhysics(),
                          padding: const EdgeInsets.fromLTRB(
                            AppSpacing.md,
                            AppSpacing.xs,
                            AppSpacing.md,
                            AppSpacing.md,
                          ),
                          itemCount: state.results.length,
                          separatorBuilder: (_, __) =>
                              const SizedBox(height: AppSpacing.sm),
                          itemBuilder: (context, index) =>
                              _ResultCard(provider: state.results[index]),
                        ),
                      ),
                    );
                  },
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _ResultCard extends StatelessWidget {
  final ProviderSummary provider;

  const _ResultCard({required this.provider});

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
