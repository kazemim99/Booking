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

/// «سالن نهال · کوتاهی مو» — what a review is about, as the dialog names it.
String reviewSubject(String providerName, String serviceName) =>
    [providerName, serviceName].where((s) => s.trim().isNotEmpty).join(' · ');
