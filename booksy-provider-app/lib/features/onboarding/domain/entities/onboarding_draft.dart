import 'package:equatable/equatable.dart';
import '../../../auth/domain/entities/provider_status.dart';
import 'onboarding_data.dart';

/// A resumable server-side onboarding draft.
class OnboardingDraft extends Equatable {
  final String providerId;

  /// The backend's registrationStep (3=location, 4=services, 6=hours, 7=gallery,
  /// 9=fully complete — see [isFullyComplete]).
  final int registrationStep;

  /// The provider's lifecycle status as of this snapshot ("Drafted",
  /// "PendingVerification", …). Null on payload shapes that omit it, in which
  /// case [isFullyComplete] falls back to [registrationStep] alone.
  final ProviderStatus? status;

  /// Every field the server had saved, rehydrated into the wizard's form model.
  final OnboardingData data;

  const OnboardingDraft({
    required this.providerId,
    required this.registrationStep,
    this.status,
    required this.data,
  });

  /// True once the backend has fully processed registration completion — the
  /// provider is no longer Drafted (status advanced to PendingVerification or
  /// beyond), or the step counter itself reached the backend's terminal step
  /// 9 (`SaveStep9Complete`, one past this wizard's own 8-step numbering).
  ///
  /// [resumeStep]'s switch only understands steps up to 8 (a draft still being
  /// filled in) and has no case for 9, so without this check a resumed draft
  /// whose registration had already completed fell through to `default: 1` —
  /// dumping the provider back on the business-info screen despite being fully
  /// registered and pending admin verification.
  bool get isFullyComplete =>
      registrationStep >= 9 || (status != null && !status!.needsOnboarding);

  /// The wizard step (1-based) to resume on — the step AFTER the last one the
  /// backend recorded as saved. Only meaningful when [isFullyComplete] is
  /// false; callers must check that first.
  int get resumeStep {
    switch (registrationStep) {
      case 3:
        return 4; // location saved → services
      case 4:
      case 5:
        return 5; // services saved → working hours
      case 6:
        return 6; // hours saved → gallery
      case 7:
      case 8:
        return 7; // gallery saved → preview
      default:
        return 1;
    }
  }

  @override
  List<Object?> get props => [providerId, registrationStep, status, data];
}
