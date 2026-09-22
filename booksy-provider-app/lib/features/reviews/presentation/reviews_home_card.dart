import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../config/routes/app_router.dart';
import '../../../config/theme/app_tokens.dart';
import '../../../core/constants/app_strings.dart';
import '../../../core/di/injection.dart';
import '../../../core/utils/persian_digits.dart';
import '../../../core/widgets/app_card.dart';
import '../domain/business_review.dart';
import 'reviews_cubit.dart';

/// The business's rating on Home: average and published count, and how many reviews wait on a reply. Home is
/// the app's designed entry point, so the reviews are reached from here, not only from a menu.
class ReviewsHomeCard extends StatelessWidget {
  final ReviewsOverview overview;
  final VoidCallback onTap;

  const ReviewsHomeCard({super.key, required this.overview, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return InkWell(
      key: const Key('home-reviews-card'),
      onTap: onTap,
      borderRadius: BorderRadius.circular(AppRadius.md),
      child: AppCard(
        child: Row(
          children: [
            const Icon(Icons.star_rounded, color: AppColors.warning),
            const SizedBox(width: AppSpacing.sm),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if (overview.hasRating)
                    Row(
                      children: [
                        Text(PersianDigits.toPersian(overview.averageRating.toStringAsFixed(1)),
                            style: theme.textTheme.titleMedium),
                        const SizedBox(width: AppSpacing.sm),
                        Flexible(
                          child: Text(AppStrings.reviewsPublishedCount(overview.publishedCount),
                              style: theme.textTheme.bodySmall, overflow: TextOverflow.ellipsis),
                        ),
                      ],
                    )
                  else
                    Text(AppStrings.reviewsNoneYet, style: theme.textTheme.titleSmall),
                  if (overview.awaitingReplyCount > 0)
                    Text(
                      AppStrings.reviewsAwaitingReply(overview.awaitingReplyCount),
                      style: theme.textTheme.bodySmall?.copyWith(color: AppColors.primary),
                    ),
                ],
              ),
            ),
            // chevron_right mirrors itself in RTL; picking chevron_left by hand flips it twice (QA 2026-09-22).
            const Icon(Icons.chevron_right),
          ],
        ),
      ),
    );
  }
}

/// [ReviewsHomeCard] with its own data: loads the overview and opens the reviews page. Says nothing while
/// loading or on failure — a summary card is a hint, and a guessed number is worse than none.
class ReviewsHomeEntry extends StatelessWidget {
  const ReviewsHomeEntry({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider<ReviewsCubit>(
      create: (_) => getIt<ReviewsCubit>()..load(),
      child: BlocBuilder<ReviewsCubit, ReviewsState>(
        builder: (context, state) {
          final overview = state.overview;
          if (!state.loaded || overview == null) return const SizedBox.shrink();
          return ReviewsHomeCard(
            overview: overview,
            onTap: () async {
              final cubit = context.read<ReviewsCubit>();
              await context.push(Routes.reviews);
              // Replies written there move the awaiting count; read it again on the way back.
              cubit.load();
            },
          );
        },
      ),
    );
  }
}
