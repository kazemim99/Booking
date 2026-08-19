import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/widgets.dart';
import '../bloc/search_bloc.dart';
import '../widgets/provider_result_card.dart';
import '../widgets/service_categories.dart';

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

  /// Category chips come from [kServiceCategories] — the same list the home
  /// tile row uses, so a home tile can never deep-link to a category these
  /// chips would reject, and the Persian label/API-value separation lives in
  /// exactly one place.
  static const _categories = kServiceCategories;

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
    // Deep link support: /explore?category=<name> (from home chips). The parameter carries the API value, and
    // is accepted only if it is one this screen actually offers — an unknown value shows everything rather
    // than sending the API something it will reject.
    final category =
        GoRouterState.of(context).uri.queryParameters['category'];
    final known = _categories.any((c) => c.apiValue == category);
    _bloc.add(SearchCategoryChanged(known ? category : null));
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
              // A single location-based entry point. There were two chips — «اطراف من» and
              // «جستجو در محله» — leading to two separate list screens. Both are now the one map
              // page: its floating button covers "near me" and its search field covers area/city
              // lookup, so a second chip would open the same screen under another name.
              Align(
                alignment: AlignmentDirectional.centerStart,
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
                  child: ActionChip(
                    avatar: const Icon(Icons.near_me_outlined, size: 18),
                    label: const Text(AppStrings.nearMe),
                    onPressed: () => context.push(Routes.exploreMap),
                  ),
                ),
              ),
              const SizedBox(height: AppSpacing.sm),
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
                          // Persian for the customer, enum name for the API.
                          label: Text(category.label),
                          selected: state.category == category.apiValue,
                          onSelected: (_) => _bloc
                              .add(SearchCategoryChanged(category.apiValue)),
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
                          itemBuilder: (context, index) => ProviderResultCard(
                            provider: state.results[index],
                          ),
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

