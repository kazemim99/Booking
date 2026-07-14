import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../auth/domain/entities/user.dart';
import '../../../auth/presentation/bloc/auth_bloc.dart';
import '../../../auth/presentation/bloc/auth_event.dart';
import '../../../auth/presentation/bloc/auth_state.dart';
import '../../../auth/presentation/pages/login_page.dart';
import '../bloc/profile_cubit.dart';

/// Profile tab: shows the login screen in place for guests and swaps to
/// the profile once authenticated (no navigation jank — same tab slot).
class ProfileTabPage extends StatelessWidget {
  const ProfileTabPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<AuthBloc, AuthState>(
      buildWhen: (prev, next) =>
          next is Authenticated || next is Unauthenticated || next is LoggedOut,
      builder: (context, state) {
        if (state is Authenticated) {
          return ProfilePage(user: state.session.user);
        }
        return const LoginPage(embedded: true);
      },
    );
  }
}

/// Authenticated profile: identity card, edit-profile sheet, logout with
/// confirmation.
class ProfilePage extends StatelessWidget {
  final User user;

  const ProfilePage({super.key, required this.user});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => ProfileCubit(
        remoteDataSource: getIt(),
        storageService: getIt(),
        initialFirstName: user.firstName,
        initialLastName: user.lastName,
      ),
      child: BlocListener<ProfileCubit, ProfileState>(
        listenWhen: (prev, next) => prev.editStatus != next.editStatus,
        listener: (context, state) {
          if (state.editStatus == ProfileEditStatus.success) {
            AppSnackbar.success(context, AppStrings.profileUpdated);
          } else if (state.editStatus == ProfileEditStatus.failure) {
            AppSnackbar.error(
              context,
              state.errorMessage ?? AppStrings.genericError,
            );
          }
        },
        child: const _ProfileView(),
      ),
    );
  }
}

class _ProfileView extends StatelessWidget {
  const _ProfileView();

  Future<void> _editProfile(BuildContext context) async {
    final cubit = context.read<ProfileCubit>();
    final firstController =
        TextEditingController(text: cubit.state.firstName ?? '');
    final lastController =
        TextEditingController(text: cubit.state.lastName ?? '');

    await AppBottomSheet.show<void>(
      context: context,
      title: AppStrings.profileEditTitle,
      isScrollControlled: true,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          AppTextField(
            controller: firstController,
            label: AppStrings.firstNameLabel,
            autofocus: true,
          ),
          const SizedBox(height: AppSpacing.sm),
          AppTextField(
            controller: lastController,
            label: AppStrings.lastNameLabel,
          ),
          const SizedBox(height: AppSpacing.lg),
          AppButton(
            label: AppStrings.save,
            onPressed: () {
              Navigator.of(context).pop();
              cubit.saveProfile(
                firstName: firstController.text.trim(),
                lastName: lastController.text.trim(),
              );
            },
          ),
        ],
      ),
    );

    firstController.dispose();
    lastController.dispose();
  }

  Future<void> _logout(BuildContext context) async {
    final authBloc = context.read<AuthBloc>();
    final confirmed = await ConfirmSheet.show(
      context: context,
      title: AppStrings.logoutConfirmTitle,
      body: AppStrings.logoutConfirmBody,
      confirmLabel: AppStrings.logout,
      destructive: true,
    );
    if (confirmed) {
      authBloc.add(const LogoutEvent());
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final authState = context.watch<AuthBloc>().state;
    final user = authState is Authenticated ? authState.session.user : null;

    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.profileTitle)),
      body: BlocBuilder<ProfileCubit, ProfileState>(
        builder: (context, state) {
          final displayName = [state.firstName, state.lastName]
              .whereType<String>()
              .where((p) => p.isNotEmpty)
              .join(' ');

          return ListView(
            padding: const EdgeInsets.all(AppSpacing.md),
            children: [
              AppCard(
                child: Row(
                  children: [
                    CircleAvatar(
                      radius: 28,
                      backgroundColor:
                          theme.colorScheme.primary.withValues(alpha: 0.1),
                      child: Icon(
                        Icons.person_outline,
                        size: 32,
                        color: theme.colorScheme.primary,
                      ),
                    ),
                    const SizedBox(width: AppSpacing.md),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          if (displayName.isNotEmpty)
                            Text(
                              displayName,
                              style: theme.textTheme.titleMedium,
                            ),
                          Directionality(
                            textDirection: TextDirection.ltr,
                            child: Text(
                              user?.phoneNumber ?? '',
                              style: theme.textTheme.bodyMedium,
                              textAlign: TextAlign.right,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.md),
              AppCard(
                padding: EdgeInsets.zero,
                child: Column(
                  children: [
                    ListTile(
                      leading: const Icon(Icons.edit_outlined),
                      title: const Text(AppStrings.profileEditTitle),
                      trailing: state.editStatus == ProfileEditStatus.saving
                          ? const SizedBox(
                              width: 18,
                              height: 18,
                              child:
                                  CircularProgressIndicator(strokeWidth: 2),
                            )
                          // Direction-aware: points "forward" in RTL and LTR.
                          : Icon(
                              Directionality.of(context) == TextDirection.rtl
                                  ? Icons.chevron_left
                                  : Icons.chevron_right,
                            ),
                      onTap: state.editStatus == ProfileEditStatus.saving
                          ? null
                          : () => _editProfile(context),
                    ),
                    const Divider(),
                    ListTile(
                      leading: Icon(
                        Icons.logout,
                        color: theme.colorScheme.error,
                      ),
                      title: Text(
                        AppStrings.logout,
                        style: TextStyle(color: theme.colorScheme.error),
                      ),
                      onTap: () => _logout(context),
                    ),
                  ],
                ),
              ),
            ],
          );
        },
      ),
    );
  }
}
