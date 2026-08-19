import 'package:equatable/equatable.dart';

/// A bookable service (composer option + Services management row).
class ComposerService extends Equatable {
  final String id;
  final String name;
  final int durationMinutes;
  final double price;

  /// Round-tripped on edits so a full-field PUT never erases it.
  final String description;

  const ComposerService({
    required this.id,
    required this.name,
    this.durationMinutes = 0,
    this.price = 0,
    this.description = '',
  });

  @override
  List<Object?> get props => [id, name, durationMinutes, price, description];
}

/// A staff member option in the composer.
class ComposerStaff extends Equatable {
  final String id;
  final String name;

  const ComposerStaff({required this.id, required this.name});

  @override
  List<Object?> get props => [id, name];
}

/// The composer's pickable catalog (loaded once when it opens).
class ComposerCatalog extends Equatable {
  final List<ComposerService> services;
  final List<ComposerStaff> staff;

  const ComposerCatalog({required this.services, required this.staff});

  /// Slots can never be generated without at least one staff member — the
  /// backend requires a qualified individual provider to own the appointment.
  bool get hasNoStaff => staff.isEmpty;

  @override
  List<Object?> get props => [services, staff];
}

/// Result of a slot lookup: the bookable start times plus, when there are
/// none, the backend's explanation of *why* (e.g. the provider has not added
/// staff yet). Without the reason every empty day looks identical to the
/// provider, who cannot tell "fully booked" from "misconfigured".
class SlotAvailability extends Equatable {
  final List<DateTime> slots;

  /// Server-supplied reason, already localized. Only meaningful when [slots]
  /// is empty; null when the server gave no explanation.
  final String? unavailableReason;

  const SlotAvailability({required this.slots, this.unavailableReason});

  const SlotAvailability.empty({this.unavailableReason}) : slots = const [];

  bool get isEmpty => slots.isEmpty;

  @override
  List<Object?> get props => [slots, unavailableReason];
}
