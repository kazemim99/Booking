import 'package:equatable/equatable.dart';
import '../../domain/usecases/get_home_data.dart' show HomeSection;

/// Base event for home screen
abstract class HomeEvent extends Equatable {
  const HomeEvent();

  @override
  List<Object?> get props => [];
}

/// Load initial home screen data
class LoadHomeData extends HomeEvent {
  const LoadHomeData();
}

/// Refresh home screen data
class RefreshHomeData extends HomeEvent {
  const RefreshHomeData();
}

/// Retry a single failed section without reloading the whole screen
class RetryHomeSection extends HomeEvent {
  final HomeSection section;

  const RetryHomeSection(this.section);

  @override
  List<Object?> get props => [section];
}
