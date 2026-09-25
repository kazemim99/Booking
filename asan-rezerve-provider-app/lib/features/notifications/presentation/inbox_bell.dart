import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../config/routes/app_router.dart';
import '../../../core/constants/app_strings.dart';
import 'inbox_cubit.dart';

/// The header bell. Replaces a placeholder that showed "coming soon" and — deliberately, per its own comment —
/// no count, because a guessed number would have been a lie. The number here is the server's.
class InboxBell extends StatefulWidget {
  final Color color;

  const InboxBell({super.key, this.color = Colors.white});

  @override
  State<InboxBell> createState() => _InboxBellState();
}

class _InboxBellState extends State<InboxBell> {
  @override
  void initState() {
    super.initState();
    context.read<InboxCubit>().refreshCount();
  }

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<InboxCubit, InboxState>(
      buildWhen: (a, b) => a.unreadCount != b.unreadCount,
      builder: (context, state) => IconButton(
        key: const Key('home-bell'),
        tooltip: AppStrings.notificationsTitle,
        onPressed: () => GoRouter.maybeOf(context)?.push(Routes.notifications),
        icon: Badge(
          key: const Key('home-bell-badge'),
          isLabelVisible: state.unreadCount > 0,
          label: Text(state.unreadCount > 99 ? '99+' : '${state.unreadCount}'),
          child: Icon(Icons.notifications_none, color: widget.color),
        ),
      ),
    );
  }
}
