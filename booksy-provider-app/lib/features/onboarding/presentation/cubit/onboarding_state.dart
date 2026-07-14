import 'package:equatable/equatable.dart';
import '../../domain/entities/onboarding_data.dart';

enum OnboardingPhase { editing, saving, error, completed }

/// Single form-wizard state. [step] is 1-based (1..totalSteps).
class OnboardingState extends Equatable {
  final int step;
  final OnboardingData data;
  final String? draftProviderId;
  final OnboardingPhase phase;
  final String? errorMessage;

  static const int totalSteps = 8;

  const OnboardingState({
    this.step = 1,
    this.data = const OnboardingData(),
    this.draftProviderId,
    this.phase = OnboardingPhase.editing,
    this.errorMessage,
  });

  bool get isSaving => phase == OnboardingPhase.saving;
  bool get isCompleted => phase == OnboardingPhase.completed;

  OnboardingState copyWith({
    int? step,
    OnboardingData? data,
    String? draftProviderId,
    OnboardingPhase? phase,
    String? errorMessage,
  }) {
    return OnboardingState(
      step: step ?? this.step,
      data: data ?? this.data,
      draftProviderId: draftProviderId ?? this.draftProviderId,
      phase: phase ?? this.phase,
      errorMessage: errorMessage,
    );
  }

  @override
  List<Object?> get props =>
      [step, data, draftProviderId, phase, errorMessage];
}
