import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../config/theme/app_tokens.dart';
import '../../../core/constants/app_strings.dart';
import '../../../core/utils/persian_digits.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/widgets/app_card.dart';
import '../../../core/widgets/app_confirm_dialog.dart';
import '../../../core/widgets/app_empty_state.dart';
import '../../../core/widgets/app_error_state.dart';
import '../../../core/widgets/app_page_scaffold.dart';
import '../../../core/widgets/app_snackbar.dart';
import '../../../core/widgets/app_status_badge.dart';
import '../../../core/widgets/app_text_tabs.dart';
import '../domain/promotion.dart';
import 'promotion_form_sheet.dart';
import 'promotions_cubit.dart';

/// More → تخفیف‌ها. The salon's own discounts (create, edit, pause, end) and the platform campaigns it can join.
/// Expects a [PromotionsCubit] above it.
class PromotionsPage extends StatelessWidget {
  const PromotionsPage({super.key});

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 2,
      child: AppPageScaffold(
        title: AppStrings.promotionsTitle,
        floatingActionButton: FloatingActionButton.extended(
          key: const Key('promotions-new'),
          onPressed: () => PromotionFormSheet.show(context, context.read<PromotionsCubit>()),
          icon: const Icon(Icons.add),
          label: const Text(AppStrings.promotionsNew),
        ),
        body: BlocBuilder<PromotionsCubit, PromotionsState>(
          builder: (context, state) {
            if (state.loading && !state.loaded) return const Center(child: CircularProgressIndicator());
            if (!state.loaded) {
              return AppErrorState(
                message: state.error ?? AppStrings.promotionsLoadFailed,
                onRetry: () => context.read<PromotionsCubit>().load(),
              );
            }
            return Column(
              children: [
                const AppTextTabs(tabs: [AppStrings.promotionsMine, AppStrings.promotionsCampaigns]),
                Expanded(
                  child: TabBarView(
                    children: [
                      _MyPromotions(state: state),
                      _Campaigns(state: state),
                    ],
                  ),
                ),
              ],
            );
          },
        ),
      ),
    );
  }
}

class _MyPromotions extends StatelessWidget {
  final PromotionsState state;
  const _MyPromotions({required this.state});

  @override
  Widget build(BuildContext context) {
    final cubit = context.read<PromotionsCubit>();
    if (state.promotions.isEmpty) {
      return AppEmptyState.add(
        icon: Icons.local_offer_outlined,
        message: AppStrings.promotionsEmpty,
        description: AppStrings.promotionsEmptyHint,
        actionLabel: AppStrings.promotionsNew,
        onAction: () => PromotionFormSheet.show(context, cubit),
      );
    }
    return RefreshIndicator(
      onRefresh: cubit.load,
      child: ListView.separated(
        key: const Key('promotions-list'),
        padding: const EdgeInsets.fromLTRB(AppSpacing.md, AppSpacing.md, AppSpacing.md, 96),
        itemCount: state.promotions.length,
        separatorBuilder: (_, _) => const SizedBox(height: AppSpacing.sm),
        itemBuilder: (context, i) {
          final promotion = state.promotions[i];
          return _PromotionCard(
            promotion: promotion,
            services: {for (final s in state.services) s.id: s.name},
            busy: state.busyId == promotion.id,
          );
        },
      ),
    );
  }
}

class _PromotionCard extends StatelessWidget {
  final Promotion promotion;
  final Map<String, String> services;
  final bool busy;

  const _PromotionCard({required this.promotion, required this.services, required this.busy});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final cubit = context.read<PromotionsCubit>();
    final conditions = conditionsOf(promotion, services);

