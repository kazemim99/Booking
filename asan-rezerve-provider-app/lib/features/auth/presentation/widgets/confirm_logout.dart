import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_confirm_dialog.dart';
import '../bloc/auth_bloc.dart';
import '../bloc/auth_event.dart';

/// Shared logout confirmation for every app-bar logout action (wizard,
/// dashboard): shows [showAppConfirmDialog] and only dispatches
/// [LogoutRequested] if the destructive action was actually confirmed.
Future<void> confirmAndLogout(BuildContext context) async {
  final confirmed = await showAppConfirmDialog(
    context,
    title: AppStrings.logoutConfirmTitle,
    message: AppStrings.logoutConfirmBody,
    confirmLabel: AppStrings.logout,
  );
  if (confirmed == true && context.mounted) {
    context.read<AuthBloc>().add(const LogoutRequested());
  }
}
