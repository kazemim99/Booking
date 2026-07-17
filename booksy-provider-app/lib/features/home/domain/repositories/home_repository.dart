import 'package:dartz/dartz.dart';

import '../../../../core/errors/failures.dart';
import '../../../onboarding/domain/entities/onboarding_data.dart'
    show DayHours, GalleryImageUpload;
import '../entities/composer_models.dart';
import '../entities/home_booking.dart';
import '../entities/home_snapshot.dart';
import '../entities/more_models.dart';
import '../entities/provider_client.dart';

/// Supplies the backend-derived Home inputs (resolver spec §2).
///
/// Implementations compose bookings/status/statistics into one [HomeSnapshot]
/// per fetch. Data-source swaps (e.g. a dedicated provider-clients endpoint, a
/// backend availability payload, push-fed updates) happen behind this
/// interface without touching the cubit or UI (resolved decisions #2/#4/#6).
abstract class HomeRepository {
  /// One consistent snapshot of the provider's current Home inputs.
  Future<Either<Failure, HomeSnapshot>> fetchSnapshot();

  // ---- Booking quick actions (Home T0/T1; spec: two-tap promises) ----

  /// Provider approves a pending booking request.
  Future<Either<Failure, void>> confirmBooking(String id);

  /// Provider declines/cancels a booking, with a client-visible [reason].
  Future<Either<Failure, void>> declineBooking(String id,
      {required String reason});

  /// Marks a booking completed.
  Future<Either<Failure, void>> completeBooking(String id);

  /// Marks a booking as a client no-show.
  Future<Either<Failure, void>> markNoShow(String id);

  // ---- Calendar (spec: provider-calendar) ----

  /// The provider's bookings within [from, to), enriched with service names
  /// (same row model the Home consumes).
  Future<Either<Failure, List<HomeBooking>>> fetchBookings({
    required DateTime from,
    required DateTime to,
  });

  // ---- More hub (spec: provider-more-hub) ----

  /// The provider's services (read surface; reuses the composer model).
  Future<Either<Failure, List<ComposerService>>> fetchServices();

  /// The provider's team members.
  Future<Either<Failure, List<ProviderStaffMember>>> fetchStaff();

  /// Adds a team member (spec: provider-staff-management).
  Future<Either<Failure, void>> addStaff({
    required String firstName,
    String? lastName,
    String? phoneNumber,
    String? role,
  });

  /// Updates a team member.
  Future<Either<Failure, void>> updateStaff(
    String staffId, {
    required String firstName,
    String? lastName,
    String? phoneNumber,
    String? role,
  });

  /// Removes a team member.
  Future<Either<Failure, void>> removeStaff(String staffId);

  /// Booking statistics: all-time + trailing 30 days.
  Future<Either<Failure, InsightsSummary>> fetchInsights();

  /// The editable business profile (spec: provider-business-profile-editing).
  Future<Either<Failure, BusinessProfile>> fetchBusinessProfile();

  /// Persists the business's public name/description.
  Future<Either<Failure, void>> updateBusinessProfile({
    required String businessName,
    String? description,
  });

  /// The weekly hours with breaks (spec: provider-working-hours-editing).
  Future<Either<Failure, List<DayHours>>> fetchBusinessHours();

  /// Replaces the weekly hours (breaks included — never silently erased).
  Future<Either<Failure, void>> updateBusinessHours(List<DayHours> days);

  // ---- Holidays (spec: provider-holidays-management) ----

  /// The provider's days off, soonest-first.
  Future<Either<Failure, List<ProviderHoliday>>> fetchHolidays();

  /// Adds a day off.
  Future<Either<Failure, void>> addHoliday({
    required DateTime date,
    required String reason,
    bool isRecurring,
  });

  /// Removes a day off.
  Future<Either<Failure, void>> removeHoliday(String holidayId);

  // ---- Block time (spec: provider-block-time) ----

  /// The provider's per-date availability exceptions, soonest-first.
  Future<Either<Failure, List<AvailabilityException>>> fetchExceptions();

  /// Blocks [date]: null times = closed all day; "HH:mm" times = modified
  /// hours for that date.
  Future<Either<Failure, void>> addException({
    required DateTime date,
    String? openTime,
    String? closeTime,
    required String reason,
  });

  /// Removes an exception, restoring the date's weekly hours.
  Future<Either<Failure, void>> removeException(String exceptionId);

  // ---- Gallery (spec: provider-gallery-management) ----

  /// The gallery, display-order first with the primary image marked.
  Future<Either<Failure, List<GalleryImage>>> fetchGallery();

  /// Uploads picked images (multipart).
  Future<Either<Failure, void>> uploadGalleryImages(
      List<GalleryImageUpload> images);

  /// Marks an image as the primary/public one.
  Future<Either<Failure, void>> setPrimaryGalleryImage(String imageId);

  /// Deletes an image.
  Future<Either<Failure, void>> removeGalleryImage(String imageId);

  // ---- Clients (spec: provider-clients) ----

  /// The provider's client book, most-recent activity first.
  Future<Either<Failure, List<ProviderClient>>> fetchClients();

  // ---- Booking composer (spec: provider-booking-composer) ----

  /// The composer's pickable catalog: the provider's services and staff.
  Future<Either<Failure, ComposerCatalog>> fetchComposerCatalog();

  /// Available start times (local) for the selection on [date].
  Future<Either<Failure, List<DateTime>>> fetchAvailableSlots({
    required String serviceId,
    required DateTime date,
    String? staffId,
  });

  /// Creates a walk-in booking. Client name/phone (when given) are carried in
  /// the notes per the MVP walk-in convention.
  Future<Either<Failure, void>> createBooking({
    required String serviceId,
    required String staffId,
    required DateTime startTime,
    String? clientName,
    String? clientPhone,
    String? notes,
  });
}
