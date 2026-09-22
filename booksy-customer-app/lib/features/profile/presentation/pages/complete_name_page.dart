import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/widgets.dart';
import '../bloc/profile_cubit.dart';

/// Asks a customer for their name, once, right after they sign up.
///
/// Signing up is only a phone number, so the account is stored as «مشتری 9384444636» — the word for their side
/// of the marketplace and their digits. That is what the salon then sees on the booking, and what the app shows
/// them on their own profile (QA walkthrough 2026-09-22). Nothing ever asked, so nothing was ever filled in.
///
/// Skipping is allowed: a name is worth asking for once, never worth blocking a booking over.
class CompleteNamePage extends StatefulWidget {
  /// Where to go once the name is saved or skipped.
  final String? redirect;

  /// Cubit override for tests; resolved from DI in the app.
  final ProfileCubit? cubit;

  const CompleteNamePage({super.key, this.redirect, this.cubit});

  @override
  State<CompleteNamePage> createState() => _CompleteNamePageState();
}

class _CompleteNamePageState extends State<CompleteNamePage> {
  final _first = TextEditingController();
  final _last = TextEditingController();
  String? _error;

  @override
  void dispose() {
    _first.dispose();
    _last.dispose();
    super.dispose();
  }

  void _continue() {
    final destination = widget.redirect == null || widget.redirect!.isEmpty
        ? Routes.home
        : Uri.decodeComponent(widget.redirect!);
    context.go(destination);
  }

  void _save(ProfileCubit cubit) {
    final first = _first.text.trim();
    final last = _last.text.trim();
    if (first.isEmpty) {
      setState(() => _error = AppStrings.firstNameRequired);
      return;
    }
    cubit.saveProfile(firstName: first, lastName: last);
    _continue();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final cubit = widget.cubit ??
        ProfileCubit(remoteDataSource: getIt(), storageService: getIt());
    return BlocProvider<ProfileCubit>.value(
      value: cubit,
      child: Scaffold(
        appBar: AppBar(
          title: const Text(AppStrings.completeNameTitle),
          actions: [
            TextButton(
              key: const Key('complete-name-skip'),
              onPressed: _continue,
              child: const Text(AppStrings.completeNameSkip),
            ),
          ],
        ),
        body: SafeArea(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(AppSpacing.md),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(AppStrings.completeNameSubtitle, style: theme.textTheme.bodyMedium),
                const SizedBox(height: AppSpacing.md),
                AppTextField(
                  key: const Key('complete-name-first'),
                  controller: _first,
                  label: AppStrings.firstNameLabel,
                  autofocus: true,
                ),
                const SizedBox(height: AppSpacing.sm),
                AppTextField(
                  key: const Key('complete-name-last'),
                  controller: _last,
                  label: AppStrings.lastNameLabel,
                ),
                if (_error != null) ...[
                  const SizedBox(height: AppSpacing.xs),
                  Text(
                    _error!,
                    key: const Key('complete-name-error'),
                    style: theme.textTheme.bodySmall
                        ?.copyWith(color: theme.colorScheme.error),
                  ),
                ],
                const SizedBox(height: AppSpacing.lg),
                AppButton(
                  key: const Key('complete-name-save'),
                  label: AppStrings.save,
                  onPressed: () => _save(cubit),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
