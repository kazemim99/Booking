import 'package:equatable/equatable.dart';

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
