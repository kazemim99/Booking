import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/widgets/app_button.dart';
import '../../../../core/widgets/app_page_scaffold.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../../domain/repositories/auth_repository.dart';
import '../bloc/auth_bloc.dart';
import '../bloc/auth_event.dart';

/// Asks the salon's person for their name, once, right after they sign in with a phone number — the way the
/// customer app does after sign-up.
///
/// Phone sign-in stores the account as «ارائه‌دهنده 9123135143», and nothing ever asked for a real one, so the app
/// showed the owner as «09123135143» and customers saw the placeholder on the booking confirm step (production
/// QA 2026-09-23). The router brings an established account here when it leaves the OTP screen without a real name;
/// an account still onboarding is not brought here — the wizard asks for the owner's name itself.
///
/// Skipping is allowed: a name is worth asking for once, never worth blocking the salon's day over. It can be set
/// any time from More → نام شما.
class CompleteNamePage extends StatefulWidget {
  /// Where to go once the name is saved or skipped (URI-encoded, as the router hands it over).
  final String? redirect;

  /// Repository override for tests; resolved from DI in the app.
  final AuthRepository? repository;

  const CompleteNamePage({super.key, this.redirect, this.repository});

  @override
  State<CompleteNamePage> createState() => _CompleteNamePageState();
}

class _CompleteNamePageState extends State<CompleteNamePage> {
  final _first = TextEditingController();
  final _last = TextEditingController();
  String? _error;
  bool _saving = false;

  AuthRepository get _repository => widget.repository ?? getIt<AuthRepository>();

  @override
  void dispose() {
    _first.dispose();
    _last.dispose();
    super.dispose();
  }

  void _continue() {
    final target = widget.redirect;
    context.go(target == null || target.isEmpty ? Routes.dashboard : Uri.decodeComponent(target));
  }

  /// Leaves only once the name is saved; a failure is said here, with what they typed still in the fields.
  Future<void> _save() async {
    final first = _first.text.trim();
    final last = _last.text.trim();
    if (first.isEmpty) {
      setState(() => _error = AppStrings.fieldRequired);
      return;
    }
    setState(() {
      _error = null;
      _saving = true;
    });

    final result = await _repository.updateMyName(firstName: first, lastName: last);
    if (!mounted) return;
    result.fold(
      (_) => setState(() {
        _saving = false;
        _error = AppStrings.completeNameSaveFailed;
      }),
      (_) {
        // The saved name is in the refreshed token; re-read the session so every header shows it at once.
        context.read<AuthBloc>().add(const ProviderStatusRefreshRequested());
        _continue();
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return PopScope(
      // Leaving mid-save would abandon it, its answer unheard.
      canPop: !_saving,
      child: AppPageScaffold(
        title: AppStrings.completeNameTitle,
        automaticallyImplyLeading: false,
        actions: [
          TextButton(
            key: const Key('complete-name-skip'),
            onPressed: _saving ? null : _continue,
            // The only way on without a name, on the blue bar: it takes the bar's own foreground.
            style: TextButton.styleFrom(foregroundColor: Colors.white),
            child: const Text(AppStrings.completeNameSkip),
          ),
        ],
        body: SafeArea(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(AppSpacing.md),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const Text(
                  AppStrings.completeNameSubtitle,
                  style: TextStyle(fontSize: 14, color: AppColors.ink),
                ),
                const SizedBox(height: AppSpacing.md),
                AppTextField(
                  key: const Key('complete-name-first'),
                  controller: _first,
                  label: AppStrings.customerFirstName,
                ),
                const SizedBox(height: AppSpacing.sm),
                AppTextField(
                  key: const Key('complete-name-last'),
                  controller: _last,
                  label: AppStrings.customerLastName,
                ),
                if (_error != null) ...[
                  const SizedBox(height: AppSpacing.xs),
                  Text(
                    _error!,
                    key: const Key('complete-name-error'),
                    style: const TextStyle(fontSize: 13, color: AppColors.inputError),
                  ),
                ],
                const SizedBox(height: AppSpacing.lg),
                AppButton(
                  key: const Key('complete-name-save'),
                  label: AppStrings.save,
                  loading: _saving,
                  onPressed: _saving ? null : _save,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
