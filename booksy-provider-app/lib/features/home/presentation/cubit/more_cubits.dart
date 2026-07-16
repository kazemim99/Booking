import 'package:dartz/dartz.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/errors/failures.dart';
import '../../domain/entities/composer_models.dart';
import '../../domain/entities/more_models.dart';
import '../../domain/repositories/home_repository.dart';

/// Shared load lifecycle for the More tab's read surfaces.
enum MoreStatus { loading, ready, failed }

/// Generic state for a simple "load one value, retry on failure" surface.
class MoreState<T> extends Equatable {
  final MoreStatus status;
  final T? data;
  final String? error;

  const MoreState({this.status = MoreStatus.loading, this.data, this.error});

  @override
  List<Object?> get props => [status, data, error];
}

/// Base for the three More read-surface cubits (identical load/retry shape
/// over different repository calls — design D2).
abstract class _MoreLoadCubit<T> extends Cubit<MoreState<T>> {
  _MoreLoadCubit() : super(const MoreState());

  Future<Either<Failure, T>> fetch();

  Future<void> load() async {
    emit(const MoreState(status: MoreStatus.loading));
    final result = await fetch();
    if (isClosed) return;
    result.fold(
      (f) => emit(MoreState(status: MoreStatus.failed, error: f.message)),
      (data) => emit(MoreState(status: MoreStatus.ready, data: data)),
    );
  }
}

/// More → گزارش‌ها (Insights).
class InsightsCubit extends _MoreLoadCubit<InsightsSummary> {
  final HomeRepository _repository;
  InsightsCubit(this._repository);

  @override
  Future<Either<Failure, InsightsSummary>> fetch() =>
      _repository.fetchInsights();
}

/// More → خدمات (Services, read-only).
class ServicesCubit extends _MoreLoadCubit<List<ComposerService>> {
  final HomeRepository _repository;
  ServicesCubit(this._repository);

  @override
  Future<Either<Failure, List<ComposerService>>> fetch() =>
      _repository.fetchServices();
}

/// More → تیم (Staff, read-only).
class StaffCubit extends _MoreLoadCubit<List<ProviderStaffMember>> {
  final HomeRepository _repository;
  StaffCubit(this._repository);

  @override
  Future<Either<Failure, List<ProviderStaffMember>>> fetch() =>
      _repository.fetchStaff();
}
