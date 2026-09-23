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
  late final ProfileCubit _cubit;
  String? _error;
  bool _saving = false;

  /// The save on its way, if any. The page's own cubit is closed only once it is done: the page can still be
  /// taken away mid-save (a sign-out redirect), and a closed cubit cannot report the answer.
  Future<void>? _inFlight;

  @override
  void initState() {
    super.initState();
    // Made once, not per build: a rebuild while saving must not swap the cubit out from under the save.
    _cubit = widget.cubit ??
        ProfileCubit(remoteDataSource: getIt(), storageService: getIt());
  }

  @override
  void dispose() {
    if (widget.cubit == null) {
      final inFlight = _inFlight;
      if (inFlight == null) {
        _cubit.close();
      } else {
        inFlight.whenComplete(_cubit.close).ignore();
      }
    }
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

  /// Leaves only once the name is saved. A failure is said here, with what they typed still in the fields —
  /// moving on regardless would drop the name without a word (UX review 2026-09-23, G.4).
  Future<void> _save() async {
    final first = _first.text.trim();
    final last = _last.text.trim();
    if (first.isEmpty) {
      setState(() => _error = AppStrings.firstNameRequired);
      return;
    }
    setState(() {
      _error = null;
      _saving = true;
    });
    final saving = _cubit.saveProfile(firstName: first, lastName: last);
    _inFlight = saving;
    try {
      await saving;
    } finally {
      if (identical(_inFlight, saving)) _inFlight = null;
    }
    // Gone while saving: nobody is left to carry on or to tell.
    if (!mounted || _cubit.isClosed) return;
    if (_cubit.state.editStatus == ProfileEditStatus.success) {
      _continue();
      return;
    }
    setState(() {
      _saving = false;
      _error = AppStrings.completeNameSaveFailed;
    });
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return BlocProvider<ProfileCubit>.value(
      value: _cubit,
      // Leaving now would abandon a save half-way, its answer unheard; it takes a moment, then either
      // path is open again.
      child: PopScope(
        canPop: !_saving,
        child: Scaffold(
          appBar: AppBar(
            title: const Text(AppStrings.completeNameTitle),
            actions: [
              TextButton(
                key: const Key('complete-name-skip'),
                onPressed: _saving ? null : _continue,
                // The theme's text-button ink is navy, which reads at 1.41:1 on the blue bar; this is the only
                // way to skip, so it takes the bar's own foreground.
                style: TextButton.styleFrom(
                  foregroundColor: theme.appBarTheme.foregroundColor ?? theme.colorScheme.onPrimary,
                ),
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
                      style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.error),
                    ),
                  ],
                  const SizedBox(height: AppSpacing.lg),
                  AppButton(
                    key: const Key('complete-name-save'),
                    label: AppStrings.save,
                    loading: _saving,
                    onPressed: _save,
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
