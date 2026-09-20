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

  const ProviderDetailState({
    this.status = ProviderDetailStatus.loading,
    this.provider,
    this.errorMessage,
      this.reviews,
    this.reviewsLoading = false,
  });

  @override
  List<Object?> get props => [status, provider, errorMessage, reviews, reviewsLoading];
}

class ProviderDetailCubit extends Cubit<ProviderDetailState> {
  final BookingRepository repository;
  final ReviewRepository? reviewRepository;

  ProviderDetailCubit(this.repository, {this.reviewRepository})
      : super(const ProviderDetailState());

  Future<void> load(String providerId) async {
    emit(const ProviderDetailState());
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
    if (state.status == ProviderDetailStatus.loaded) {
      await loadReviews(providerId);
    }
  }

  /// Reviews come after the profile: the page is useful without them, and a
  /// failure here only means the section says there are none to show.
  Future<void> loadReviews(String providerId) async {
    final reviews = reviewRepository;
    if (reviews == null) return;
    final result = await reviews.getProviderReviews(providerId);
    if (isClosed) return;
    emit(ProviderDetailState(
      status: state.status,
      provider: state.provider,
      errorMessage: state.errorMessage,
      reviews: result.fold((_) => null, (r) => r),
      reviewsLoading: false,
    ));
  }
}
