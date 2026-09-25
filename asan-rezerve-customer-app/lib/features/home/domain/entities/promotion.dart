import 'package:equatable/equatable.dart';

/// Promotion banner for home screen
class Promotion extends Equatable {
  final String id;
  final String title;
  final String? description;
  final String imageUrl;
  final String? actionUrl;
  final DateTime? validUntil;

  const Promotion({
    required this.id,
    required this.title,
    this.description,
    required this.imageUrl,
    this.actionUrl,
    this.validUntil,
  });

  bool get isExpired {
    if (validUntil == null) return false;
    return DateTime.now().isAfter(validUntil!);
  }

  @override
  List<Object?> get props => [
        id,
        title,
        description,
        imageUrl,
        actionUrl,
        validUntil,
      ];
}