    return AppCard(
      key: Key('promotion-${promotion.id}'),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(promotion.title,
                    style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
                    overflow: TextOverflow.ellipsis),
              ),
              const SizedBox(width: AppSpacing.sm),
              AppStatusBadge(label: stateLabel(promotion.state), status: stateBadge(promotion.state)),
            ],
          ),
          const SizedBox(height: AppSpacing.xs),
          Text(promotion.benefitText,
              style: theme.textTheme.titleSmall?.copyWith(color: AppColors.success, fontWeight: FontWeight.w700)),
          const SizedBox(height: AppSpacing.sm),
          Wrap(
            spacing: AppSpacing.xs,
            runSpacing: AppSpacing.xs,
            children: [
              if (promotion.activation == PromotionActivation.code && promotion.code != null)
                _CodeChip(code: promotion.code!)
              else
                const _InfoChip(icon: Icons.bolt_outlined, label: AppStrings.promotionsAutomatic),
              for (final c in conditions) _InfoChip(label: c),
              _InfoChip(icon: Icons.event_outlined, label: periodLabel(promotion, DateTime.now().toUtc())),
            ],
          ),
          const SizedBox(height: AppSpacing.sm),
          Text(
            AppStrings.promotionsUsage(promotion.uses, promotion.totalUsageLimit,
                PersianDigits.toPersian(PriceText.format(promotion.totalDiscount))),
            style: theme.textTheme.bodySmall?.copyWith(color: AppColors.subtitle),
          ),
          if (promotion.pausedByPlatform) ...[
            const SizedBox(height: AppSpacing.xs),
            Text(AppStrings.promotionsPausedByPlatform,
                style: theme.textTheme.bodySmall?.copyWith(color: AppColors.danger)),
          ],
          if (!promotion.isEnded) ...[
            const Divider(height: AppSpacing.lg),
            Row(
              children: [
                TextButton.icon(
                  key: Key('promotion-edit-${promotion.id}'),
                  onPressed: busy ? null : () => PromotionFormSheet.show(context, cubit, initial: promotion),
                  icon: const Icon(Icons.edit_outlined, size: AppIconSize.sm),
                  label: const Text('ویرایش'),
                ),
                const Spacer(),
                if (busy) const SizedBox.square(dimension: 20, child: CircularProgressIndicator(strokeWidth: 2)),
                for (final action in promotion.actions)
                  TextButton(
                    key: Key('promotion-${action.name}-${promotion.id}'),
                    onPressed: busy ? null : () => _act(context, action),
                    style: action == PromotionAction.end
                        ? TextButton.styleFrom(foregroundColor: AppColors.danger)
                        : null,
                    child: Text(switch (action) {
                      PromotionAction.pause => AppStrings.promotionsPause,
                      PromotionAction.resume => AppStrings.promotionsResume,
                      PromotionAction.end => AppStrings.promotionsEnd,
                    }),
                  ),
              ],
            ),
          ],
        ],
      ),
    );
  }

  Future<void> _act(BuildContext context, PromotionAction action) async {
    if (action == PromotionAction.end) {
      final confirmed = await showAppConfirmDialog(
        context,
        title: AppStrings.promotionsEndConfirmTitle,
        message: AppStrings.promotionsEndConfirmBody,
        confirmLabel: AppStrings.promotionsEnd,
        destructive: true,
      );
      if (confirmed != true || !context.mounted) return;
    }
    final error = await context.read<PromotionsCubit>().change(promotion.id, action);
    if (!context.mounted) return;
    if (error != null) AppSnackbar.error(context, error);
  }
}

