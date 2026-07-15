import 'package:equatable/equatable.dart';

/// A bookable service option in the composer.
class ComposerService extends Equatable {
  final String id;
  final String name;
  final int durationMinutes;
  final double price;

  const ComposerService({
    required this.id,
    required this.name,
    this.durationMinutes = 0,
    this.price = 0,
  });

  @override
  List<Object?> get props => [id, name, durationMinutes, price];
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
