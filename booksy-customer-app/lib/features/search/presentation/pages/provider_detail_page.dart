import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../booking/domain/entities/booking_entities.dart';
import '../bloc/provider_detail_cubit.dart';
import '../widgets/contact_location_section.dart';
import '../widgets/services_grid.dart';
import '../widgets/working_hours_section.dart';

/// Provider profile (deep-linkable at `/providers/:id`).
///
/// Hero cover → name + meta line → working hours (with an "open now" pill) →
/// services grid → about → contact & location, with the booking CTA pinned to
/// the bottom so it is reachable without scrolling.
class ProviderDetailPage extends StatelessWidget {
  final String providerId;

  /// Cubit override for tests; resolved from DI in the app.
  final ProviderDetailCubit? cubit;

  /// Fixed "now" for deterministic open/closed tests.
  final DateTime? now;

  const ProviderDetailPage({
    super.key,
    required this.providerId,
    this.cubit,
    this.now,
  });

  @override
  Widget build(BuildContext context) {
    return BlocProvider<ProviderDetailCubit>(
      create: (_) =>
          cubit ?? (getIt<ProviderDetailCubit>()..load(providerId)),
      child: BlocBuilder<ProviderDetailCubit, ProviderDetailState>(
        builder: (context, state) {
          return Scaffold(
            appBar: AppBar(
              title: Text(state.provider?.businessName ?? ''),
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
                      padding:
                          const EdgeInsets.symmetric(horizontal: AppSpacing.md),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          SkeletonLoader.box(width: 180, height: 24),
                          const SizedBox(height: AppSpacing.sm),
                          SkeletonLoader.box(width: 120, height: 16),
                          const SizedBox(height: AppSpacing.lg),
                          SkeletonLoader.box(height: 72, radius: AppRadius.md),
                          const SizedBox(height: AppSpacing.sm),
                          SkeletonLoader.box(height: 72, radius: AppRadius.md),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              contentBuilder: (context) =>
                  _ProviderContent(provider: state.provider!, now: now),
            ),
          );
        },
      ),
    );
  }
}

class _ProviderContent extends StatelessWidget {
  final ProviderDetail provider;
  final DateTime? now;

  const _ProviderContent({required this.provider, this.now});

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
    // the price band is derived from this provider's own service prices.
    final meta = ProviderMetaLine(
      category: provider.city,
      rating: provider.averageRating,
      reviewCount: provider.totalReviews,
      priceBand: PriceBand.fromPrices(provider.services.map((s) => s.price)),
    );

    final contact = ContactLocationSection(
      address: address.isEmpty ? null : address,
    );

    return ListView(
      padding: const EdgeInsets.only(bottom: AppSpacing.lg),
      children: [
        ProviderImage(
          key: const Key('provider-hero-image'),
          imageUrl: provider.profileImageUrl ?? provider.logoUrl,
          width: double.infinity,
          height: 200,
          placeholderIconSize: AppIconSize.hero,
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
              ServicesGrid(services: provider.services),
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
            ],
          ),
        ),
      ],
    );
  }
}
