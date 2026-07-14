import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/utils/jalali_formatter.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../booking/domain/entities/booking_entities.dart';
import '../bloc/provider_detail_cubit.dart';

/// Provider detail (deep-linkable at /providers/:id): gallery header,
/// name/rating/address/hours, bookable services. The booking CTA is pinned
/// to the bottom so it is always visible without scrolling.
class ProviderDetailPage extends StatelessWidget {
  final String providerId;

  const ProviderDetailPage({super.key, required this.providerId});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => getIt<ProviderDetailCubit>()..load(providerId),
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
                  padding: const EdgeInsets.all(AppSpacing.md),
                  children: [
                    SkeletonLoader.box(height: 180, radius: AppRadius.lg),
                    const SizedBox(height: AppSpacing.md),
                    SkeletonLoader.box(width: 180, height: 24),
                    const SizedBox(height: AppSpacing.sm),
                    SkeletonLoader.box(width: 120, height: 16),
                    const SizedBox(height: AppSpacing.lg),
                    SkeletonLoader.box(height: 72, radius: AppRadius.lg),
                    const SizedBox(height: AppSpacing.sm),
                    SkeletonLoader.box(height: 72, radius: AppRadius.lg),
                  ],
                ),
              ),
              contentBuilder: (context) =>
                  _ProviderContent(provider: state.provider!),
            ),
          );
        },
      ),
    );
  }
}

class _ProviderContent extends StatelessWidget {
  final ProviderDetail provider;

  const _ProviderContent({required this.provider});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final image = provider.profileImageUrl ?? provider.logoUrl;

    return ListView(
      padding: const EdgeInsets.only(bottom: AppSpacing.lg),
      children: [
        SizedBox(
          height: 200,
          child: image != null
              ? CachedNetworkImage(
                  imageUrl: image,
                  fit: BoxFit.cover,
                  placeholder: (_, __) => SkeletonLoader(
                    child: SkeletonLoader.box(height: 200, radius: 0),
                  ),
                  errorWidget: (_, __, ___) => _headerPlaceholder(theme),
                )
              : _headerPlaceholder(theme),
        ),
        Padding(
          padding: const EdgeInsets.all(AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(provider.businessName, style: theme.textTheme.headlineSmall),
              const SizedBox(height: AppSpacing.xs),
              Row(
                children: [
                  const Icon(Icons.star, size: 18, color: Colors.amber),
                  const SizedBox(width: AppSpacing.xxs),
                  Text(
                    provider.averageRating.toStringAsFixed(1),
                    style: theme.textTheme.titleSmall,
                  ),
                  const SizedBox(width: AppSpacing.xxs),
                  Text(
                    '(${JalaliFormatter.toPersianDigits('${provider.totalReviews}')} نظر)',
                    style: theme.textTheme.bodySmall,
                  ),
                ],
              ),
              if (provider.addressLine != null || provider.city != null) ...[
                const SizedBox(height: AppSpacing.xs),
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Icon(
                      Icons.location_on_outlined,
                      size: 18,
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                    const SizedBox(width: AppSpacing.xxs),
                    Expanded(
                      child: Text(
                        [provider.city, provider.addressLine]
                            .whereType<String>()
                            .join('، '),
                        style: theme.textTheme.bodyMedium,
                      ),
                    ),
                  ],
                ),
              ],
              if (provider.description?.isNotEmpty == true) ...[
                const SizedBox(height: AppSpacing.md),
                Text(AppStrings.aboutTitle, style: theme.textTheme.titleLarge),
                const SizedBox(height: AppSpacing.xs),
                Text(provider.description!, style: theme.textTheme.bodyLarge),
              ],
              if (provider.businessHours.isNotEmpty) ...[
                const SizedBox(height: AppSpacing.md),
                Text(
                  AppStrings.workingHoursTitle,
                  style: theme.textTheme.titleLarge,
                ),
                const SizedBox(height: AppSpacing.xs),
                ...provider.businessHours.map(
                  (h) => Padding(
                    padding: const EdgeInsets.symmetric(
                      vertical: AppSpacing.xxs,
                    ),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(h.dayOfWeek, style: theme.textTheme.bodyMedium),
                        Text(
                          h.isClosed
                              ? 'تعطیل'
                              : JalaliFormatter.toPersianDigits(
                                  '${h.openTime ?? ''} – ${h.closeTime ?? ''}',
                                ),
                          style: theme.textTheme.bodyMedium,
                        ),
                      ],
                    ),
                  ),
                ),
              ],
              const SizedBox(height: AppSpacing.md),
              Text(AppStrings.servicesTitle, style: theme.textTheme.titleLarge),
              const SizedBox(height: AppSpacing.xs),
              if (provider.services.isEmpty)
                Text(
                  AppStrings.noResultsTitle,
                  style: theme.textTheme.bodyMedium,
                )
              else
                ...provider.services.map(
                  (service) => Padding(
                    padding: const EdgeInsets.only(bottom: AppSpacing.sm),
                    child: ServiceTile(service: service),
                  ),
                ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _headerPlaceholder(ThemeData theme) => Container(
        color: theme.colorScheme.surfaceContainerHighest,
        child: Icon(
          Icons.storefront_outlined,
          size: 56,
          color: theme.colorScheme.onSurfaceVariant,
        ),
      );
}

/// A bookable service row: name, duration, price.
class ServiceTile extends StatelessWidget {
  final ServiceItem service;
  final VoidCallback? onTap;
  final bool selected;

  const ServiceTile({
    super.key,
    required this.service,
    this.onTap,
    this.selected = false,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final price = JalaliFormatter.toPersianDigits(
      '${service.price.toStringAsFixed(0)} ${service.currency}'.trim(),
    );
    final duration = JalaliFormatter.toPersianDigits(
      '${service.durationMinutes} دقیقه',
    );

    return AppCard(
      onTap: onTap,
      semanticLabel: '${service.name}، $duration، $price',
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(service.name, style: theme.textTheme.titleSmall),
                const SizedBox(height: AppSpacing.xxs),
                Row(
                  children: [
                    Icon(
                      Icons.schedule,
                      size: 14,
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                    const SizedBox(width: AppSpacing.xxs),
                    Text(duration, style: theme.textTheme.bodySmall),
                  ],
                ),
              ],
            ),
          ),
          Text(price, style: theme.textTheme.titleSmall),
          if (selected) ...[
            const SizedBox(width: AppSpacing.xs),
            Icon(
              Icons.check_circle,
              size: 20,
              color: theme.colorScheme.primary,
            ),
          ],
        ],
      ),
    );
  }
}
