import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../booking/domain/entities/booking_entities.dart';
import '../../../booking/domain/repositories/booking_repository.dart';

enum ProviderDetailStatus { loading, loaded, error }

class ProviderDetailState extends Equatable {
  final ProviderDetailStatus status;
  final ProviderDetail? provider;
  final String? errorMessage;

  const ProviderDetailState({
    this.status = ProviderDetailStatus.loading,
    this.provider,
    this.errorMessage,
  });

  @override
  List<Object?> get props => [status, provider, errorMessage];
}

class ProviderDetailCubit extends Cubit<ProviderDetailState> {
  final BookingRepository repository;

  ProviderDetailCubit(this.repository) : super(const ProviderDetailState());

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
      )),
    );
  }
}
