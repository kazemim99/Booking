import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/persian_digits.dart';
import '../../../../core/utils/persian_text.dart';
import '../../../../core/widgets/app_list_row.dart';
import '../../../../core/widgets/app_section_header.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../../domain/entities/onboarding_data.dart';
import '../../domain/entities/service_catalog.dart';
import '../cubit/onboarding_cubit.dart';
import '../cubit/onboarding_state.dart';
import '../widgets/step_scaffold.dart';

/// Step 4 — services. At least one service is required.
///
/// Built around a catalogue of what salons of the chosen kind usually offer, so
/// the provider ticks rather than types: a barbershop has ~30 usual services and
/// a women's salon ~60, and this used to be one dialog each. A ticked row opens
/// its price and duration inline — editing where the eye already is, and no
/// sixty text fields on one screen. Anything unusual still goes through
/// "خدمت دلخواه".
class ServicesStep extends StatefulWidget {
  const ServicesStep({super.key});

  @override
  State<ServicesStep> createState() => _ServicesStepState();
}

class _ServicesStepState extends State<ServicesStep> {
  final _search = TextEditingController();

  /// Price/duration editors for ticked rows, kept per service name so the text
  /// (and caret) survives rebuilds while the user types.
  final Map<String, TextEditingController> _priceFields = {};
  final Map<String, TextEditingController> _durationFields = {};

  @override
  void dispose() {
    _search.dispose();
    for (final c in _priceFields.values) {
      c.dispose();
    }
    for (final c in _durationFields.values) {
      c.dispose();
    }
    super.dispose();
  }

  List<ServiceDraft> get _services =>
      context.read<OnboardingCubit>().state.data.services;

  ServiceDraft? _selected(String name) {
    for (final s in _services) {
      if (s.name == name) return s;
    }
    return null;
  }

  void _toggle(ServicePreset preset, bool select) {
    final cubit = context.read<OnboardingCubit>();
    final next = [..._services]..removeWhere((s) => s.name == preset.name);
    if (select) {
      next.add(
        ServiceDraft(
          name: preset.name,
          durationHours: preset.minutes ~/ 60,
          durationMinutes: preset.minutes % 60,
          price: preset.price,
        ),
      );
    } else {
      _priceFields.remove(preset.name)?.dispose();
      _durationFields.remove(preset.name)?.dispose();
    }
    cubit.setServices(next);
  }

  void _update(String name, {double? price, int? minutes}) {
    final cubit = context.read<OnboardingCubit>();
    final next = [
      for (final s in _services)
        if (s.name != name)
          s
        else
          ServiceDraft(
            name: s.name,
            durationHours:
                (minutes ?? s.durationHours * 60 + s.durationMinutes) ~/ 60,
            durationMinutes:
                (minutes ?? s.durationHours * 60 + s.durationMinutes) % 60,
            price: price ?? s.price,
            priceType: s.priceType,
          ),
    ];
    cubit.setServices(next);
  }

  Future<void> _addCustomService() async {
    final service = await showDialog<ServiceDraft>(
      context: context,
      builder: (_) => const _ServiceFormDialog(),
    );
    if (service != null && mounted) {
      final cubit = context.read<OnboardingCubit>();
      cubit.setServices([..._services, service]);
    }
  }

