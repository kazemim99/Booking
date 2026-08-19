import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../search/presentation/bloc/nearby_providers_cubit.dart';
import '../../domain/entities/provider_summary.dart';
import '../../domain/usecases/get_home_data.dart' show HomeSection;
import '../bloc/home_bloc.dart';
import '../bloc/home_event.dart';
import '../bloc/home_state.dart';
import '../widgets/featured_provider_card.dart';
import '../widgets/home_category_row.dart';
import '../widgets/home_menu_drawer.dart';
import '../widgets/nearby_provider_card.dart';
import '../widgets/upcoming_booking_card.dart';

/// Home / discovery surface.
///
/// Top to bottom: app bar (menu, title, map-search) → search pill → category
/// tiles → next booking → featured rail → nearest providers → promotions →
/// recent & favourites. The four bottom-nav destinations come from the shell
/// (`AppShell`), not from here.
///
/// Two independent sources feed the two provider sections, so neither can
/// duplicate the other: the horizontal rail shows [HomeBloc]'s top providers
/// (available without any permission), while the vertical list shows genuinely
/// nearest providers from [NearbyProvidersCubit] and degrades to the manual
/// area search when location is denied or switched off.
class HomePage extends StatefulWidget {
  /// Nearest-providers cubit. Injected in tests; resolved from DI in the app.
  final NearbyProvidersCubit? nearbyCubit;

  const HomePage({super.key, this.nearbyCubit});

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  late final NearbyProvidersCubit _nearby;

  /// Only a self-resolved cubit is disposed here; an injected one belongs to
  /// the caller.
  late final bool _ownsNearby;

  @override
  void initState() {
    super.initState();
    final bloc = context.read<HomeBloc>();
    if (bloc.state is HomeInitial) {
      bloc.add(const LoadHomeData());
    }

    _ownsNearby = widget.nearbyCubit == null;
    _nearby = widget.nearbyCubit ?? getIt<NearbyProvidersCubit>();
    if (_nearby.state.status == NearbyStatus.initial) {
      _nearby.load();
    }
  }

  @override
  void dispose() {
    if (_ownsNearby) _nearby.close();
    super.dispose();
  }

