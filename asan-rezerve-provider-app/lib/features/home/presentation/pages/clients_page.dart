import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/contacts/contact_picker.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/utils/phone_number.dart';
import '../../../../core/widgets/app_empty_state.dart';
import '../../../../core/widgets/app_error_state.dart';
import '../../../../core/widgets/app_loading.dart';
import '../../../../core/widgets/app_page_scaffold.dart';
import '../../../../core/widgets/app_snackbar.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../../domain/entities/provider_client.dart';
import '../../domain/entities/saved_customer.dart';
import '../cubit/clients_cubit.dart';
import '../widgets/customer_form_dialog.dart';
import '../widgets/provider_nav_bar.dart';

/// The Clients tab (specs: provider-clients, provider-customer-book): the
/// salon's customer book — customers it saved or picked from the phone's
/// contacts, plus everyone who booked online — with Persian-normalized search
/// and a client action sheet (book again, call, edit, remove, save).
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

/// Separated from [ClientsPage] so tests can pump it with a fake cubit and a
/// fake contact picker.
class ClientsView extends StatelessWidget {
  final ContactPicker? contactPicker;

  const ClientsView({super.key, this.contactPicker});

  @override
  Widget build(BuildContext context) {
    final picker = contactPicker ?? ContactPicker.platform();
    return BlocBuilder<ClientsCubit, ClientsState>(
      builder: (context, state) {
        final cubit = context.read<ClientsCubit>();
        return AppPageScaffold(
          automaticallyImplyLeading: false,
          titleWidget: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Text(
                AppStrings.clientsTitle,
                style: TextStyle(
                  fontSize: 17,
                  fontWeight: FontWeight.w700,
                  color: Colors.white,
                ),
              ),
              const SizedBox(width: AppSpacing.sm),
              if (state.status == ClientsStatus.ready)
                Text(
                  AppStrings.clientsCount(state.all.length),
                  // Softened white, not muted grey: the count is secondary
                  // but still sits on the blue chrome.
                  style: const TextStyle(fontSize: 13, color: Colors.white70),
                ),
            ],
          ),
          actions: [
            // Only where the phone offers its own picker (Chrome on Android
            // today); elsewhere customers are typed in.
            if (picker.isSupported)
              IconButton(
                key: const Key('clients-import'),
                tooltip: AppStrings.customerImportContacts,
                icon: const Icon(Icons.contact_phone_outlined,
                    color: Colors.white),
                onPressed: () => _importContacts(context, picker),
              ),
          ],
          floatingActionButton: state.status == ClientsStatus.ready
              ? FloatingActionButton.extended(
                  key: const Key('clients-add'),
                  onPressed: () => _addCustomer(context),
                  icon: const Icon(Icons.person_add_alt_1_outlined),
                  label: const Text(AppStrings.customerAdd),
                )
              : null,
          body: switch (state.status) {
            ClientsStatus.loading => const AppLoading.page(),
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
        // Bottom room so the last row is never under the add button.
        padding: const EdgeInsets.fromLTRB(
            AppSpacing.md, AppSpacing.md, AppSpacing.md, 88),
        itemCount: clients.length,
        separatorBuilder: (_, _) =>
            const Divider(color: AppColors.divider, height: 1),
        itemBuilder: (context, i) => _ClientRow(client: clients[i]),
      ),
    );
  }

  Future<void> _addCustomer(BuildContext context) async {
    final cubit = context.read<ClientsCubit>();
    final draft = await showCustomerForm(context);
    if (draft == null || !context.mounted) return;
    final failure = await cubit.add(draft);
    if (!context.mounted) return;
    failure == null
        ? AppSnackbar.success(context, AppStrings.customerSaved)
        : AppSnackbar.error(context, failure);
  }

  Future<void> _importContacts(
      BuildContext context, ContactPicker picker) async {
    final cubit = context.read<ClientsCubit>();
    final picked = await picker.pick();
    if (!context.mounted) return;
    if (picked.isEmpty) {
      AppSnackbar.info(context, AppStrings.customerContactsNothing);
      return;
    }
    final (summary, failure) = await cubit.importContacts(picked);
    if (!context.mounted) return;
    if (summary == null) {
      AppSnackbar.error(context, failure ?? AppStrings.homeLoadError);
      return;
    }
    AppSnackbar.success(
        context,
        AppStrings.customerImportResult(
            summary.added, summary.alreadySaved, summary.invalid));
  }
}

class _ClientRow extends StatelessWidget {
  final ProviderClient client;

  const _ClientRow({required this.client});

  String get _displayName =>
      client.name.isEmpty ? AppStrings.clientUnknownName : client.name;