  @override
  Widget build(BuildContext context) {
    final cubit = context.read<OnboardingCubit>();
    return BlocBuilder<OnboardingCubit, OnboardingState>(
      builder: (context, state) {
        final catalogue = ServiceCatalog.forCategory(state.data.categoryId);
        final query = _search.text.trim();
        final catalogueNames = {
          for (final g in catalogue)
            for (final s in g.services) s.name,
        };
        final custom = state.data.services
            .where((s) => !catalogueNames.contains(s.name))
            .toList();

        return StepScaffold(
          title: AppStrings.servicesTitle,
          subtitle: AppStrings.servicesSubtitle,
          loading: state.isSaving,
          onBack: cubit.back,
          onNext: cubit.next,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              AppSectionHeader(
                title: AppStrings.customServices,
                action: AppInlineAddButton(
                  key: const Key('onboarding-add-service'),
                  label: AppStrings.addService,
                  onPressed: _addCustomService,
                ),
              ),
              const SizedBox(height: AppSpacing.sm),
              if (custom.isEmpty && catalogue.isEmpty)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: AppSpacing.lg),
                  child: Text(
                    AppStrings.noServicesYet,
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.bodyMedium,
                  ),
                ),
              ...custom.map(
                (s) => Padding(
                  padding: const EdgeInsets.only(bottom: AppSpacing.sm),
                  child: AppListRow(
                    leadingIcon: Icons.design_services_outlined,
                    title: s.name,
                    subtitle:
                        '${s.durationHours * 60 + s.durationMinutes} '
                        'دقیقه · ${PriceText.format(s.price)} تومان',
                    trailing: IconButton(
                      icon: const Icon(Icons.delete_outline),
                      onPressed: () => cubit.setServices(
                        [..._services]..removeWhere((x) => x.name == s.name),
                      ),
                    ),
                  ),
                ),
              ),
              const SizedBox(height: AppSpacing.md),
              if (catalogue.isNotEmpty) ...[
                AppTextField(
                  controller: _search,
                  label: AppStrings.searchServices,
                  hint: AppStrings.searchServicesHint,
                  prefixIcon: Icons.search,
                  onChanged: (_) => setState(() {}),
                  key: const Key('onboarding-service-search'),
                ),
                const SizedBox(height: AppSpacing.sm),
                Text(
                  state.data.services.isEmpty
                      ? AppStrings.servicesPickHint
                      : AppStrings.servicesSelectedCount(
                          state.data.services.length,
                        ),
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: Theme.of(context).colorScheme.onSurfaceVariant,
                  ),
                ),
                const SizedBox(height: AppSpacing.md),
                ...catalogue.map((group) => _buildGroup(group, query)),
                const SizedBox(height: AppSpacing.md),
              ],
            ],
          ),
        );
      },
    );
  }

  Widget _buildGroup(ServiceCatalogGroup group, String query) {
    final matches = query.isEmpty
        ? group.services
        : group.services
              .where((s) => PersianText.contains(s.name, query))
              .toList();
    if (matches.isEmpty) return const SizedBox.shrink();

    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: Theme(
        // A plain divider-less section: the groups are content, not settings.
        data: Theme.of(context).copyWith(dividerColor: Colors.transparent),
        child: ExpansionTile(
          key: PageStorageKey(group.title),
          title: Text(
            group.title,
            style: Theme.of(context).textTheme.titleSmall,
          ),
          // While searching, everything matching is open — a closed group would
          // hide the very row the user searched for.
          initiallyExpanded: query.isNotEmpty || group == _firstGroup,
          tilePadding: EdgeInsets.zero,
          childrenPadding: EdgeInsets.zero,
          children: matches.map(_buildPresetRow).toList(),
        ),
      ),
    );
  }

  ServiceCatalogGroup? get _firstGroup {
    final catalogue = ServiceCatalog.forCategory(
      context.read<OnboardingCubit>().state.data.categoryId,
    );
    return catalogue.isEmpty ? null : catalogue.first;
  }

  Widget _buildPresetRow(ServicePreset preset) {
    final selected = _selected(preset.name);
    final theme = Theme.of(context);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        CheckboxListTile(
          key: Key('service-preset-${preset.name}'),
          value: selected != null,
          onChanged: (checked) => _toggle(preset, checked ?? false),
          controlAffinity: ListTileControlAffinity.leading,
          contentPadding: EdgeInsets.zero,
          dense: true,
          title: Text(preset.name),
          subtitle: Text(
            preset.unitNote == null
                ? '${AppStrings.suggestedPrice}: ${PriceText.format(preset.price)} ${AppStrings.toman}'
                : '${AppStrings.suggestedPrice}: ${PriceText.format(preset.price)} ${AppStrings.toman} (${preset.unitNote})',
            style: theme.textTheme.bodySmall?.copyWith(
              color: theme.colorScheme.onSurfaceVariant,
            ),
          ),
        ),
        if (selected != null)
          Padding(
            padding: const EdgeInsets.only(
              bottom: AppSpacing.sm,
              right: AppSpacing.lg,
            ),
            child: Row(
              children: [
                Expanded(
                  flex: 2,
                  child: AppTextField(
                    key: Key('service-price-${preset.name}'),
                    controller: _priceController(preset.name, selected.price),
                    label: AppStrings.servicePrice,
                    keyboardType: TextInputType.number,
                    inputFormatters: const [ThousandsSeparatorInputFormatter()],
                    onChanged: (value) => _update(
                      preset.name,
                      price: PriceText.parse(value) ?? 0,
                    ),
                  ),
                ),
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: AppTextField(
                    key: Key('service-duration-${preset.name}'),
                    controller: _durationController(
                      preset.name,
                      selected.durationHours * 60 + selected.durationMinutes,
                    ),
                    label: AppStrings.serviceDuration,
                    keyboardType: TextInputType.number,
                    inputFormatters: const [DigitsOnlyInputFormatter()],
                    onChanged: (value) =>
                        _update(preset.name, minutes: int.tryParse(value) ?? 0),
                  ),
                ),
              ],
            ),
          ),
      ],
    );
  }

  TextEditingController _priceController(String name, double price) =>
      _priceFields.putIfAbsent(
        name,
        () => TextEditingController(text: PriceText.format(price)),
      );

  TextEditingController _durationController(String name, int minutes) =>
      _durationFields.putIfAbsent(
        name,
        () => TextEditingController(text: '$minutes'),
      );
}

