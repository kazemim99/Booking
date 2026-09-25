import 'package:equatable/equatable.dart';

/// Public summary of a staff invitation, shown on the accept screen opened from
/// an SMS link (the phone is masked server-side).
class InvitationSummary extends Equatable {
  final String invitationId;
  final String organizationId;
  final String organizationName;
  final String? organizationLogo;
  final String? inviteeName;
  final String maskedPhone;
  final String status;
  final bool isValid;

  const InvitationSummary({
    required this.invitationId,
    required this.organizationId,
    required this.organizationName,
    this.organizationLogo,
    this.inviteeName,
    this.maskedPhone = '',
    this.status = '',
    this.isValid = false,
  });

  factory InvitationSummary.fromJson(Map<String, dynamic> json) {
    String s(String key) => json[key]?.toString() ?? '';
    return InvitationSummary(
      invitationId: s('invitationId'),
      organizationId: s('organizationId'),
      organizationName: s('organizationName'),
      organizationLogo:
          s('organizationLogo').isEmpty ? null : s('organizationLogo'),
      inviteeName: s('inviteeName').isEmpty ? null : s('inviteeName'),
      maskedPhone: s('maskedPhone'),
      status: s('status'),
      isValid: json['isValid'] == true,
    );
  }

  @override
  List<Object?> get props => [
        invitationId,
        organizationId,
        organizationName,
        organizationLogo,
        inviteeName,
        maskedPhone,
        status,
        isValid,
      ];
}
