import 'dart:async';

import 'package:connectivity_plus/connectivity_plus.dart';

/// Thin wrapper over connectivity_plus exposing a simple online/offline
/// stream the UI can subscribe to (OfflineBanner, fail-fast checks).
///
/// Registered manually in injection.dart (codegen is currently unavailable —
/// see the note there).
class ConnectivityService {
  final Connectivity _connectivity;

  ConnectivityService(this._connectivity);

  /// Emits true when any network transport is available.
  Stream<bool> get onStatusChange => _connectivity.onConnectivityChanged
      .map(_isOnline)
      .distinct();

  Future<bool> get isOnline async =>
      _isOnline(await _connectivity.checkConnectivity());

  bool _isOnline(ConnectivityResult result) =>
      result != ConnectivityResult.none;
}
