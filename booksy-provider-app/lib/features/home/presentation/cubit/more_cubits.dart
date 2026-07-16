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

/// More → مشخصات کسب‌وکار — load + save
/// (spec: provider-business-profile-editing).
class BusinessProfileCubit extends _MoreLoadCubit<BusinessProfile> {
  final HomeRepository _repository;
  BusinessProfileCubit(this._repository);

  @override
  Future<Either<Failure, BusinessProfile>> fetch() =>
      _repository.fetchBusinessProfile();

  /// Null on success; the Failure for the caller to surface otherwise.
  Future<Failure?> save({
    required String businessName,
    String? description,
  }) async {
    final result = await _repository.updateBusinessProfile(
        businessName: businessName, description: description);
    if (isClosed) return null;
    return result.fold((f) => f, (_) => null);
  }
}

/// More → تیم (Staff) — list + CRUD mutations
/// (spec: provider-staff-management).
class StaffCubit extends _MoreLoadCubit<List<ProviderStaffMember>> {
  final HomeRepository _repository;
  StaffCubit(this._repository);

  @override
  Future<Either<Failure, List<ProviderStaffMember>>> fetch() =>
      _repository.fetchStaff();

  Future<Failure?> addStaff({
    required String firstName,
    String? lastName,
    String? phoneNumber,
    String? role,
  }) =>
      _mutate(() => _repository.addStaff(
          firstName: firstName,
          lastName: lastName,
          phoneNumber: phoneNumber,
          role: role));

  Future<Failure?> updateStaff(
    String staffId, {
    required String firstName,
    String? lastName,
    String? phoneNumber,
    String? role,
  }) =>
      _mutate(() => _repository.updateStaff(staffId,
          firstName: firstName,
          lastName: lastName,
          phoneNumber: phoneNumber,
          role: role));

  Future<Failure?> removeStaff(String staffId) =>
      _mutate(() => _repository.removeStaff(staffId));

  /// Home-style mutation: null on success (and the list reloads), or the
  /// Failure for the caller to surface.
  Future<Failure?> _mutate(
    Future<Either<Failure, void>> Function() call,
  ) async {
    final result = await call();
    if (isClosed) return null;
    return result.fold((f) => f, (_) {
      load();
      return null;
    });
  }
}
