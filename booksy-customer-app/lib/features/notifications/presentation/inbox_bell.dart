import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../config/routes/app_router.dart';
import '../../../core/constants/app_strings.dart';
import '../../../core/widgets/app_circle_icon_button.dart';
import 'inbox_cubit.dart';

/// The home header bell, shown only to a signed-in customer (a guest has no inbox). The number is the server's;
/// when it cannot be read the badge shows nothing rather than a guess.
class InboxBell extends StatefulWidget {
  const InboxBell({super.key});

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
      builder: (context, state) => Badge(
        key: const Key('home-bell-badge'),
        isLabelVisible: state.unreadCount > 0,
        label: Text(state.unreadCount > 99 ? '99+' : '${state.unreadCount}'),
        child: AppCircleIconButton(
          key: const Key('home-bell'),
          icon: Icons.notifications_none,
          semanticLabel: AppStrings.notificationsTitle,
          onPressed: () => GoRouter.maybeOf(context)?.push(Routes.notifications),
        ),
      ),
    );
  }
}
