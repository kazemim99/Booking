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

  @override
  List<Object?> get props => [services, staff];
}
