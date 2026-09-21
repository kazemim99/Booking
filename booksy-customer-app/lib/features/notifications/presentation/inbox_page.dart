import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../core/constants/app_strings.dart';
import '../domain/inbox_item.dart';
import 'inbox_cubit.dart';
import 'inbox_destination.dart';

/// The customer's notifications. Expects an [InboxCubit] above it — the same instance the bell reads, so the
/// badge and this list cannot disagree.
class InboxPage extends StatefulWidget {
  const InboxPage({super.key});

  @override
  State<InboxPage> createState() => _InboxPageState();
}

class _InboxPageState extends State<InboxPage> {
  @override
  void initState() {
    super.initState();
    context.read<InboxCubit>().load();
  }

  Future<void> _open(InboxItem item) async {
    final cubit = context.read<InboxCubit>();
    final messenger = ScaffoldMessenger.of(context);
    final router = GoRouter.maybeOf(context);

    if (!await cubit.markRead(item.id)) {
      messenger.showSnackBar(const SnackBar(content: Text(AppStrings.notificationsMarkFailed)));
    }

    final target = inboxDestination(item);
    if (target != null && mounted) router?.push(target);
  }

  Future<void> _markAll() async {
    final ok = await context.read<InboxCubit>().markAllRead();
    if (!ok && mounted) {
      ScaffoldMessenger.of(context)
          .showSnackBar(const SnackBar(content: Text(AppStrings.notificationsMarkFailed)));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text(AppStrings.notificationsTitle),
        actions: [
          BlocBuilder<InboxCubit, InboxState>(
            buildWhen: (a, b) => a.unreadCount != b.unreadCount,
            builder: (context, state) => state.unreadCount > 0
                ? TextButton(
                    key: const Key('inbox-mark-all'),
                    onPressed: _markAll,
                    child: const Text(AppStrings.notificationsMarkAllRead),
                  )
                : const SizedBox.shrink(),
          ),
        ],
      ),
      body: BlocBuilder<InboxCubit, InboxState>(
        builder: (context, state) {
          if (state.loading && !state.loaded) {
            return const Center(child: CircularProgressIndicator());
          }

          // A failure is shown as a failure, never as "you have no notifications".
          if (state.error != null && !state.loaded) {
            return Center(
              key: const Key('inbox-error'),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Text(AppStrings.notificationsLoadFailed),
                  TextButton(
                    onPressed: () => context.read<InboxCubit>().load(),
                    child: const Text(AppStrings.notificationsRetry),
                  ),
                ],
              ),
            );
          }

          if (state.isEmpty) {
            return const Center(key: Key('inbox-empty'), child: Text(AppStrings.notificationsEmpty));
          }

          return RefreshIndicator(
            onRefresh: () => context.read<InboxCubit>().load(),
            child: ListView.separated(
              itemCount: state.items.length,
              separatorBuilder: (_, __) => const Divider(height: 1),
              itemBuilder: (context, i) => _InboxRow(item: state.items[i], onTap: _open),
            ),
          );
        },
      ),
    );
  }
}

class _InboxRow extends StatelessWidget {
  final InboxItem item;
  final ValueChanged<InboxItem> onTap;

  const _InboxRow({required this.item, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return ListTile(
      key: Key('inbox-row-${item.id}'),
      onTap: () => onTap(item),
      tileColor: item.isUnread ? theme.colorScheme.primary.withValues(alpha: 0.06) : null,
      leading: item.isUnread
          ? Icon(Icons.circle, key: Key('inbox-unread-${item.id}'), size: 10, color: theme.colorScheme.primary)
          : const SizedBox(width: 10),
      title: Text(item.subject, style: const TextStyle(fontWeight: FontWeight.w600)),
      subtitle: Text(item.body),
    );
  }
}
