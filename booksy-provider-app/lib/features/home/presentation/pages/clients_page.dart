import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/app_empty_state.dart';
import '../../../../core/widgets/app_error_state.dart';
import '../../../../core/widgets/app_snackbar.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../../domain/entities/provider_client.dart';
import '../cubit/clients_cubit.dart';
import '../widgets/provider_nav_bar.dart';

/// The Clients tab (spec: provider-clients): the provider's client book with
/// Persian-normalized search and a client action sheet (call, book again).
class ClientsPage extends StatelessWidget {
  const ClientsPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider<ClientsCubit>(
      create: (_) => getIt<ClientsCubit>()..load(),
      child: const ClientsView(),
    );
  }
}

/// Separated from [ClientsPage] so tests can pump it with a fake cubit.
class ClientsView extends StatelessWidget {
  const ClientsView({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<ClientsCubit, ClientsState>(
      builder: (context, state) {
        final cubit = context.read<ClientsCubit>();
        return Scaffold(
          backgroundColor: Colors.white,
          appBar: AppBar(
            backgroundColor: Colors.white,
            surfaceTintColor: Colors.transparent,
            elevation: 0,
            automaticallyImplyLeading: false,
            title: Row(
              children: [
                const Text(
                  AppStrings.clientsTitle,
                  style: TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w700,
                    color: AppColors.ink,
                  ),
                ),
                const SizedBox(width: AppSpacing.sm),
                if (state.status == ClientsStatus.ready)
                  Text(
                    AppStrings.clientsCount(state.all.length),
                    style:
                        const TextStyle(fontSize: 13, color: AppColors.muted),
                  ),
              ],
            ),
          ),
          body: switch (state.status) {
            ClientsStatus.loading =>
              const Center(child: CircularProgressIndicator()),
            ClientsStatus.failed => AppErrorState(
                message: state.error ?? AppStrings.homeLoadError,
                onRetry: cubit.load,
              ),
            ClientsStatus.ready => Column(
                children: [
                  Padding(
                    padding: const EdgeInsets.fromLTRB(
                        AppSpacing.md, AppSpacing.xs, AppSpacing.md, 0),
                    child: AppTextField(
                      key: const Key('clients-search'),
                      hint: AppStrings.clientsSearchHint,
                      prefixIcon: Icons.search,
                      onChanged: cubit.search,
                    ),
                  ),
                  Expanded(child: _list(context, state)),
                ],
              ),
          },
          bottomNavigationBar: const ProviderNavBar(active: NavTab.clients),
        );
      },
    );
  }

  Widget _list(BuildContext context, ClientsState state) {
    if (state.all.isEmpty) {
      return const AppEmptyState(
        icon: Icons.people_outline,
        message: AppStrings.clientsEmptyTitle,
        description: AppStrings.clientsEmptyBody,
      );
    }
    final clients = state.filtered;
    if (clients.isEmpty) {
      return const AppEmptyState(
        icon: Icons.search_off,
        message: AppStrings.clientsSearchEmpty,
      );
    }

    return RefreshIndicator(
      onRefresh: () => context.read<ClientsCubit>().refresh(),
      child: ListView.separated(
        key: const Key('clients-list'),
        padding: const EdgeInsets.all(AppSpacing.md),
        itemCount: clients.length,
        separatorBuilder: (_, _) =>
            const Divider(color: AppColors.divider, height: 1),
        itemBuilder: (context, i) => _ClientRow(client: clients[i]),
      ),
    );
  }
}

class _ClientRow extends StatelessWidget {
  final ProviderClient client;

  const _ClientRow({required this.client});

  String get _displayName =>
      client.name.isEmpty ? AppStrings.clientUnknownName : client.name;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      key: Key('client-row-${client.customerId}'),
      onTap: () => _showClientSheet(context),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
        child: Row(
          children: [
            CircleAvatar(
              radius: 20,
              backgroundColor: AppColors.primarySoft,
              child: Text(
                _displayName.characters.first,
                style: const TextStyle(
                  fontSize: 16,
                  color: AppColors.primary,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
            const SizedBox(width: AppSpacing.sm),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    _displayName,
                    style: const TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.w600,
                      color: AppColors.ink,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    [
                      if (client.phone.isNotEmpty) client.phone,
                      AppStrings.clientBookings(
                          client.totalBookings, client.upcomingBookings),
                    ].join(' · '),
                    style: const TextStyle(
                        fontSize: 12, color: AppColors.muted),
                  ),
                ],
              ),
            ),
            const Icon(Icons.chevron_left,
                size: AppIconSize.action, color: AppColors.muted),
          ],
        ),
      ),
    );
  }

  void _showClientSheet(BuildContext context) {
    showModalBottomSheet<void>(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(
          top: Radius.circular(AppRadius.bottomSheet),
        ),
      ),
      builder: (sheetContext) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.md),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                _displayName,
                style: const TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                  color: AppColors.ink,
                ),
              ),
              const SizedBox(height: AppSpacing.xs),
              Text(
                [
                  if (client.phone.isNotEmpty) client.phone,
                  AppStrings.clientBookings(
                      client.totalBookings, client.upcomingBookings),
                  if (client.lastVisitAt != null)
                    AppStrings.clientLastVisit(
                        '${client.lastVisitAt!.day}/${client.lastVisitAt!.month}'),
                ].join(' · '),
                style: const TextStyle(fontSize: 13, color: AppColors.muted),
              ),
              const SizedBox(height: AppSpacing.md),
              Row(
                children: [
                  Expanded(
                    child: FilledButton.icon(
                      key: const Key('client-book-again'),
                      onPressed: () {
                        Navigator.pop(sheetContext);
                        context.push(Routes.newBookingFor(
                          client: _displayName ==
                                  AppStrings.clientUnknownName
                              ? ''
                              : client.name,
                          phone: client.phone,
                        ));
                      },
                      icon: const Icon(Icons.event_outlined,
                          size: AppIconSize.action),
                      label: const Text(AppStrings.clientBookAgain),
                    ),
                  ),
                  if (client.phone.isNotEmpty) ...[
                    const SizedBox(width: AppSpacing.sm),
                    Expanded(
                      child: OutlinedButton.icon(
                        key: const Key('client-call'),
                        onPressed: () {
                          Clipboard.setData(
                              ClipboardData(text: client.phone));
                          Navigator.pop(sheetContext);
                          AppSnackbar.info(
                              context, AppStrings.phoneCopied);
                        },
                        icon: const Icon(Icons.phone_outlined,
                            size: AppIconSize.action),
                        label: const Text(AppStrings.homeActionCall),
                      ),
                    ),
                  ],
                ],
              ),
              const SizedBox(height: AppSpacing.sm),
            ],
          ),
        ),
      ),
    );
  }
}
