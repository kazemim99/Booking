import 'package:flutter/material.dart';

import '../../../core/constants/app_strings.dart';
import '../../../core/di/injection.dart';
import '../../../core/widgets/widgets.dart';
import '../domain/repositories/review_repository.dart';
import 'widgets/write_review_dialog.dart';

/// Writes a review for a booking: the dialog, the request, and the customer
/// told how it went — the server's own reason when it refuses. True when the
/// review was saved. One flow for the booking page and the bookings list, so
/// both say the same things (openspec/changes/_inline/customer-reviews-and-nahal-seed).
Future<bool> writeReviewForBooking(
  BuildContext context, {
  required String bookingId,
  String? subject,
  double? rating,
}) async {
  final draft =
      await showWriteReviewDialog(context, subject: subject, rating: rating);
  if (draft == null || !context.mounted) return false;

  final result = await getIt<ReviewRepository>().createReview(
    bookingId: bookingId,
    rating: draft.rating,
    comment: draft.comment,
    dimensions: draft.dimensions,
    showName: draft.showName,
  );
  if (!context.mounted) return false;

  return result.fold(
    (failure) {
      AppSnackbar.error(context, failure.message);
      return false;
    },
    (_) {
      AppSnackbar.success(context, AppStrings.reviewSaved);
      return true;
    },
  );
}

/// Edits the customer's review [reviewId] from a booking — one review per salon,
/// so a later visit's card offers «ویرایش نظر» instead of a second «ثبت نظر»
/// (reviews-and-reschedule-round2 item 8). The review is read from the
/// customer's own list (the path «نظرهای من» edits by), opened in the same
/// dialog, and saved through the edit endpoint. True when it was saved.
Future<bool> editReviewForBooking(
  BuildContext context, {
  required String reviewId,
  String? subject,
}) async {
  final repository = getIt<ReviewRepository>();
  final mine = await repository.getMyReviews();
  if (!context.mounted) return false;

  final String? loadError = mine.fold((failure) => failure.message, (_) => null);
  if (loadError != null) {
    AppSnackbar.error(context, loadError);
    return false;
  }
  final review = mine
      .getOrElse(() => const [])
      .where((r) => r.id == reviewId)
      .firstOrNull;
  if (review == null) {
    AppSnackbar.error(context, AppStrings.reviewNotFound);
    return false;
  }

  final draft = await showWriteReviewDialog(
    context,
    subject: subject ??
        reviewSubject(review.providerName ?? '', review.serviceName ?? ''),
    initial: ReviewDraft(
      rating: review.rating,
      comment: review.comment,
      dimensions: review.dimensions,
      showName: review.showName,
    ),
  );
  if (draft == null || !context.mounted) return false;

  final result = await repository.editReview(
    reviewId: reviewId,
    rating: draft.rating,
    comment: draft.comment,
    dimensions: draft.dimensions,
    showName: draft.showName,
  );
  if (!context.mounted) return false;

  return result.fold(
    (failure) {
      AppSnackbar.error(context, failure.message);
      return false;
    },
    (_) {
      AppSnackbar.success(context, AppStrings.reviewEdited);
      return true;
    },
  );
}

/// «سالن نهال · کوتاهی مو» — what a review is about, as the dialog names it.
String reviewSubject(String providerName, String serviceName) =>
    [providerName, serviceName].where((s) => s.trim().isNotEmpty).join(' · ');
