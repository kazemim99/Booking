import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../domain/business_review.dart';
import '../domain/reviews_repository.dart';

class ReviewsState extends Equatable {
  final ReviewsOverview? overview;
  final bool loading;

  /// True once a load has SUCCEEDED — a failure is never shown as "no reviews".
  final bool loaded;
  final String? error;

  /// The review whose reply is being sent, if any.
  final String? busyId;

  const ReviewsState({this.overview, this.loading = false, this.loaded = false, this.error, this.busyId});

  ReviewsState copyWith({
    ReviewsOverview? overview,
    bool? loading,
    bool? loaded,
    String? error,
    bool clearError = false,
    String? busyId,
    bool clearBusy = false,
  }) =>
      ReviewsState(
        overview: overview ?? this.overview,
        loading: loading ?? this.loading,
        loaded: loaded ?? this.loaded,
        error: clearError ? null : (error ?? this.error),
        busyId: clearBusy ? null : (busyId ?? this.busyId),
      );

  @override
  List<Object?> get props => [overview, loading, loaded, error, busyId];
}

/// The business's reviews and its replies. A reply takes the state the server gave it (always awaiting approval
/// at first), never one the app assumed.
class ReviewsCubit extends Cubit<ReviewsState> {
  final ReviewsRepository _repository;

  ReviewsCubit(this._repository) : super(const ReviewsState());

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearError: true));
    final result = await _repository.load();
    if (isClosed) return;
    result.fold(
      (failure) => emit(state.copyWith(loading: false, error: failure.message)),
      (overview) => emit(state.copyWith(overview: overview, loading: false, loaded: true, clearError: true)),
    );
  }

  /// Writes or rewrites the reply. Returns the server's reason when it refused.
  Future<String?> reply(String reviewId, String text) async {
    final review = _find(reviewId);
    if (review == null) return null;
    emit(state.copyWith(busyId: reviewId));
    final result = await _repository.reply(reviewId, text, edit: review.hasReply);
    if (isClosed) return null;
    return result.fold((failure) {
      emit(state.copyWith(clearBusy: true));
      return failure.message;
    }, (reply) {
      _replace(review.withReply(reply));
      return null;
    });
  }

  Future<String?> removeReply(String reviewId) async {
    final review = _find(reviewId);
    if (review == null) return null;
    emit(state.copyWith(busyId: reviewId));
    final result = await _repository.removeReply(reviewId);
    if (isClosed) return null;
    return result.fold((failure) {
      emit(state.copyWith(clearBusy: true));
      return failure.message;
    }, (_) {
      _replace(review.withReply(null));
      return null;
    });
  }

  BusinessReview? _find(String id) {
    for (final r in state.overview?.items ?? const <BusinessReview>[]) {
      if (r.id == id) return r;
    }
    return null;
  }

  /// Swaps one review and moves the awaiting count by what that review's change means — the rest of the count
  /// is the server's and is left alone.
  void _replace(BusinessReview next) {
    final overview = state.overview!;
    final before = _find(next.id)!;
    final delta = (next.awaitingReply ? 1 : 0) - (before.awaitingReply ? 1 : 0);
    emit(state.copyWith(
      overview: overview.copyWith(
        items: [for (final r in overview.items) r.id == next.id ? next : r],
        awaitingReplyCount: (overview.awaitingReplyCount + delta).clamp(0, 1 << 30),
      ),
      clearBusy: true,
    ));
  }
}
