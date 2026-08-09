import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../home/domain/entities/provider_summary.dart';
import '../bloc/area_search_cubit.dart';
import '../widgets/provider_result_card.dart';

/// Area / district discovery. The typed area name is geocoded (keyless OSM
/// Nominatim) to coordinates, then providers are searched around that point
/// via the confirmed `/Providers/search` distance path.
class AreaPage extends StatefulWidget {
  const AreaPage({super.key});

  @override
  State<AreaPage> createState() => _AreaPageState();
}

class _AreaPageState extends State<AreaPage> {
  final _controller = TextEditingController();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => getIt<AreaSearchCubit>(),
      child: Scaffold(
        appBar: AppBar(title: const Text(AppStrings.searchByArea)),
        body: Builder(
          builder: (context) {
            final cubit = context.read<AreaSearchCubit>();
            void submit() {
              FocusScope.of(context).unfocus();
              cubit.searchArea(_controller.text);
            }

            return Column(
              children: [
                Padding(
                  padding: const EdgeInsets.all(AppSpacing.md),
                  child: AppTextField(
                    controller: _controller,
                    hint: AppStrings.areaSearchHint,
                    prefixIcon: Icons.map_outlined,
                    onSubmitted: (_) => submit(),
                  ),
                ),
                Expanded(
                  child: BlocBuilder<AreaSearchCubit, AreaSearchState>(
                    builder: (context, state) {
                      switch (state.status) {
                        case AreaSearchStatus.initial:
                          return const EmptyState(
                            icon: Icons.travel_explore,
                            title: AppStrings.searchByArea,
                            subtitle: AppStrings.areaSearchHint,
                          );
                        case AreaSearchStatus.loading:
                          return Padding(
                            padding: const EdgeInsets.symmetric(
                              horizontal: AppSpacing.md,
                            ),
                            child: SkeletonLoader.list(
                              items: 4,
                              itemHeight: 104,
                            ),
                          );
                        case AreaSearchStatus.loaded:
                          return _ResultsList(providers: state.providers);
                        case AreaSearchStatus.empty:
                          return const EmptyState(
                            icon: Icons.search_off,
                            title: AppStrings.nearbyEmpty,
                          );
                        case AreaSearchStatus.areaNotFound:
                          return const EmptyState(
                            icon: Icons.wrong_location_outlined,
                            title: AppStrings.areaNotFound,
                          );
                        case AreaSearchStatus.error:
                          return ErrorState(
                            message: state.errorMessage,
                            onRetry: submit,
                          );
                      }
                    },
                  ),
                ),
              ],
            );
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