  String get _phone => PhoneNumber.display(client.phone);

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
                      if (client.phone.isNotEmpty) _phone,
                      AppStrings.clientBookings(
                          client.totalBookings, client.upcomingBookings),
                    ].join(' · '),
                    textDirection: TextDirection.rtl,
                    style: const TextStyle(
                        fontSize: 12, color: AppColors.muted),
                  ),
                ],
              ),
            ),
            const Icon(Icons.chevron_right,
                size: AppIconSize.action, color: AppColors.muted),
          ],
        ),
      ),
    );
  }

  void _showClientSheet(BuildContext context) {
    final cubit = context.read<ClientsCubit>();
    final saved = client.saved;
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
                  if (client.phone.isNotEmpty) _phone,
                  AppStrings.clientBookings(
                      client.totalBookings, client.upcomingBookings),
                  if (client.lastVisitAt != null)
                    AppStrings.clientLastVisit(
                        '${client.lastVisitAt!.day}/${client.lastVisitAt!.month}'),
                ].join(' · '),
                style: const TextStyle(fontSize: 13, color: AppColors.muted),
              ),
              if (saved?.notes case final notes? when notes.isNotEmpty) ...[
                const SizedBox(height: AppSpacing.xs),
                Text(notes,
                    style: const TextStyle(fontSize: 13, color: AppColors.ink)),
              ],
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
                          customerId: saved?.id,
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
              if (saved != null)
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton.icon(
                        key: const Key('client-edit'),
                        onPressed: () {
                          Navigator.pop(sheetContext);
                          _edit(context, cubit, saved);
                        },
                        icon: const Icon(Icons.edit_outlined,
                            size: AppIconSize.action),
                        label: const Text(AppStrings.customerEdit),
                      ),
                    ),
                    const SizedBox(width: AppSpacing.sm),
                    Expanded(
                      child: OutlinedButton.icon(
                        key: const Key('client-remove'),
                        style: OutlinedButton.styleFrom(
                            foregroundColor: AppColors.danger),
                        onPressed: () {
                          Navigator.pop(sheetContext);
                          _remove(context, cubit, saved);
                        },
                        icon: const Icon(Icons.delete_outline,
                            size: AppIconSize.action),
                        label: const Text(AppStrings.customerRemove),
                      ),
                    ),
                  ],
                )
              else if (client.phone.isNotEmpty)
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton.icon(
                        key: const Key('client-save'),
                        onPressed: () {
                          Navigator.pop(sheetContext);
                          _saveToBook(context, cubit);
                        },
                        icon: const Icon(Icons.bookmark_add_outlined,
                            size: AppIconSize.action),
                        label: const Text(AppStrings.customerSaveToBook),
                      ),
                    ),
                  ],
                ),
              const SizedBox(height: AppSpacing.sm),
            ],
          ),
        ),
      ),
    );
  }

  Future<void> _edit(
      BuildContext context, ClientsCubit cubit, SavedCustomer saved) async {
    final draft = await showCustomerForm(
      context,
      title: AppStrings.customerEdit,
      initial: CustomerDraft(
        firstName: saved.firstName,
        lastName: saved.lastName,
        phone: saved.phone,
        notes: saved.notes,
      ),
    );
    if (draft == null || !context.mounted) return;
    final failure = await cubit.update(saved.id, draft);
    if (!context.mounted) return;
    failure == null
        ? AppSnackbar.success(context, AppStrings.customerSaved)
        : AppSnackbar.error(context, failure);
  }

  Future<void> _remove(
      BuildContext context, ClientsCubit cubit, SavedCustomer saved) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(saved.fullName),
        content: const Text(AppStrings.customerRemoveConfirm),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext, false),
            child: const Text(AppStrings.cancel),
          ),
          TextButton(
            key: const Key('client-remove-confirm'),
            onPressed: () => Navigator.pop(dialogContext, true),
            style: TextButton.styleFrom(foregroundColor: AppColors.danger),
            child: const Text(AppStrings.customerRemove),
          ),
        ],
      ),
    );
    if (confirmed != true || !context.mounted) return;
    final failure = await cubit.remove(saved.id);
    if (!context.mounted) return;
    failure == null
        ? AppSnackbar.success(context, AppStrings.customerRemoved)
        : AppSnackbar.error(context, failure);
  }

  /// An online client into the book, name split on the first space.
  Future<void> _saveToBook(BuildContext context, ClientsCubit cubit) async {
    final name = client.name.trim();
    final space = name.indexOf(' ');
    final draft = await showCustomerForm(
      context,
      initial: CustomerDraft(
        firstName: space < 0 ? name : name.substring(0, space),
        lastName: space < 0 ? '' : name.substring(space + 1),
        phone: client.phone,
      ),
    );
    if (draft == null || !context.mounted) return;
    final failure = await cubit.add(draft);
    if (!context.mounted) return;
    failure == null
        ? AppSnackbar.success(context, AppStrings.customerSaved)
        : AppSnackbar.error(context, failure);
  }
}
