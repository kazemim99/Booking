import 'package:equatable/equatable.dart';
import 'onboarding_data.dart';

/// A resumable server-side onboarding draft.
class OnboardingDraft extends Equatable {
  final String providerId;

  /// The backend's registrationStep (3=location, 4=services, 6=hours, 7=gallery).
  final int registrationStep;

  /// Every field the server had saved, rehydrated into the wizard's form model.
  final OnboardingData data;

  const OnboardingDraft({
    required this.providerId,
    required this.registrationStep,
    required this.data,
  });

  /// The wizard step (1-based) to resume on — the step AFTER the last one the
  /// backend recorded as saved.
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
  List<Object?> get props => [providerId, registrationStep, data];
}
