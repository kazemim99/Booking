import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/widgets.dart';
import '../../domain/entities/provider_summary.dart';
import '../../domain/usecases/get_home_data.dart' show HomeSection;
import '../bloc/home_bloc.dart';
import '../bloc/home_event.dart';
import '../bloc/home_state.dart';
import '../widgets/upcoming_booking_card.dart';

/// Home: search entry → upcoming booking → categories → top providers →
/// promotions → recent & favorites. Sections load and fail independently.
class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  @override
  void initState() {
    super.initState();
    final bloc = context.read<HomeBloc>();
    if (bloc.state is HomeInitial) {
      bloc.add(const LoadHomeData());
    }
  }

  Future<void> _onRefresh() {
    final bloc = context.read<HomeBloc>();
    bloc.add(const RefreshHomeData());
    return bloc.stream
        .firstWhere((state) => state is HomeLoaded || state is HomeError)
        .then((_) {});
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
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
                    padding: const EdgeInsets.symmetric(
                      vertical: AppSpacing.md,
                    ),
                    children: [
                      const _SearchEntry(),
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
                        section: HomeSection.categories,
                        failed: loaded.failedSections,
                        isEmpty: loaded.categories.isEmpty,
                        title: AppStrings.categoriesTitle,
                        child: SizedBox(
                          height: 44,
                          child: ListView.separated(
                            scrollDirection: Axis.horizontal,
                            padding: const EdgeInsets.symmetric(
                              horizontal: AppSpacing.md,
                            ),
                            itemCount: loaded.categories.length,
                            separatorBuilder: (_, __) =>
                                const SizedBox(width: AppSpacing.xs),
                            itemBuilder: (context, index) {
                              final category = loaded.categories[index];
                              return ActionChip(
                                label: Text(category.name),
                                onPressed: () => context.go(
                                  '${Routes.explore}?category='
                                  '${Uri.encodeComponent(category.name)}',
                                ),
                              );
                            },
                          ),
                        ),
                      ),
                      _Section(
                        section: HomeSection.topProviders,
                        failed: loaded.failedSections,
                        isEmpty: loaded.topProviders.isEmpty,
                        title: AppStrings.topProvidersTitle,
                        child: SizedBox(
                          height: 180,
                          child: ListView.separated(
                            scrollDirection: Axis.horizontal,
                            padding: const EdgeInsets.symmetric(
                              horizontal: AppSpacing.md,
                            ),
                            itemCount: loaded.topProviders.length,
                            separatorBuilder: (_, __) =>
                                const SizedBox(width: AppSpacing.sm),
                            itemBuilder: (context, index) => _ProviderCard(
                              provider: loaded.topProviders[index],
                            ),
                          ),
                        ),
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
                                borderRadius:
                                    BorderRadius.circular(AppRadius.lg),
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
                                      const _ImagePlaceholder(width: 280),
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
    );
  }
}

/// Tappable search pill leading to explore.
class _SearchEntry extends StatelessWidget {
  const _SearchEntry();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
      child: Semantics(
        button: true,
        label: AppStrings.searchPlaceholder,
        child: Material(
          color: theme.colorScheme.surface,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(AppRadius.full),
            side: BorderSide(color: theme.colorScheme.outline),
          ),
          child: InkWell(
            borderRadius: BorderRadius.circular(AppRadius.full),
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
                    size: 20,
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  Expanded(
                    child: Text(
                      AppStrings.searchPlaceholder,
                      style: theme.textTheme.bodyMedium,
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
                    size: 18,
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

class _ProviderCard extends StatelessWidget {
  final ProviderSummary provider;

  const _ProviderCard({required this.provider});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return SizedBox(
      width: 200,
      child: AppCard(
        padding: EdgeInsets.zero,
        semanticLabel: provider.name,
        onTap: () => context.push(Routes.providerDetail(provider.id)),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            SizedBox(
              height: 96,
              width: double.infinity,
              child: provider.imageUrl != null
                  ? CachedNetworkImage(
                      imageUrl: provider.imageUrl!,
                      fit: BoxFit.cover,
                      placeholder: (_, __) => SkeletonLoader(
                        child: SkeletonLoader.box(height: 96, radius: 0),
                      ),
                      errorWidget: (_, __, ___) => const _ImagePlaceholder(),
                    )
                  : const _ImagePlaceholder(),
            ),
            Padding(
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
                ],
              ),
            ),
          ],
        ),
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
                    size: 20,
                    color: theme.colorScheme.primary,
                  ),
                  const Spacer(),
                  if (badge != null)
                    Text(badge!, style: theme.textTheme.bodySmall)
                  else
                    Icon(
                      Icons.favorite,
                      size: 16,
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
              if (rating != null)
                Row(
                  children: [
                    const Icon(Icons.star, size: 12, color: Colors.amber),
                    const SizedBox(width: AppSpacing.xxs),
                    Text(
                      rating!.toStringAsFixed(1),
                      style: theme.textTheme.bodySmall,
                    ),
                  ],
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _ImagePlaceholder extends StatelessWidget {
  final double? width;

  const _ImagePlaceholder({this.width});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      width: width ?? double.infinity,
      color: theme.colorScheme.surfaceContainerHighest,
      child: Icon(
        Icons.storefront_outlined,
        size: 32,
        color: theme.colorScheme.onSurfaceVariant,
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
          SkeletonLoader.box(height: 48, radius: AppRadius.full),
          const SizedBox(height: AppSpacing.lg),
          SkeletonLoader.box(width: 140, height: 20),
          const SizedBox(height: AppSpacing.sm),
          SkeletonLoader.box(height: 110, radius: AppRadius.lg),
          const SizedBox(height: AppSpacing.lg),
          SkeletonLoader.box(width: 140, height: 20),
          const SizedBox(height: AppSpacing.sm),
          Row(
            children: [
              Expanded(
                child: SkeletonLoader.box(height: 160, radius: AppRadius.lg),
              ),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: SkeletonLoader.box(height: 160, radius: AppRadius.lg),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
