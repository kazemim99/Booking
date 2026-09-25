import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/constants/app_strings.dart';
import '../../../../core/errors/failures.dart';
import '../../../reviews/domain/entities/review.dart';
import '../../domain/entities/booking_summary.dart';
import '../../domain/repositories/bookings_repository.dart';

enum AppointmentDetailStatus { loading, loaded, error }

/// One-shot feedback for the UI (snackbars).
enum AppointmentDetailNotice { none, cancelSuccess, cancelFailure }

class AppointmentDetailState extends Equatable {
  final AppointmentDetailStatus status;
  final BookingSummary? booking;
  final String? errorMessage;
  final AppointmentDetailNotice notice;
  final bool cancelling;

  /// A review was saved for this booking while the screen was open. The
  /// server refuses a second one, so the action is not offered again.
  final bool reviewed;

  const AppointmentDetailState({
    this.status = AppointmentDetailStatus.loading,
    this.booking,
    this.errorMessage,
    this.notice = AppointmentDetailNotice.none,
    this.cancelling = false,
    this.reviewed = false,
  });

  bool get canWriteReview => booking?.canReview == true && !reviewed;

  AppointmentDetailState copyWith({
    AppointmentDetailStatus? status,
    BookingSummary? booking,
    String? errorMessage,
    AppointmentDetailNotice? notice,
    bool? cancelling,
    bool? reviewed,
  }) {
    return AppointmentDetailState(
      status: status ?? this.status,
      booking: booking ?? this.booking,
      errorMessage: errorMessage,
      notice: notice ?? AppointmentDetailNotice.none,
      cancelling: cancelling ?? this.cancelling,
      reviewed: reviewed ?? this.reviewed,
    );
  }

  @override
  List<Object?> get props =>
      [status, booking, errorMessage, notice, cancelling, reviewed];
}

/// One booking of the customer's, with the same cancel/reschedule rules as
/// the list cards. It is opened from the list, the home "next booking" card
/// and notifications, so it must find bookings older than the lists' first
/// page too.
class AppointmentDetailCubit extends Cubit<AppointmentDetailState> {
  final BookingsRepository repository;
  final String bookingId;

  AppointmentDetailCubit(this.repository, this.bookingId)
      : super(const AppointmentDetailState());

  Future<void> load() async {
    // A review saved in this session outlives a reload (retry): the
    // server's copy may still say canReview, and it refuses a second one.
    final reviewed = state.reviewed;
    emit(AppointmentDetailState(reviewed: reviewed));
    final (booking, failure) = await _find();
    if (isClosed) return;
    if (booking != null) {
      emit(AppointmentDetailState(
        status: AppointmentDetailStatus.loaded,
        booking: booking,
        reviewed: reviewed,
      ));
    } else {
      emit(AppointmentDetailState(
        status: AppointmentDetailStatus.error,
        errorMessage: _messageFor(failure),
        reviewed: reviewed,
      ));
    }
  }

  /// Cancel with confirmation already given.
  Future<void> cancel({String reason = ''}) async {
    final booking = state.booking;
    if (booking == null || !booking.canCancel || state.cancelling) return;

    emit(state.copyWith(cancelling: true));
    final result = await repository.cancelBooking(
      bookingId: booking.id,
      reason: reason.isEmpty ? AppStrings.cancelBookingConfirmTitle : reason,
    );
    if (isClosed) return;

    await result.fold(
      (failure) async => emit(state.copyWith(
        cancelling: false,
        errorMessage: failure.message,
        notice: AppointmentDetailNotice.cancelFailure,
      )),
      (_) async {
        // Shown at once; the server's own copy replaces it when it arrives.
        emit(state.copyWith(
          cancelling: false,
          booking: booking.copyWith(
            status: 'Cancelled',
            canCancel: false,
            canReschedule: false,
          ),
          notice: AppointmentDetailNotice.cancelSuccess,
        ));
        await _refresh();
      },
    );
  }

  /// The reschedule screen succeeded with [newStartTime].
  Future<void> rescheduled(DateTime newStartTime) async {
    final booking = state.booking;
    if (booking == null) return;
    emit(state.copyWith(booking: booking.copyWith(startTime: newStartTime)));
    await _refresh();
  }

  /// A review was just saved for this booking: from here it shows as written and waiting for approval.
  void reviewed() {
    final booking = state.booking;
    emit(state.copyWith(
      reviewed: true,
      booking: booking?.copyWith(
        canReview: false,
        reviewStatus: ReviewModerationStatus.pending,
      ),
    ));
  }

  /// Replaces the shown booking with the server's copy; keeps what is shown
  /// when that fails.
  Future<void> _refresh() async {
    final (booking, _) = await _find();
    if (isClosed || booking == null) return;
    emit(state.copyWith(booking: booking));
  }

  /// The lists first (the commonest case, and what the list screen shows),
  /// then the booking by id.
  Future<(BookingSummary?, Failure?)> _find() async {
    final results = await Future.wait([
      repository.getMyBookings(upcoming: true),
      repository.getMyBookings(upcoming: false),
    ]);
    for (final result in results) {
      final match = result.fold<BookingSummary?>(
        (_) => null,
        (bookings) => bookings.where((b) => b.id == bookingId).firstOrNull,
      );
      if (match != null) return (match, null);
    }

    final byId = await repository.getBookingById(bookingId);
    return byId.fold((failure) => (null, failure), (b) => (b, null));
  }

  /// Offline and "not yours" say so; anything else is the generic line
  /// (a server error's own text is not written for customers).
  static String _messageFor(Failure? failure) =>
      failure is NetworkFailure || failure is UnauthorizedFailure
          ? failure!.message
          : AppStrings.genericError;
}
