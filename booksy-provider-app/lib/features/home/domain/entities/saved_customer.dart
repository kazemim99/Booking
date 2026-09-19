import 'package:equatable/equatable.dart';

/// One entry of the salon's own customer book (spec: provider-customer-book):
/// added by hand or picked from the phone's contacts.
class SavedCustomer extends Equatable {
  final String id;
  final String firstName;
  final String lastName;

  /// As the server stores it (`+98912…`); show with `PhoneNumber.display`.
  final String phone;
  final String? notes;

  /// `Manual` or `Contacts`.
  final String source;
  final int totalBookings;
  final int upcomingBookings;
  final DateTime? lastBookingAt;

  const SavedCustomer({
    required this.id,
    required this.firstName,
    this.lastName = '',
    required this.phone,
    this.notes,
    this.source = 'Manual',
    this.totalBookings = 0,
    this.upcomingBookings = 0,
    this.lastBookingAt,
  });

  String get fullName => lastName.isEmpty ? firstName : '$firstName $lastName';

  @override
  List<Object?> get props => [
        id,
        firstName,
        lastName,
        phone,
        notes,
        source,
        totalBookings,
        upcomingBookings,
        lastBookingAt,
      ];
}

/// What the provider typed or picked, before the server has saved it.
class CustomerDraft extends Equatable {
  final String firstName;
  final String lastName;
  final String phone;
  final String? notes;

  const CustomerDraft({
    required this.firstName,
    this.lastName = '',
    required this.phone,
    this.notes,
  });

  String get fullName => lastName.isEmpty ? firstName : '$firstName $lastName';

  /// The body the customers endpoints take.
  Map<String, dynamic> toJson() => {
        'firstName': firstName.trim(),
        'lastName': lastName.trim(),
        'phoneNumber': phone.trim(),
        if (notes != null && notes!.trim().isNotEmpty) 'notes': notes!.trim(),
      };

  @override
  List<Object?> get props => [firstName, lastName, phone, notes];
}

/// The server's answer to an import of picked contacts.
class CustomerImportSummary extends Equatable {
  final int added;
  final int alreadySaved;
  final int invalid;

  const CustomerImportSummary({
    this.added = 0,
    this.alreadySaved = 0,
    this.invalid = 0,
  });

  @override
  List<Object?> get props => [added, alreadySaved, invalid];
}
