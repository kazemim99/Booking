import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../auth/presentation/bloc/auth_bloc.dart';
import '../../../auth/presentation/bloc/auth_state.dart';
import '../../../booking/domain/entities/booking_entities.dart';
import '../bloc/provider_customer_cubit.dart';
import '../bloc/provider_detail_cubit.dart';
import '../widgets/contact_location_section.dart';
import '../widgets/services_grid.dart';
import '../widgets/working_hours_section.dart';
import '../widgets/provider_gallery.dart';
import '../../../reviews/presentation/widgets/provider_reviews_section.dart';
import '../../../reviews/domain/entities/review.dart';

/// Provider profile (deep-linkable at `/providers/:id`).
///
/// Hero cover → name + meta line → working hours (with an "open now" pill) →
/// services grid → about → contact & location (address, map, directions) →
/// reviews, with the booking CTA pinned to the bottom so it is reachable without
/// scrolling.
///
/// For a signed-in customer the opening is recorded as a visit (home's
/// "recently visited") and the app bar carries a favourite heart.
class ProviderDetailPage extends StatelessWidget {
  final String providerId;

  /// Cubit override for tests; resolved from DI in the app.
  final ProviderDetailCubit? cubit;

  /// The customer's side (visit + favourite); resolved from DI in the app.
  final ProviderCustomerCubit? customerCubit;

  /// Fixed "now" for deterministic open/closed tests.
  final DateTime? now;

  const ProviderDetailPage({
    super.key,
    required this.providerId,
    this.cubit,
    this.customerCubit,
    this.now,
  });

  /// No session any more: it expired or was refused (Unauthenticated), or the
  /// customer logged out (LoggedOut).
  static bool _signedOut(AuthState state) =>
      state is Unauthenticated || state is LoggedOut;