/// Adds a service the catalogue does not list.
///
/// Every required field says so in its own label and under its own box: a single
/// "fill in all the fields" line left the user hunting for which one was empty.
class _ServiceFormDialog extends StatefulWidget {
  const _ServiceFormDialog();

  @override
  State<_ServiceFormDialog> createState() => _ServiceFormDialogState();
}

class _ServiceFormDialogState extends State<_ServiceFormDialog> {
  final _name = TextEditingController();
  final _duration = TextEditingController(text: '30');
  final _price = TextEditingController();

  final Set<String> _touched = {};
  bool _submitted = false;

  @override
  void dispose() {
    _name.dispose();
    _duration.dispose();
    _price.dispose();
    super.dispose();
  }

  String? _validate(String field) => switch (field) {
    'name' => _name.text.trim().isEmpty ? AppStrings.fieldRequired : null,
    'duration' =>
      (int.tryParse(_duration.text.trim()) ?? 0) <= 0
          ? AppStrings.fieldRequired
          : null,
    'price' =>
      PriceText.parse(_price.text) == null ? AppStrings.fieldRequired : null,
    _ => null,
  };

  String? _errorFor(String field) =>
      (_submitted || _touched.contains(field)) ? _validate(field) : null;

  void _submit() {
    if (['name', 'duration', 'price'].any((f) => _validate(f) != null)) {
      setState(() => _submitted = true);
      return;
    }

    final minutes = int.parse(_duration.text.trim());
    Navigator.of(context).pop(
      ServiceDraft(
        name: _name.text.trim(),
        durationHours: minutes ~/ 60,
        durationMinutes: minutes % 60,
        price: PriceText.parse(_price.text)!,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text(AppStrings.addService),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            AppTextField(
              controller: _name,
              key: const Key('service-name'),
              label: AppStrings.serviceName,
              isRequired: true,
              errorText: _errorFor('name'),
              onBlur: () => setState(() => _touched.add('name')),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: AppSpacing.sm),
            AppTextField(
              controller: _duration,
              key: const Key('service-duration'),
              label: AppStrings.serviceDuration,
              isRequired: true,
              keyboardType: TextInputType.number,
              inputFormatters: const [DigitsOnlyInputFormatter()],
              errorText: _errorFor('duration'),
              onBlur: () => setState(() => _touched.add('duration')),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: AppSpacing.sm),
            AppTextField(
              controller: _price,
              key: const Key('service-price'),
              label: AppStrings.servicePrice,
              isRequired: true,
              keyboardType: TextInputType.number,
              inputFormatters: const [ThousandsSeparatorInputFormatter()],
              errorText: _errorFor('price'),
              onBlur: () => setState(() => _touched.add('price')),
              onChanged: (_) => setState(() {}),
            ),
          ],
        ),
      ),
      // One row, like the wizard's footer: this app's buttons are full-width by
      // theme, so the default actions layout stacked cancel above save.
      actions: [
        Row(
          children: [
            Expanded(
              child: OutlinedButton(
                onPressed: () => Navigator.of(context).pop(),
                child: const Text(AppStrings.cancel),
              ),
            ),
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: FilledButton(
                key: const Key('service-save'),
                onPressed: _submit,
                child: const Text(AppStrings.save),
              ),
            ),
          ],
        ),
      ],
    );
  }
}
