import 'package:equatable/equatable.dart';

/// Service category entity
class Category extends Equatable {
  final String id;
  final String name;
  final String? icon;
  final String? imageUrl;
  final int providerCount;

  const Category({
    required this.id,
    required this.name,
    this.icon,
    this.imageUrl,
    required this.providerCount,
  });

  @override
  List<Object?> get props => [id, name, icon, imageUrl, providerCount];
}
