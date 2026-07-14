import 'package:dartz/dartz.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/errors/failures.dart';
import '../../domain/entities/onboarding_data.dart';
import '../../domain/repositories/onboarding_repository.dart';
import 'onboarding_state.dart';

/// Drives the organization onboarding wizard (mirrors OrganizationRegistrationFlow.vue).
///
/// Step map (1-based): 1 business info · 2 category · 3 location (→ create draft)
/// · 4 services (→ save) · 5 working hours (→ save) · 6 gallery (optional) ·
/// 7 preview (→ complete) · 8 completion.
class OnboardingCubit extends Cubit<OnboardingState> {
  final OnboardingRepository _repository;

  OnboardingCubit(this._repository) : super(const OnboardingState());

  /// Seeds default working hours and pre-fills the owner phone. Attempts to
  /// resume any existing server-side draft.
  Future<void> init({String? phoneNumber}) async {
    emit(OnboardingState(
      data: OnboardingData(
        businessInfo: BusinessInfo(phone: phoneNumber ?? ''),
        businessHours: _defaultHours(),
      ),
    ));

    final draft = await _repository.getDraftProviderId();
    draft.fold(
      (_) {},
      (providerId) {
        if (providerId != null) {
          // A draft exists server-side; keep its id so saves target it. Field
          // rehydration from /progress can be layered in later.
          emit(state.copyWith(draftProviderId: providerId));
        }
      },
    );
  }

  void updateBusinessInfo(BusinessInfo info) =>
      emit(state.copyWith(data: state.data.copyWith(businessInfo: info)));

  void selectCategory(String categoryId) =>
      emit(state.copyWith(data: state.data.copyWith(categoryId: categoryId)));

  void updateAddress(OnboardingAddress address) =>
      emit(state.copyWith(data: state.data.copyWith(address: address)));

  void setServices(List<ServiceDraft> services) =>
      emit(state.copyWith(data: state.data.copyWith(services: services)));

  void setBusinessHours(List<DayHours> hours) =>
      emit(state.copyWith(data: state.data.copyWith(businessHours: hours)));

  void back() {
    if (state.step > 1) {
      emit(state.copyWith(step: state.step - 1, phase: OnboardingPhase.editing));
    }
  }

  /// Jump to a specific step (used by the preview "edit" affordance).
  void goToStep(int step) {
    if (step >= 1 && step <= OnboardingState.totalSteps) {
      emit(state.copyWith(step: step, phase: OnboardingPhase.editing));
    }
  }

  /// Validates the current step, performs its backend save, then advances.
  Future<void> next() async {
    final error = _validateCurrent();
    if (error != null) {
      emit(state.copyWith(phase: OnboardingPhase.error, errorMessage: error));
      return;
    }

    switch (state.step) {
      case 3:
        await _run(() => _repository.createDraft(state.data), onOk: (providerId) {
          emit(state.copyWith(
            draftProviderId: providerId,
            step: 4,
            phase: OnboardingPhase.editing,
          ));
        });
        break;
      case 4:
        await _requireDraft((id) => _run(
              () => _repository.saveServices(id, state.data.services),
              onOk: (_) => _advance(),
            ));
        break;
      case 5:
        await _requireDraft((id) => _run(
              () => _repository.saveWorkingHours(id, state.data.businessHours),
              onOk: (_) => _advance(),
            ));
        break;
      default:
        // Steps 1, 2, 6 (gallery optional): no backend save; just advance.
        _advance();
    }
  }

  /// Final submit from the preview step (step 7) → complete → step 8.
  Future<void> complete() async {
    await _requireDraft((id) => _run(
          () => _repository.complete(id),
          onOk: (_) => emit(state.copyWith(
            step: 8,
            phase: OnboardingPhase.completed,
          )),
        ));
  }

  // ---- helpers ----

  void _advance() {
    if (state.step < OnboardingState.totalSteps) {
      emit(state.copyWith(step: state.step + 1, phase: OnboardingPhase.editing));
    }
  }

  Future<void> _requireDraft(Future<void> Function(String id) op) async {
    final id = state.draftProviderId;
    if (id == null) {
      emit(state.copyWith(
        phase: OnboardingPhase.error,
        errorMessage: 'خطا: شناسه کسب‌وکار یافت نشد',
      ));
      return;
    }
    await op(id);
  }

  Future<void> _run<T>(
    Future<Either<Failure, T>> Function() op, {
    required void Function(T value) onOk,
  }) async {
    emit(state.copyWith(phase: OnboardingPhase.saving));
    final result = await op();
    result.fold(
      (failure) => emit(state.copyWith(
        phase: OnboardingPhase.error,
        errorMessage: failure.message,
      )),
      onOk,
    );
  }

  String? _validateCurrent() {
    switch (state.step) {
      case 1:
        return state.data.businessInfo.isComplete
            ? null
            : 'لطفاً تمام فیلدهای الزامی را تکمیل کنید';
      case 2:
        return (state.data.categoryId?.isNotEmpty ?? false)
            ? null
            : 'لطفاً دسته‌بندی را انتخاب کنید';
      case 3:
        return state.data.address.isComplete
            ? null
            : 'لطفاً آدرس و شهر را وارد کنید';
      case 4:
        return state.data.services.isNotEmpty
            ? null
            : 'لطفاً حداقل یک خدمت اضافه کنید';
      case 5:
        return state.data.businessHours.any((d) => d.isOpen)
            ? null
            : 'لطفاً حداقل یک روز کاری را باز بگذارید';
      default:
        return null;
    }
  }

  static List<DayHours> _defaultHours() {
    // dayOfWeek 0..6; default open 09:00–18:00 for all days.
    return List.generate(
      7,
      (i) => DayHours(
        dayOfWeek: i,
        isOpen: true,
        openTime: const ClockTime(9, 0),
        closeTime: const ClockTime(18, 0),
      ),
    );
  }
}
