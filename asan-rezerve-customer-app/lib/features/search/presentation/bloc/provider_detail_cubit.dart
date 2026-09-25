import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../booking/domain/entities/booking_entities.dart';
import '../../../booking/domain/repositories/booking_repository.dart';
import '../../../reviews/domain/entities/review.dart';
import '../../../reviews/domain/repositories/review_repository.dart';

enum ProviderDetailStatus { loading, loaded, error }

class ProviderDetailState extends Equatable {
  final ProviderDetailStatus status;
  final ProviderDetail? provider;
  final String? errorMessage;

  /// What people said about this salon. Loaded after the profile itself, so a
  /// slow or failing reviews call never holds up the page.
  final ProviderReviews? reviews;
  final bool reviewsLoading;

  /// The reviews could not be read. Said as such, with a retry — it used to
  /// read «هنوز نظری ثبت نشده است», a claim about the salon that was not true.
  final bool reviewsFailed;

  /// The next page of reviews is on its way.
  final bool reviewsLoadingMore;

  const ProviderDetailState({
    this.status = ProviderDetailStatus.loading,
    this.provider,
    this.errorMessage,
    this.reviews,
    this.reviewsLoading = false,
    this.reviewsFailed = false,
    this.reviewsLoadingMore = false,
  });

  ProviderDetailState withReviews({
    ProviderReviews? reviews,
    bool? loading,
    bool? failed,
    bool? loadingMore,
  }) =>
      ProviderDetailState(
        status: status,
        provider: provider,
        errorMessage: errorMessage,
        reviews: reviews ?? this.reviews,
        reviewsLoading: loading ?? reviewsLoading,
        reviewsFailed: failed ?? reviewsFailed,
        reviewsLoadingMore: loadingMore ?? reviewsLoadingMore,
      );

  @override
  List<Object?> get props =>
      [status, provider, errorMessage, reviews, reviewsLoading, reviewsFailed, reviewsLoadingMore];
}

class ProviderDetailCubit extends Cubit<ProviderDetailState> {
  final BookingRepository repository;
  final ReviewRepository? reviewRepository;

  ProviderDetailCubit(this.repository, {this.reviewRepository})
      : super(const ProviderDetailState());

  Future<void> load(String providerId) async {
    emit(const ProviderDetailState());
    // Both requests leave together. The reviews used to wait for the profile to come back first, so the section
    // filled a whole round-trip after the page did — on a slow connection that is the wait the salon's own
    // reviewer complained about (QA walkthrough 2026-09-22).
    final reviewsInFlight = reviewRepository?.getProviderReviews(providerId);
    final result = await repository.getProviderDetail(providerId);
    result.fold(
      (failure) => emit(ProviderDetailState(
        status: ProviderDetailStatus.error,
        errorMessage: failure.message,
      )),
      (provider) => emit(ProviderDetailState(
        status: ProviderDetailStatus.loaded,
        provider: provider,
        reviewsLoading: reviewRepository != null,
      )),
    );
    if (state.status == ProviderDetailStatus.loaded && reviewsInFlight != null) {
      final reviews = await reviewsInFlight;
      if (isClosed) return;
      emit(reviews.fold(
        (_) => state.withReviews(loading: false, failed: true),
        (r) => state.withReviews(reviews: r, loading: false, failed: false),
      ));
    }
  }

  /// Reviews come after the profile: the page is useful without them. Also the
  /// section's «تلاش مجدد» after a failed read.
  Future<void> loadReviews(String providerId) async {
    final reviews = reviewRepository;
    if (reviews == null) return;
    emit(state.withReviews(loading: true, failed: false));
    final result = await reviews.getProviderReviews(providerId);
    if (isClosed) return;
    emit(result.fold(
      (_) => state.withReviews(loading: false, failed: true),
      (r) => state.withReviews(reviews: r, loading: false),
    ));
  }

  /// The next page of the salon's reviews, after the ones shown. A failure
  /// keeps what is shown; the button stays to try again.
  Future<void> loadMoreReviews(String providerId) async {
    final reviews = reviewRepository;
    final current = state.reviews;
    if (reviews == null || current == null || !current.hasMore || state.reviewsLoadingMore) return;
    emit(state.withReviews(loadingMore: true));
    final result = await reviews.getProviderReviews(providerId, page: current.page + 1);
    if (isClosed) return;
    emit(result.fold(
      (_) => state.withReviews(loadingMore: false),
      (next) => state.withReviews(reviews: current.appending(next), loadingMore: false),
    ));
  }

  /// A signed-in reader's vote on one review. The server's tally is adopted as
  /// it comes back — the client never counts for itself, so a withdrawn or
  /// moved vote cannot drift. Returns the reason when the vote was refused.
  Future<String?> vote(String reviewId, bool isHelpful) async {
    final reviews = reviewRepository;
    if (reviews == null) return null;
    final result = await reviews.vote(reviewId, isHelpful);
    if (isClosed) return null;
    return result.fold((failure) => failure.message, (tally) {
      final current = state.reviews;
      if (current != null) {
        emit(state.withReviews(
          reviews: current.withItems([
            for (final r in current.items) r.id == reviewId ? r.withVote(tally) : r,
          ]),
        ));
      }
      return null;
    });
  }
}
