import 'package:dartz/dartz.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/errors/failures.dart';
import '../../../onboarding/domain/entities/onboarding_data.dart'
    show ClockTime, DayHours, GalleryImageUpload;
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

/// More → ساعات کاری — weekly hours editor state
/// (spec: provider-working-hours-editing). Day/time edits are pure state;
/// save replaces the whole week (breaks round-trip untouched).
class BusinessHoursCubit extends _MoreLoadCubit<List<DayHours>> {
  final HomeRepository _repository;
  BusinessHoursCubit(this._repository);

  @override
  Future<Either<Failure, List<DayHours>>> fetch() =>
      _repository.fetchBusinessHours();

  void toggleDay(int dayOfWeek, bool isOpen) {
    _editDay(
      dayOfWeek,
      (d) => d.copyWith(
        isOpen: isOpen,
        // A day opened without times gets a sensible default range.
        openTime: isOpen ? (d.openTime ?? const ClockTime(9, 0)) : d.openTime,
        closeTime:
            isOpen ? (d.closeTime ?? const ClockTime(18, 0)) : d.closeTime,
      ),
    );
  }

  void setOpenTime(int dayOfWeek, ClockTime time) =>
      _editDay(dayOfWeek, (d) => d.copyWith(openTime: time));

  void setCloseTime(int dayOfWeek, ClockTime time) =>
      _editDay(dayOfWeek, (d) => d.copyWith(closeTime: time));

  Future<Failure?> save() async {
    final days = state.data;
    if (days == null) return null;
    final result = await _repository.updateBusinessHours(days);
    if (isClosed) return null;
    return result.fold((f) => f, (_) => null);
  }

  void _editDay(int dayOfWeek, DayHours Function(DayHours) edit) {
    final days = state.data;
    if (days == null) return;
    emit(MoreState(
      status: MoreStatus.ready,
      data: [
        for (final d in days) d.dayOfWeek == dayOfWeek ? edit(d) : d,
      ],
    ));
  }
}

/// More → گالری — photo grid + mutations
/// (spec: provider-gallery-management).
class GalleryCubit extends _MoreLoadCubit<List<GalleryImage>> {
  final HomeRepository _repository;
  GalleryCubit(this._repository);

  @override
  Future<Either<Failure, List<GalleryImage>>> fetch() =>
      _repository.fetchGallery();

  Future<Failure?> uploadImages(List<GalleryImageUpload> images) =>
      _reloadAfter(() => _repository.uploadGalleryImages(images));

  Future<Failure?> setPrimary(String imageId) =>
      _reloadAfter(() => _repository.setPrimaryGalleryImage(imageId));

  Future<Failure?> removeImage(String imageId) =>
      _reloadAfter(() => _repository.removeGalleryImage(imageId));

  Future<Failure?> _reloadAfter(
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

/// More → تعطیلات و مرخصی — days off list + mutations
/// (spec: provider-holidays-management).
class HolidaysCubit extends _MoreLoadCubit<List<ProviderHoliday>> {
  final HomeRepository _repository;
  HolidaysCubit(this._repository);

  @override
  Future<Either<Failure, List<ProviderHoliday>>> fetch() =>
      _repository.fetchHolidays();

  Future<Failure?> addHoliday({
    required DateTime date,
    required String reason,
    bool isRecurring = false,
  }) =>
      _mutateAndReload(() => _repository.addHoliday(
          date: date, reason: reason, isRecurring: isRecurring));

  Future<Failure?> removeHoliday(String holidayId) =>
      _mutateAndReload(() => _repository.removeHoliday(holidayId));

  Future<Failure?> _mutateAndReload(
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
