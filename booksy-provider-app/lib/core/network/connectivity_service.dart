import 'package:connectivity_plus/connectivity_plus.dart';

/// Thin wrapper over connectivity_plus (v5 — single [ConnectivityResult]).
class ConnectivityService {
  final Connectivity _connectivity;

  ConnectivityService(this._connectivity);

  Future<bool> get isOnline async {
    final result = await _connectivity.checkConnectivity();
    return result != ConnectivityResult.none;
  }

  Stream<bool> get onStatusChange => _connectivity.onConnectivityChanged
      .map((result) => result != ConnectivityResult.none);
}