  Future<void> _onRefresh() {
    final bloc = context.read<HomeBloc>();
    bloc.add(const RefreshHomeData());
    _nearby.load();
    return bloc.stream
        .firstWhere((state) => state is HomeLoaded || state is HomeError)
        .then((_) {});
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text(AppStrings.homeTitle),
        leading: Builder(
          builder: (context) => IconButton(
            key: const Key('home-menu-button'),
            icon: const Icon(Icons.menu),
            tooltip: AppStrings.homeMenu,
            onPressed: Scaffold.of(context).openDrawer,
          ),
        ),
        actions: [
          Padding(
            padding: const EdgeInsetsDirectional.only(end: AppSpacing.xs),
            child: AppCircleIconButton(
              key: const Key('home-map-search-button'),
              icon: Icons.travel_explore,
              semanticLabel: AppStrings.mapSearch,
              onPressed: () => context.go(Routes.exploreMap),
            ),
          ),
        ],
      ),
      drawer: const HomeMenuDrawer(),
      body: Column(
        children: [
          const _SearchPill(),
          Expanded(
            child: BlocBuilder<HomeBloc, HomeState>(
              builder: (context, state) {
                final status = switch (state) {
                  HomeInitial() || HomeLoading() => ViewStatus.loading,
                  HomeError() => ViewStatus.error,
                  _ => ViewStatus.content,
                };

                return StateSwitcher(
                  status: status,
                  errorMessage: state is HomeError ? state.message : null,
                  onRetry: () =>
                      context.read<HomeBloc>().add(const LoadHomeData()),
                  skeleton: const _HomeSkeleton(),
                  contentBuilder: (context) {
                    final loaded = state as HomeLoaded;
                    return RefreshIndicator(
                      onRefresh: _onRefresh,
                      child: ListView(
                        physics: const AlwaysScrollableScrollPhysics(),
                        padding: const EdgeInsets.only(bottom: AppSpacing.lg),
                        children: [
                          const SizedBox(height: AppSpacing.md),
                          const HomeCategoryRow(),
                          _Section(
                            section: HomeSection.upcomingBookings,
                            failed: loaded.failedSections,
                            isEmpty: loaded.upcomingBookings.isEmpty,
                            title: AppStrings.upcomingBookingTitle,
                            child: Padding(
                              padding: const EdgeInsets.symmetric(
                                horizontal: AppSpacing.md,
                              ),
                              child: loaded.upcomingBookings.isEmpty
                                  ? const SizedBox.shrink()
                                  : UpcomingBookingCard(
                                      booking: loaded.upcomingBookings.first,
                                    ),
                            ),
                          ),
                          _Section(
                            section: HomeSection.topProviders,
                            failed: loaded.failedSections,
                            isEmpty: loaded.topProviders.isEmpty,
                            title: AppStrings.topProvidersTitle,
                            child: _FeaturedRail(
                              providers: loaded.topProviders,
                            ),
                          ),
                          BlocProvider.value(
                            value: _nearby,
                            child: const _NearestSection(),
                          ),
                          _Section(
                            section: HomeSection.promotions,
                            failed: loaded.failedSections,
                            isEmpty: loaded.promotions.isEmpty,
                            title: AppStrings.promotionsTitle,
                            child: SizedBox(
                              height: 140,
                              child: ListView.separated(
                                scrollDirection: Axis.horizontal,
                                padding: const EdgeInsets.symmetric(
                                  horizontal: AppSpacing.md,
                                ),
                                itemCount: loaded.promotions.length,
                                separatorBuilder: (_, __) =>
                                    const SizedBox(width: AppSpacing.sm),
                                itemBuilder: (context, index) {
                                  final promo = loaded.promotions[index];
                                  return ClipRRect(
                                    borderRadius: BorderRadius.circular(
                                      AppRadius.lg,
                                    ),
                                    child: CachedNetworkImage(
                                      imageUrl: promo.imageUrl,
                                      width: 280,
                                      fit: BoxFit.cover,
                                      placeholder: (_, __) => SkeletonLoader(
                                        child: SkeletonLoader.box(
                                          width: 280,
                                          height: 140,
                                          radius: AppRadius.lg,
                                        ),
                                      ),
                                      errorWidget: (_, __, ___) =>
                                          const ProviderImage(
                                        imageUrl: null,
                                        width: 280,
                                        height: 140,
                                      ),
                                    ),
                                  );
                                },
                              ),
                            ),
                          ),
                          _Section(
                            section: HomeSection.recentAndFavorites,
                            failed: loaded.failedSections,
                            isEmpty: loaded.recentlyVisitedProviders.isEmpty &&
                                loaded.favoriteProviders.isEmpty,
                            title: AppStrings.recentAndFavoritesTitle,
                            child: SizedBox(
                              height: 120,
                              child: ListView(
                                scrollDirection: Axis.horizontal,
                                padding: const EdgeInsets.symmetric(
                                  horizontal: AppSpacing.md,
                                ),
                                children: [
                                  ...loaded.recentlyVisitedProviders.map(
                                    (p) => _MiniProviderCard(
                                      name: p.providerName,
                                      subtitle: p.city,
                                      rating: p.averageRating,
                                      badge: AppStrings.recentBadge,
                                    ),
                                  ),
                                  ...loaded.favoriteProviders.map(
                                    (p) => _MiniProviderCard(
                                      name: p.providerName,
                                      subtitle: p.city,
                                      rating: p.averageRating,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ],
                      ),
                    );
                  },
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

/// Full-width rounded search pill. Tapping it opens explore, where the real
/// debounced search field lives — one search implementation, not two.
class _SearchPill extends StatelessWidget {
  const _SearchPill();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.md,
        AppSpacing.sm,
        AppSpacing.md,
        AppSpacing.xxs,
      ),
      child: Semantics(
        button: true,
        label: AppStrings.homeSearchHint,
        child: Material(
          color: theme.colorScheme.surfaceContainerHighest,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(AppRadius.full),
            side: BorderSide(color: theme.dividerColor),
          ),
          clipBehavior: Clip.antiAlias,
          child: InkWell(
            key: const Key('home-search-pill'),
            onTap: () => context.go(Routes.explore),
            child: Padding(
              padding: const EdgeInsets.symmetric(
                horizontal: AppSpacing.md,
                vertical: AppSpacing.sm,
              ),
              child: Row(
                children: [
                  Icon(
                    Icons.search,
                    size: AppIconSize.action,
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  Expanded(
                    child: Text(
                      AppStrings.homeSearchHint,
                      style: theme.textTheme.bodyMedium,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// Horizontal rail of featured provider cards.
///
/// A horizontal [SingleChildScrollView] rather than a fixed-height list: the
/// cards then keep their intrinsic height, so nothing clips at 1.3× font scale.
class _FeaturedRail extends StatelessWidget {
  final List<ProviderSummary> providers;

  const _FeaturedRail({required this.providers});

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          for (var i = 0; i < providers.length; i++) ...[
            if (i > 0) const SizedBox(width: AppSpacing.sm),
            FeaturedProviderCard(provider: providers[i]),
          ],
        ],
      ),
    );
  }
}

/// "نزدیک‌ترین‌ها": vertically stacked wide cards for the providers actually
/// closest to the customer. Location is optional — every unavailable-location
/// outcome falls back to the manual area search instead of an empty section.
class _NearestSection extends StatelessWidget {
  const _NearestSection();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return BlocBuilder<NearbyProvidersCubit, NearbyState>(
      builder: (context, state) {
        final Widget child = switch (state.status) {
          NearbyStatus.initial || NearbyStatus.loading => Padding(
              padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
              child: SkeletonLoader.list(items: 2, itemHeight: 128),
            ),
          NearbyStatus.loaded => Padding(
              padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
              child: Column(
                children: [
                  for (var i = 0; i < state.providers.length; i++) ...[
                    if (i > 0) const SizedBox(height: AppSpacing.sm),
                    NearbyProviderCard(provider: state.providers[i]),
                  ],
                ],
              ),
            ),
          NearbyStatus.empty => const _NearestFallback(
              key: Key('home-nearest-empty'),
              message: AppStrings.nearbyEmpty,
            ),
          NearbyStatus.permissionDenied => const _NearestFallback(
              key: Key('home-nearest-permission'),
              message: AppStrings.locationPermissionNeeded,
            ),
          NearbyStatus.serviceDisabled => const _NearestFallback(
              key: Key('home-nearest-service-off'),
              message: AppStrings.locationServiceDisabled,
            ),
          NearbyStatus.error => Padding(
              padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
              child: Row(
                children: [
                  Icon(
                    Icons.error_outline,
                    size: AppIconSize.action,
                    color: theme.colorScheme.error,
                  ),
                  const SizedBox(width: AppSpacing.xs),
                  Expanded(
                    child: Text(
                      AppStrings.sectionLoadFailed,
                      style: theme.textTheme.bodyMedium,
                    ),
                  ),
                  AppButton.text(
                    label: AppStrings.retry,
                    onPressed: context.read<NearbyProvidersCubit>().load,
                  ),
                ],
              ),
            ),
        };

        return Padding(
          padding: const EdgeInsets.only(top: AppSpacing.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Padding(
                padding:
                    const EdgeInsets.symmetric(horizontal: AppSpacing.md),
                child: Text(
                  AppStrings.nearestTitle,
                  style: theme.textTheme.titleLarge,
                ),
              ),
              const SizedBox(height: AppSpacing.sm),
              child,
            ],
          ),
        );
      },
    );
  }
}

/// Inline "we could not use your location" row with the manual area-search way
/// out. Deliberately compact: it sits inside the page, not over it.
class _NearestFallback extends StatelessWidget {
  final String message;

  const _NearestFallback({super.key, required this.message});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
      child: AppCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(
                  Icons.location_searching,
                  size: AppIconSize.action,
                  color: theme.colorScheme.onSurfaceVariant,
                ),
                const SizedBox(width: AppSpacing.xs),
                Expanded(
                  child: Text(message, style: theme.textTheme.bodyMedium),
                ),
              ],
            ),
            const SizedBox(height: AppSpacing.xs),
            AppButton.secondary(
              key: const Key('home-nearest-area-cta'),
              label: AppStrings.searchByArea,
              onPressed: () => context.go(Routes.exploreMap),
            ),
          ],
        ),
      ),
    );
  }
}

/// Section wrapper: renders its title + child, an inline retry when the
/// section failed, or nothing when legitimately empty.
class _Section extends StatelessWidget {
  final HomeSection section;
  final Set<HomeSection> failed;
  final bool isEmpty;
  final String title;
  final Widget child;

  const _Section({
    required this.section,
    required this.failed,
    required this.isEmpty,
    required this.title,
    required this.child,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final hasFailed = failed.contains(section);
    if (isEmpty && !hasFailed) return const SizedBox.shrink();

    return Padding(
      padding: const EdgeInsets.only(top: AppSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
            child: Text(title, style: theme.textTheme.titleLarge),
          ),
          const SizedBox(height: AppSpacing.sm),
          if (hasFailed)
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
              child: Row(
                children: [
                  Icon(
                    Icons.error_outline,
                    size: AppIconSize.action,
                    color: theme.colorScheme.error,
                  ),
                  const SizedBox(width: AppSpacing.xs),
                  Expanded(
                    child: Text(
                      AppStrings.sectionLoadFailed,
                      style: theme.textTheme.bodyMedium,
                    ),
                  ),
                  AppButton.text(
                    label: AppStrings.retry,
                    onPressed: () => context
                        .read<HomeBloc>()
                        .add(RetryHomeSection(section)),
                  ),
                ],
              ),
            )
          else
            child,
        ],
      ),
    );
  }
}

class _MiniProviderCard extends StatelessWidget {
  final String name;
  final String? subtitle;
  final double? rating;
  final String? badge;

  const _MiniProviderCard({
    required this.name,
    this.subtitle,
    this.rating,
    this.badge,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsetsDirectional.only(end: AppSpacing.sm),
      child: SizedBox(
        width: 160,
        child: AppCard(
          padding: const EdgeInsets.all(AppSpacing.sm),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  Icon(
                    Icons.store_outlined,
                    size: AppIconSize.action,
                    color: theme.colorScheme.primary,
                  ),
                  const Spacer(),
                  if (badge != null)
                    Text(badge!, style: theme.textTheme.bodySmall)
                  else
                    Icon(
                      Icons.favorite,
                      size: AppIconSize.sm,
                      color: theme.colorScheme.error,
                    ),
                ],
              ),
              const SizedBox(height: AppSpacing.xs),
              Text(
                name,
                style: theme.textTheme.titleSmall,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
              if (subtitle != null)
                Text(
                  subtitle!,
                  style: theme.textTheme.bodySmall,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              if (rating != null && ProviderRating.hasRating(rating!))
                ProviderRating(rating: rating!),
            ],
          ),
        ),
      ),
    );
  }
}

/// Content-shaped skeleton for the whole home screen (first load).
class _HomeSkeleton extends StatelessWidget {
  const _HomeSkeleton();

  @override
  Widget build(BuildContext context) {
    return SkeletonLoader(
      child: ListView(
        physics: const NeverScrollableScrollPhysics(),
        padding: const EdgeInsets.all(AppSpacing.md),
        children: [
          Row(
            children: [
              for (var i = 0; i < 5; i++) ...[
                SkeletonLoader.box(
                  width: 60,
                  height: 60,
                  radius: AppRadius.lg,
                ),
                const SizedBox(width: AppSpacing.sm),
              ],
            ],
          ),
          const SizedBox(height: AppSpacing.lg),
          SkeletonLoader.box(width: 140, height: 20),
          const SizedBox(height: AppSpacing.sm),
          Row(
            children: [
              Expanded(
                child: SkeletonLoader.box(height: 200, radius: AppRadius.card),
              ),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: SkeletonLoader.box(height: 200, radius: AppRadius.card),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.lg),
          SkeletonLoader.box(width: 140, height: 20),
          const SizedBox(height: AppSpacing.sm),
          SkeletonLoader.box(height: 128, radius: AppRadius.card),
        ],
      ),
    );
  }
}