class _Campaigns extends StatelessWidget {
  final PromotionsState state;
  const _Campaigns({required this.state});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final cubit = context.read<PromotionsCubit>();
    return RefreshIndicator(
      onRefresh: cubit.load,
      child: ListView(
        key: const Key('campaigns-list'),
        padding: const EdgeInsets.fromLTRB(AppSpacing.md, AppSpacing.md, AppSpacing.md, 96),
        children: [
          Container(
            padding: const EdgeInsets.all(AppSpacing.card),
            decoration: BoxDecoration(
              color: AppColors.primarySoft,
              borderRadius: BorderRadius.circular(AppRadius.md),
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Icon(Icons.info_outline, color: AppColors.primary, size: AppIconSize.md),
                const SizedBox(width: AppSpacing.sm),
                Expanded(child: Text(AppStrings.promotionsCampaignsHint, style: theme.textTheme.bodySmall)),
              ],
            ),
          ),
          const SizedBox(height: AppSpacing.md),
          if (state.campaigns.isEmpty)
            const AppEmptyState(icon: Icons.campaign_outlined, message: AppStrings.promotionsCampaignsEmpty)
          else
            for (final offer in state.campaigns)
              Padding(
                padding: const EdgeInsets.only(bottom: AppSpacing.sm),
                child: _CampaignCard(offer: offer, busy: state.busyId == offer.campaign.id),
              ),
        ],
      ),
    );
  }
}

class _CampaignCard extends StatelessWidget {
  final CampaignOffer offer;
  final bool busy;
  const _CampaignCard({required this.offer, required this.busy});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final campaign = offer.campaign;
    final conditions = conditionsOf(campaign, const {});
    return AppCard(
      key: Key('campaign-${campaign.id}'),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.campaign_outlined, color: AppColors.primary),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: Text(campaign.title,
                    style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
              ),
              if (offer.isJoined)
                const AppStatusBadge(label: AppStrings.promotionsJoined, status: AppBadgeStatus.success),
            ],
          ),
          if ((campaign.description ?? '').isNotEmpty) ...[
            const SizedBox(height: AppSpacing.xs),
            Text(campaign.description!, style: theme.textTheme.bodySmall),
          ],
          const SizedBox(height: AppSpacing.sm),
          Text(campaign.benefitText,
              style: theme.textTheme.titleSmall?.copyWith(color: AppColors.success, fontWeight: FontWeight.w700)),
          const SizedBox(height: AppSpacing.sm),
          Wrap(
            spacing: AppSpacing.xs,
            runSpacing: AppSpacing.xs,
            children: [
              if (campaign.activation == PromotionActivation.code && campaign.code != null)
                _CodeChip(code: campaign.code!),
              for (final c in conditions) _InfoChip(label: c),
              _InfoChip(icon: Icons.event_outlined, label: periodLabel(campaign, DateTime.now().toUtc())),
            ],
          ),
          const SizedBox(height: AppSpacing.md),
          offer.isJoined
              ? AppButton.secondary(
                  key: Key('campaign-leave-${campaign.id}'),
                  label: AppStrings.promotionsLeave,
                  loading: busy,
                  onPressed: busy ? null : () => _toggle(context, join: false),
                )
              : AppButton(
                  key: Key('campaign-join-${campaign.id}'),
                  label: AppStrings.promotionsJoin,
                  loading: busy,
                  onPressed: busy ? null : () => _toggle(context, join: true),
                ),
        ],
      ),
    );
  }

  Future<void> _toggle(BuildContext context, {required bool join}) async {
    if (!join) {
      final confirmed = await showAppConfirmDialog(
        context,
        title: AppStrings.promotionsLeaveConfirmTitle,
        message: AppStrings.promotionsLeaveConfirmBody,
        confirmLabel: AppStrings.promotionsLeave,
        destructive: true,
      );
      if (confirmed != true || !context.mounted) return;
    }
    final error = await context.read<PromotionsCubit>().setJoined(offer.campaign.id, join: join);
    if (!context.mounted) return;
    if (error != null) {
      AppSnackbar.error(context, error);
    } else {
      AppSnackbar.success(context, join ? AppStrings.promotionsJoinedToast : AppStrings.promotionsLeftToast);
    }
  }
}

class _InfoChip extends StatelessWidget {
  final IconData? icon;
  final String label;
  const _InfoChip({this.icon, required this.label});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.sm, vertical: AppSpacing.xs),
      decoration: BoxDecoration(
        color: AppColors.surfaceSoft,
        border: Border.all(color: AppColors.border),
        borderRadius: BorderRadius.circular(AppRadius.sm),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: AppIconSize.sm, color: AppColors.muted),
            const SizedBox(width: AppSpacing.xs),
          ],
          Text(label, style: Theme.of(context).textTheme.bodySmall),
        ],
      ),
    );
  }
}

