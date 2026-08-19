import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/jalali_formatter.dart';
import '../../../../core/widgets/widgets.dart';
import '../../domain/entities/booking_entities.dart';
import '../bloc/booking_bloc.dart';

/// The booking flow's first step: pick the services for one visit.
///
/// Multi-select — a visit can bundle a cut and a colour. A pinned summary keeps
/// the running total duration and price visible while the customer builds the
/// selection, because both change the slot they will be offered next. Continue
/// stays disabled until at least one service is picked.
class ServiceSelectionStep extends StatelessWidget {
  final BookingState state;

  const ServiceSelectionStep({super.key, required this.state});

  @override
  Widget build(BuildContext context) {
    final services = state.provider?.services ?? const <ServiceItem>[];
    if (services.isEmpty) {
      return const EmptyState(
        icon: Icons.design_services_outlined,
        title: AppStrings.noResultsTitle,
      );
    }

    final bloc = context.read<BookingBloc>();

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(
            AppSpacing.md,
            AppSpacing.md,
            AppSpacing.md,
            0,
          ),
          child: Align(
            alignment: AlignmentDirectional.centerStart,
            child: Text(
              AppStrings.bookingSelectServicesHint,
              style: Theme.of(context).textTheme.bodySmall,
            ),
          ),
        ),
        Expanded(
          child: ListView.separated(
            padding: const EdgeInsets.all(AppSpacing.md),
            itemCount: services.length,
            separatorBuilder: (_, __) => const SizedBox(height: AppSpacing.sm),
            itemBuilder: (context, index) {
              final service = services[index];
              return SelectableServiceTile(
                key: Key('booking-service-${service.id}'),
                service: service,
                selected: state.isServiceSelected(service),
                onTap: () => bloc.add(BookingServiceToggled(service)),
              );
            },
          ),
        ),
        ServiceSelectionSummary(
          state: state,
          onContinue: () => bloc.add(const BookingServicesConfirmed()),
        ),
      ],
    );
  }
}

/// A service row that toggles in and out of the visit.
///
/// The checkbox (rather than a trailing tick) is what tells the customer that
/// more than one row can be on at a time.
class SelectableServiceTile extends StatelessWidget {
  final ServiceItem service;
  final bool selected;
  final VoidCallback onTap;

  const SelectableServiceTile({
    super.key,
    required this.service,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final price = JalaliFormatter.toPersianDigits(
      '${service.price.toStringAsFixed(0)} ${service.currency}'.trim(),
    );
    final duration = JalaliFormatter.toPersianDigits(
      '${service.durationMinutes} دقیقه',
    );

    // The card is one semantic node, so its label has to carry the checked
    // state too — a screen reader user cannot see the checkbox.
    final semanticLabel = selected
        ? '${service.name}، $duration، $price، ${AppStrings.bookingServiceSelectedA11y}'
        : '${service.name}، $duration، $price';

    return AppCard(
      onTap: onTap,
      semanticLabel: semanticLabel,
      child: Row(
        children: [
          Icon(
            selected ? Icons.check_box : Icons.check_box_outline_blank,
            color: selected
                ? theme.colorScheme.primary
                : theme.colorScheme.onSurfaceVariant,
          ),
          const SizedBox(width: AppSpacing.sm),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(service.name, style: theme.textTheme.titleSmall),
                const SizedBox(height: AppSpacing.xxs),
                Row(
                  children: [
                    Icon(
                      Icons.schedule,
                      size: 14,
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                    const SizedBox(width: AppSpacing.xxs),
                    Text(duration, style: theme.textTheme.bodySmall),
                  ],
                ),
              ],
            ),
          ),
          Text(price, style: theme.textTheme.titleSmall),
        ],
      ),
    );
  }
}

/// Pinned running total for the selected services, plus the step's continue
/// button.
class ServiceSelectionSummary extends StatelessWidget {
  final BookingState state;
  final VoidCallback onContinue;

  const ServiceSelectionSummary({
    super.key,
    required this.state,
    required this.onContinue,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final count = state.services.length;

    final countLabel = count == 0
        ? AppStrings.bookingSelectAtLeastOneService
        : JalaliFormatter.toPersianDigits(
            '$count ${AppStrings.bookingServicesSelectedSuffix}',
          );
    final durationLabel = JalaliFormatter.toPersianDigits(
      '${state.totalDurationMinutes} دقیقه',
    );
    final priceLabel = JalaliFormatter.toPersianDigits(
      '${state.totalPrice.toStringAsFixed(0)} ${state.currency}'.trim(),
    );

    return Material(
      elevation: 8,
      color: theme.colorScheme.surface,
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                countLabel,
                key: const Key('booking-services-count'),
                style: theme.textTheme.bodySmall,
              ),
              const SizedBox(height: AppSpacing.xs),
              // Both totals are flexible so they wrap instead of overflowing at
              // large font scales — Persian labels plus long prices are wide.
              Row(
                children: [
                  Icon(
                    Icons.schedule,
                    size: 16,
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                  const SizedBox(width: AppSpacing.xxs),
                  Expanded(
                    child: Text(
                      '${AppStrings.bookingTotalDuration}: $durationLabel',
                      key: const Key('booking-services-total-duration'),
                      style: theme.textTheme.bodyMedium,
                    ),
                  ),
                  const SizedBox(width: AppSpacing.xs),
                  Flexible(
                    child: Text(
                      '${AppStrings.bookingTotalPrice}: $priceLabel',
                      key: const Key('booking-services-total-price'),
                      style: theme.textTheme.titleSmall,
                      textAlign: TextAlign.end,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppSpacing.sm),
              AppButton(
                key: const Key('booking-services-continue'),
                label: AppStrings.bookingContinue,
                // Zero selections must not advance the flow.
                onPressed: state.hasServices ? onContinue : null,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
