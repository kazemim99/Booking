import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../config/theme/app_tokens.dart';
import '../../../core/constants/app_strings.dart';
import '../../../core/utils/relative_time.dart';
import '../../../core/widgets/empty_state.dart';
import '../../../core/widgets/error_state.dart';
import '../../../core/widgets/skeleton_loader.dart';
import '../domain/inbox_item.dart';
import 'inbox_cubit.dart';
import 'inbox_destination.dart';

/// The customer's notifications. Expects an [InboxCubit] above it — the same instance the bell reads, so the
/// badge and this list cannot disagree.
class InboxPage extends StatefulWidget {
  /// The clock the relative times are read against; the device clock unless a test fixes it.
  final DateTime Function() now;

  const InboxPage({super.key, this.now = DateTime.now});

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
    final theme = Theme.of(context);
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
                    // The theme's text-button ink is navy, which reads at 1.41:1 on the blue bar. On the bar a
                    // text button takes the bar's own foreground.
                    style: TextButton.styleFrom(
                      foregroundColor: theme.appBarTheme.foregroundColor ?? theme.colorScheme.onPrimary,
                    ),
                    child: const Text(AppStrings.notificationsMarkAllRead),
                  )
                : const SizedBox.shrink(),
          ),
        ],
      ),
      body: BlocBuilder<InboxCubit, InboxState>(
        builder: (context, state) {
          if (state.loading && !state.loaded) {
            return const _InboxSkeleton();
          }

          // A failure is shown as a failure, never as "you have no notifications".
          if (state.error != null && !state.loaded) {
            return ErrorState(
              key: const Key('inbox-error'),
              message: AppStrings.notificationsLoadFailed,
              onRetry: () => context.read<InboxCubit>().load(),
            );
          }

          if (state.isEmpty) {
            return const EmptyState(
              key: Key('inbox-empty'),
              icon: Icons.notifications_none_outlined,
              title: AppStrings.notificationsEmpty,
            );
          }

          final now = widget.now();
          return RefreshIndicator(
            onRefresh: () => context.read<InboxCubit>().load(),
            child: ListView.separated(
              itemCount: state.items.length,
              separatorBuilder: (_, __) => const Divider(height: 1),
              itemBuilder: (context, i) => _InboxRow(item: state.items[i], now: now, onTap: _open),
            ),
          );
        },
      ),
    );
  }
}

/// Row-shaped placeholders while the first page loads.
class _InboxSkeleton extends StatelessWidget {
  const _InboxSkeleton();

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      physics: const NeverScrollableScrollPhysics(),
      padding: const EdgeInsets.all(AppSpacing.md),
      child: SkeletonLoader.list(items: 6, itemHeight: 64),
    );
  }
}

class _InboxRow extends StatelessWidget {
  final InboxItem item;
  final DateTime now;
  final ValueChanged<InboxItem> onTap;

  const _InboxRow({required this.item, required this.now, required this.onTap});

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
      subtitle: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(item.body),
          const SizedBox(height: AppSpacing.xxs),
          Text(
            relativeTime(item.createdAt, now: now),
            key: Key('inbox-time-${item.id}'),
            style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
          ),
        ],
      ),
    );
  }
}
