import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/persian_digits.dart';
import '../../domain/entities/onboarding_data.dart';
import '../cubit/onboarding_cubit.dart';
import '../cubit/onboarding_state.dart';
import '../widgets/step_scaffold.dart';

/// Step 4 — services. At least one service is required.
class ServicesStep extends StatelessWidget {
  const ServicesStep({super.key});

  Future<void> _addService(BuildContext context) async {
    final service = await showDialog<ServiceDraft>(
      context: context,
      builder: (_) => const _ServiceFormDialog(),
    );
    if (service != null && context.mounted) {
      final cubit = context.read<OnboardingCubit>();
      cubit.setServices([...cubit.state.data.services, service]);
    }
  }

  @override
  Widget build(BuildContext context) {
    final cubit = context.read<OnboardingCubit>();
    return BlocBuilder<OnboardingCubit, OnboardingState>(
      builder: (context, state) {
        final services = state.data.services;
        return StepScaffold(
          title: AppStrings.servicesTitle,
          subtitle: AppStrings.servicesSubtitle,
          loading: state.isSaving,
          onBack: cubit.back,
          onNext: cubit.next,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (services.isEmpty)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: AppSpacing.lg),
                  child: Text(
                    AppStrings.noServicesYet,
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.bodyMedium,
                  ),
                )
              else
                ...services.asMap().entries.map((entry) {
                  final i = entry.key;
                  final s = entry.value;
                  return Card(
                    child: ListTile(
                      title: Text(s.name),
                      subtitle: Text(
                        '${s.durationHours * 60 + s.durationMinutes} دقیقه · '
                        '${s.price.toStringAsFixed(0)} تومان',
                      ),
                      trailing: IconButton(
                        icon: const Icon(Icons.delete_outline),
                        onPressed: () {
                          final next = [...services]..removeAt(i);
                          cubit.setServices(next);
                        },
                      ),
                    ),
                  );
                }),
              const SizedBox(height: AppSpacing.md),
              OutlinedButton.icon(
                key: const Key('onboarding-add-service'),
                onPressed: () => _addService(context),
                icon: const Icon(Icons.add),
                label: const Text(AppStrings.addService),
              ),
            ],
          ),
        );
      },
    );
  }
}

class _ServiceFormDialog extends StatefulWidget {
  const _ServiceFormDialog();

  @override
  State<_ServiceFormDialog> createState() => _ServiceFormDialogState();
}

class _ServiceFormDialogState extends State<_ServiceFormDialog> {
  final _name = TextEditingController();
  final _duration = TextEditingController(text: '30');
  final _price = TextEditingController();
  String? _error;

  @override
  void dispose() {
    _name.dispose();
    _duration.dispose();
    _price.dispose();
    super.dispose();
  }

  void _submit() {
    final name = _name.text.trim();
    final minutes = int.tryParse(_duration.text.trim()) ?? 0;
    final price = double.tryParse(_price.text.trim()) ?? -1;

    if (name.isEmpty || minutes <= 0 || price < 0) {
      setState(() => _error = 'لطفاً تمام فیلدها را درست وارد کنید');
      return;
    }

    Navigator.of(context).pop(
      ServiceDraft(
        name: name,
        durationHours: minutes ~/ 60,
        durationMinutes: minutes % 60,
        price: price,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text(AppStrings.addService),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          TextField(
            controller: _name,
            key: const Key('service-name'),
            decoration: const InputDecoration(labelText: AppStrings.serviceName),
          ),
          const SizedBox(height: AppSpacing.sm),
          TextField(
            controller: _duration,
            key: const Key('service-duration'),
            keyboardType: TextInputType.number,
            inputFormatters: const [DigitsOnlyInputFormatter()],
            decoration:
                const InputDecoration(labelText: AppStrings.serviceDuration),
          ),
          const SizedBox(height: AppSpacing.sm),
          TextField(
            controller: _price,
            key: const Key('service-price'),
            keyboardType: TextInputType.number,
            inputFormatters: const [DigitsOnlyInputFormatter()],
            decoration: const InputDecoration(labelText: AppStrings.servicePrice),
          ),
          if (_error != null) ...[
            const SizedBox(height: AppSpacing.sm),
            Text(
              _error!,
              style: TextStyle(color: Theme.of(context).colorScheme.error),
            ),
          ],
        ],
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text(AppStrings.cancel),
        ),
        FilledButton(
          key: const Key('service-save'),
          onPressed: _submit,
          child: const Text(AppStrings.save),
        ),
      ],
    );
  }
}
