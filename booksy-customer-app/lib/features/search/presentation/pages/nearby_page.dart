import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../home/domain/entities/provider_summary.dart';
import '../bloc/nearby_providers_cubit.dart';
import '../widgets/provider_result_card.dart';

/// "Near me" discovery. Resolves device location and shows providers ordered
/// by distance via the confirmed `/Providers/search` contract. When location
/// permission is denied or the service is off, it offers the manual
/// area/district search as a graceful fallback (never blocks).
class NearbyPage extends StatelessWidget {
  const NearbyPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => getIt<NearbyProvidersCubit>()..load(),
      child: Scaffold(
        appBar: AppBar(title: const Text(AppStrings.nearbyTitle)),
        body: BlocBuilder<NearbyProvidersCubit, NearbyState>(
          builder: (context, state) {
            final cubit = context.read<NearbyProvidersCubit>();
            switch (state.status) {
              case NearbyStatus.initial:
              case NearbyStatus.loading:
                return Padding(
                  padding:
                      const EdgeInsets.symmetric(horizontal: AppSpacing.md),
                  child: SkeletonLoader.list(items: 4, itemHeight: 104),
                );
              case NearbyStatus.loaded:
                return _ResultsList(providers: state.providers);
              case NearbyStatus.empty:
                return EmptyState(
                  icon: Icons.location_searching,
                  title: AppStrings.nearbyEmpty,
                  ctaLabel: AppStrings.searchByArea,
                  onCta: () => context.push(Routes.exploreArea),
                );
              case NearbyStatus.permissionDenied:
                return EmptyState(
                  icon: Icons.location_disabled,
                  title: AppStrings.locationPermissionNeeded,
                  ctaLabel: AppStrings.searchByArea,
                  onCta: () => context.push(Routes.exploreArea),
                );
              case NearbyStatus.serviceDisabled:
                return EmptyState(
                  icon: Icons.location_off,
                  title: AppStrings.locationServiceDisabled,
                  ctaLabel: AppStrings.searchByArea,
                  onCta: () => context.push(Routes.exploreArea),
                );
              case NearbyStatus.error:
                return ErrorState(
                  message: state.errorMessage,
                  onRetry: cubit.load,
                );
            }
          },
        ),
      ),
    );
  }
}

class _ResultsList extends StatelessWidget {
  final List<ProviderSummary> providers;
  const _ResultsList({required this.providers});

  @override
  Widget build(BuildContext context) {
    return ListView.separated(
      padding: const EdgeInsets.all(AppSpacing.md),
      itemCount: providers.length,
      separatorBuilder: (_, __) => const SizedBox(height: AppSpacing.sm),
      itemBuilder: (context, index) =>
          ProviderResultCard(provider: providers[index]),
    );
  }
}
