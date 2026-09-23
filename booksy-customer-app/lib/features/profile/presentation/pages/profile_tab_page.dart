import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import '../../../../config/routes/app_router.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/utils/person_name.dart';
import '../../../../core/widgets/forward_chevron.dart';
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
    // A skipped name prompt leaves «مشتری 9384444636»: neither the card nor the edit form may offer it back as a
    // name ("the number must never be written anywhere", production QA 2026-09-23).
    final name = realNameParts(user.firstName, user.lastName);
    return BlocProvider(
      create: (_) => ProfileCubit(
        remoteDataSource: getIt(),
        storageService: getIt(),
        initialFirstName: name.first,
        initialLastName: name.last,
      ),
      child: BlocListener<ProfileCubit, ProfileState>(
        listenWhen: (prev, next) => prev.editStatus != next.editStatus,
        listener: (context, state) {
          if (state.editStatus == ProfileEditStatus.success) {
            // The session follows, so the booking confirm step knows the name.
            context.read<AuthBloc>().add(UserNameChangedEvent(
                  firstName: state.firstName ?? '',
                  lastName: state.lastName ?? '',
                ));
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

/// The edit sheet's fields; closes with the trimmed names, or with nothing when dismissed.
///
/// The controllers belong to this widget, so they are disposed with it — after the sheet's closing animation. They
/// used to be disposed the moment the sheet's future completed, while the closing sheet still built its fields
/// ("A TextEditingController was used after being disposed").
class _EditNameForm extends StatefulWidget {
  final String firstName;
  final String lastName;

  const _EditNameForm({required this.firstName, required this.lastName});

  @override
  State<_EditNameForm> createState() => _EditNameFormState();
}

class _EditNameFormState extends State<_EditNameForm> {
  late final _first = TextEditingController(text: widget.firstName);
  late final _last = TextEditingController(text: widget.lastName);

  @override
  void dispose() {
    _first.dispose();
    _last.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        AppTextField(
          controller: _first,
          label: AppStrings.firstNameLabel,
          autofocus: true,
        ),
        const SizedBox(height: AppSpacing.sm),
        AppTextField(
          controller: _last,
          label: AppStrings.lastNameLabel,
        ),
        const SizedBox(height: AppSpacing.lg),
        AppButton(
          label: AppStrings.save,
          onPressed: () => Navigator.of(context).pop((
            first: _first.text.trim(),
            last: _last.text.trim(),
          )),
        ),
      ],
    );
  }
}

class _ProfileView extends StatelessWidget {
  const _ProfileView();

  Future<void> _editProfile(BuildContext context) async {
    final cubit = context.read<ProfileCubit>();
    final name = await AppBottomSheet.show<({String first, String last})>(
      context: context,
      title: AppStrings.profileEditTitle,
      isScrollControlled: true,
      child: _EditNameForm(
        firstName: cubit.state.firstName ?? '',
        lastName: cubit.state.lastName ?? '',
      ),
    );
    if (name == null) return;
    await cubit.saveProfile(firstName: name.first, lastName: name.last);
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
          final displayName =
              realNameOrNull(state.firstName, state.lastName) ?? '';

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
                          : const ForwardChevron(),
                      onTap: state.editStatus == ProfileEditStatus.saving
                          ? null
                          : () => _editProfile(context),
                    ),
                    const Divider(),
                    // The bell lives in the Home chrome only, so from any other tab there was no way to reach
                    // the inbox at all — "where do I see a notification if one arrives?" (QA 2026-09-22).
                    ListTile(
                      key: const Key('profile-notifications'),
                      leading: const Icon(Icons.notifications_none),
                      title: const Text(AppStrings.notificationsTitle),
                      trailing: const ForwardChevron(),
                      onTap: () => context.push(Routes.notifications),
                    ),
                    const Divider(),
                    ListTile(
                      key: const Key('profile-my-reviews'),
                      leading: const Icon(Icons.rate_review_outlined),
                      title: const Text(AppStrings.myReviewsTitle),
                      trailing: const ForwardChevron(),
                      onTap: () => context.push(Routes.myReviews),
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