/// A code, left to right in a monospaced face, copied on tap so the salon can paste it into a message.
class _CodeChip extends StatelessWidget {
  final String code;
  const _CodeChip({required this.code});

  @override
  Widget build(BuildContext context) {
    return InkWell(
      borderRadius: BorderRadius.circular(AppRadius.sm),
      onTap: () async {
        await Clipboard.setData(ClipboardData(text: code));
        if (context.mounted) AppSnackbar.info(context, 'کد کپی شد');
      },
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: AppSpacing.sm, vertical: AppSpacing.xs),
        decoration: BoxDecoration(
          color: AppColors.primarySoft,
          borderRadius: BorderRadius.circular(AppRadius.sm),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.copy_rounded, size: AppIconSize.sm, color: AppColors.primary),
            const SizedBox(width: AppSpacing.xs),
            Text(code,
                textDirection: TextDirection.ltr,
                style: const TextStyle(fontFamily: 'monospace', letterSpacing: 1, color: AppColors.primary)),
          ],
        ),
      ),
    );
  }
}

String stateLabel(PromotionState state) => switch (state) {
      PromotionState.scheduled => AppStrings.promotionsStateScheduled,
      PromotionState.active => AppStrings.promotionsStateActive,
      PromotionState.paused => AppStrings.promotionsStatePaused,
      PromotionState.expired => AppStrings.promotionsStateExpired,
      PromotionState.exhausted => AppStrings.promotionsStateExhausted,
      PromotionState.ended => AppStrings.promotionsStateEnded,
    };

AppBadgeStatus stateBadge(PromotionState state) => switch (state) {
      PromotionState.active => AppBadgeStatus.success,
      PromotionState.scheduled || PromotionState.paused => AppBadgeStatus.warning,
      PromotionState.exhausted => AppBadgeStatus.danger,
      PromotionState.expired || PromotionState.ended => AppBadgeStatus.neutral,
    };

/// The conditions in the words a customer reads, most restrictive first.
List<String> conditionsOf(Promotion p, Map<String, String> serviceNames) {
  return [
    if (p.newCustomersOnly) AppStrings.promotionsNewCustomers,
    if (p.serviceIds.isNotEmpty)
      p.serviceIds.length == 1 && serviceNames.containsKey(p.serviceIds.first)
          ? serviceNames[p.serviceIds.first]!
          : AppStrings.promotionsServiceCount(p.serviceIds.length),
    if (p.daysOfWeek.isNotEmpty && p.daysOfWeek.length < 7)
      persianWeek.where(p.daysOfWeek.contains).map((d) => AppStrings.weekdayShort[d]!).join('، '),
    if (p.dailyStartTime != null && p.dailyEndTime != null) AppStrings.promotionsHours(p.dailyStartTime!, p.dailyEndTime!),
    if ((p.minimumSubtotal ?? 0) > 0)
      AppStrings.promotionsMinimum(PersianDigits.toPersian(PriceText.format(p.minimumSubtotal!))),
    if (p.perCustomerLimit != null) AppStrings.promotionsPerCustomer(p.perCustomerLimit!),
  ];
}

/// "۱۲ روز مانده" / "۳ روز دیگر شروع می‌شود" / "بدون تاریخ پایان".
String periodLabel(Promotion p, DateTime nowUtc) {
  if (p.startsAt.isAfter(nowUtc)) return AppStrings.promotionsStartsIn(p.startsAt.difference(nowUtc).inDays);
  if (p.endsAt == null) return AppStrings.promotionsNoEnd;
  return AppStrings.promotionsDaysLeft(p.endsAt!.difference(nowUtc).inDays);
}