  @override
  Widget build(BuildContext context) {
    return MultiBlocProvider(
      providers: [
        BlocProvider<ProviderDetailCubit>(
          create: (_) =>
              cubit ?? (getIt<ProviderDetailCubit>()..load(providerId)),
        ),
        BlocProvider<ProviderCustomerCubit>(
          // Not lazy: the visit is recorded on opening, heart or no heart.
          lazy: false,
          create: (context) {
            final customer = customerCubit ??
                getIt<ProviderCustomerCubit>(param1: providerId);
            if (context.read<AuthBloc>().state is Authenticated) {
              customer.customerSignedIn();
            }
            return customer;
          },
        ),
      ],
      // A sign-in can bring the customer back to this very page (the heart's
      // login round-trip), so the session is followed, not read once. The page
      // stays mounted in the home tab's stack across a logout, which ends in
      // LoggedOut, so that is a sign-out here as much as Unauthenticated is.
      child: BlocListener<AuthBloc, AuthState>(
        listenWhen: (previous, current) =>
            (current is Authenticated && previous is! Authenticated) ||
            (_signedOut(current) && !_signedOut(previous)),
        listener: (context, auth) {
          final customer = context.read<ProviderCustomerCubit>();
          if (auth is Authenticated) {
            customer.customerSignedIn();
          } else {
            customer.customerSignedOut();
          }
        },
        child: BlocBuilder<ProviderDetailCubit, ProviderDetailState>(
          builder: (context, state) {
            return Scaffold(
              appBar: AppBar(
                title: Text(state.provider?.businessName ?? ''),
                actions: [
                  if (state.status == ProviderDetailStatus.loaded)
                    _FavoriteToggle(providerId: providerId),
                ],
              ),
              bottomNavigationBar: state.status == ProviderDetailStatus.loaded
                  ? SafeArea(
                      child: Padding(
                        padding: const EdgeInsets.all(AppSpacing.md),
                        child: AppButton(
                          key: const Key('provider-book-cta'),
                          label: AppStrings.bookAction,
                          onPressed: () =>
                              context.push(Routes.bookingFlow(providerId)),
                        ),
                      ),
                    )
                  : null,
              body: StateSwitcher(
                status: switch (state.status) {
                  ProviderDetailStatus.loading => ViewStatus.loading,
                  ProviderDetailStatus.loaded => ViewStatus.content,
                  ProviderDetailStatus.error => ViewStatus.error,
                },
                errorMessage: state.errorMessage,
                onRetry: () =>
                    context.read<ProviderDetailCubit>().load(providerId),
                skeleton: SkeletonLoader(
                  child: ListView(
                    physics: const NeverScrollableScrollPhysics(),
                    padding: EdgeInsets.zero,
                    children: [
                      SkeletonLoader.box(height: 200, radius: 0),
                      const SizedBox(height: AppSpacing.md),
                      Padding(
                        padding: const EdgeInsets.symmetric(
                            horizontal: AppSpacing.md),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            SkeletonLoader.box(width: 180, height: 24),
                            const SizedBox(height: AppSpacing.sm),
                            SkeletonLoader.box(width: 120, height: 16),
                            const SizedBox(height: AppSpacing.lg),
                            SkeletonLoader.box(
                                height: 72, radius: AppRadius.md),
                            const SizedBox(height: AppSpacing.sm),
                            SkeletonLoader.box(
                                height: 72, radius: AppRadius.md),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
                contentBuilder: (context) => _ProviderContent(
                  provider: state.provider!,
                  now: now,
                  reviews: state.reviews,
                  reviewsLoading: state.reviewsLoading,
                  reviewsFailed: state.reviewsFailed,
                  reviewsLoadingMore: state.reviewsLoadingMore,
                ),
              ),
            );
          },
        ),
      ),
    );
  }
}

/// The favourite heart in the app bar. A guest is sent to sign in and brought
/// back here; for a customer it changes at once and changes back, with a
/// message, if the server refuses.
class _FavoriteToggle extends StatelessWidget {
  final String providerId;

  const _FavoriteToggle({required this.providerId});

  Future<void> _toggle(BuildContext context) async {
    if (context.read<AuthBloc>().state is! Authenticated) {
      final target = Uri.encodeComponent(Routes.providerDetail(providerId));
      context.push('${Routes.login}?redirect=$target');
      return;
    }
    final customer = context.read<ProviderCustomerCubit>();
    final adding = !customer.state.isFavorite;
    final done = await customer.toggleFavorite();
    if (!done && context.mounted) {
      AppSnackbar.error(
        context,
        adding ? AppStrings.favoriteAddFailed : AppStrings.favoriteRemoveFailed,
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<ProviderCustomerCubit, ProviderCustomerState>(
      builder: (context, state) {
        final favorite = state.isFavorite;
        final label =
            favorite ? AppStrings.favoriteRemove : AppStrings.favoriteAdd;
        // One node: a button named for what the tap will do.
        return Semantics(
          key: const Key('provider-favorite-toggle'),
          button: true,
          label: label,
          excludeSemantics: true,
          onTap: () => _toggle(context),
          // IconButton: a 48 dp target.
          child: IconButton(
            tooltip: label,
            icon: Icon(
              favorite ? Icons.favorite : Icons.favorite_border,
              // The bar is brand blue; its foreground is the icon colour.
              color: Theme.of(context).appBarTheme.foregroundColor,
            ),
            onPressed: () => _toggle(context),
          ),
        );
      },
    );
  }
}

class _ProviderContent extends StatelessWidget {
  final ProviderDetail provider;
  final DateTime? now;
  final ProviderReviews? reviews;
  final bool reviewsLoading;
  final bool reviewsFailed;
  final bool reviewsLoadingMore;

  const _ProviderContent({
    required this.provider,
    this.now,
    this.reviews,
    this.reviewsLoading = false,
    this.reviewsFailed = false,
    this.reviewsLoadingMore = false,
  });

  /// Voting is for signed-in readers: a guest is sent to sign in and brought
  /// back here. A refused vote (the author's own review, say) is said aloud.
  Future<void> _vote(BuildContext context, Review review, bool helpful) async {
    if (context.read<AuthBloc>().state is! Authenticated) {
      final target = Uri.encodeComponent(Routes.providerDetail(provider.id));
      context.push('${Routes.login}?redirect=$target');
      return;
    }
    final error =
        await context.read<ProviderDetailCubit>().vote(review.id, helpful);
    if (error != null && context.mounted) {
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(error)));
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final address = [provider.city, provider.addressLine]
        .whereType<String>()
        .where((part) => part.isNotEmpty)
        .join('، ');

    // The meta line's first slot is the category — which `ProviderDetail` does
    // not carry (the payload's `type` is dropped by the parser), so the city
    // stands in for it. Rating hides itself while the provider is unrated, and
    // the starting price is this provider's cheapest priced service.
    final meta = ProviderMetaLine(
      category: provider.city,
      rating: provider.averageRating,
      reviewCount: provider.totalReviews,
      startingPrice: provider.services
          .map((s) => s.price.round())
          .where((price) => price > 0)
          .fold<int?>(null, (min, p) => min == null || p < min ? p : min),
    );

    // Address, map and directions in one section (QA recording 2026-09-23 #7); a salon known only by its pin still
    // gets the map, one with only an address the address row.
    final contact = ContactLocationSection(
      address: address.isEmpty ? null : address,
      businessName: provider.businessName,
      latitude: provider.latitude,
      longitude: provider.longitude,
    );

    return ListView(
      padding: const EdgeInsets.only(bottom: AppSpacing.lg),
      children: [
        ProviderGallery(
          images: provider.images,
          fallbackImageUrl: provider.profileImageUrl ?? provider.logoUrl,
        ),
        Padding(
          padding: const EdgeInsets.all(AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                provider.businessName,
                style: theme.textTheme.headlineSmall,
              ),
              if (meta.hasContent) ...[
                const SizedBox(height: AppSpacing.xs),
                meta,
              ],
              if (provider.businessHours.isNotEmpty) ...[
                const SizedBox(height: AppSpacing.lg),
                WorkingHoursSection(
                  hours: provider.businessHours,
                  now: now,
                ),
              ],
              const SizedBox(height: AppSpacing.lg),
              Text(AppStrings.servicesTitle, style: theme.textTheme.titleLarge),
              const SizedBox(height: AppSpacing.xs),
              ServicesGrid(
                services: provider.services,
                // Straight into booking with that service already chosen.
                onServiceTap: (service) => context.push(
                  Routes.bookingFlow(provider.id, serviceId: service.id),
                ),
              ),
              if (provider.description?.isNotEmpty == true) ...[
                const SizedBox(height: AppSpacing.lg),
                Text(AppStrings.aboutTitle, style: theme.textTheme.titleLarge),
                const SizedBox(height: AppSpacing.xs),
                Text(provider.description!, style: theme.textTheme.bodyLarge),
              ],
              if (contact.hasContent) ...[
                const SizedBox(height: AppSpacing.lg),
                contact,
              ],
              // Reviews stand on their own: a salon the catalogue knows only by
              // its pin used to lose them, because they sat inside the
              // address-only contact block (UX review #12).
              const SizedBox(height: AppSpacing.lg),
              ProviderReviewsSection(
                reviews: reviews,
                loading: reviewsLoading,
                failed: reviewsFailed,
                loadingMore: reviewsLoadingMore,
                onRetry: () =>
                    context.read<ProviderDetailCubit>().loadReviews(provider.id),
                onLoadMore: () => context
                    .read<ProviderDetailCubit>()
                    .loadMoreReviews(provider.id),
                onVote: (review, helpful) => _vote(context, review, helpful),
                salonName: provider.businessName,
                salonLogoUrl: provider.logoUrl,
              ),
            ],
          ),
        ),
      ],
    );
  }
}
