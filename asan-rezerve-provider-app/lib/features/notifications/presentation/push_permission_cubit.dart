import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../../../core/push/push_registration.dart';

/// Whether the one-time card has been dismissed on this device.
abstract class PushPromptMemory {
  Future<bool> wasDismissed();
  Future<void> dismiss();
}

/// Kept in the app's secure storage (this app has no other key-value store). Best-effort, like the session store: a
/// storage that fails to read or write forgets the dismissal, which only means the card may be offered once more.
class SecureStoragePushPromptMemory implements PushPromptMemory {
  static const _key = 'push_prompt_dismissed';
  final FlutterSecureStorage _storage;
  bool _dismissed = false;

  SecureStoragePushPromptMemory(this._storage);

  @override
  Future<bool> wasDismissed() async {
    if (_dismissed) return true;
    try {
      return _dismissed = await _storage.read(key: _key) == 'true';
    } catch (_) {
      return false;
    }
  }

  @override
  Future<void> dismiss() async {
    _dismissed = true;
    try {
      await _storage.write(key: _key, value: 'true');
    } catch (_) {
      // The in-memory flag holds for this session.
    }
  }
}

class PushPermissionState extends Equatable {
  final PushStatus status;

  /// The permission prompt is open.
  final bool busy;
  final bool promptDismissed;

  const PushPermissionState({
    this.status = PushStatus.unavailable,
    this.busy = false,
    this.promptDismissed = false,
  });

  /// The one-time card: only while nobody has been asked, and only until it is dismissed.
  bool get showsSoftPrompt => status == PushStatus.notAsked && !promptDismissed;

  PushPermissionState copyWith({PushStatus? status, bool? busy, bool? promptDismissed}) => PushPermissionState(
        status: status ?? this.status,
        busy: busy ?? this.busy,
        promptDismissed: promptDismissed ?? this.promptDismissed,
      );

  @override
  List<Object?> get props => [status, busy, promptDismissed];
}

/// Notifications on this device, shared by the More row and the one-time card on Home so turning them on in one place
/// updates the other. Starts [PushStatus.unavailable], so nothing is offered until the status is known.
class PushPermissionCubit extends Cubit<PushPermissionState> {
  final PushSettings _settings;
  final PushPromptMemory _memory;

  PushPermissionCubit(this._settings, this._memory) : super(const PushPermissionState());

  Future<void> load() async {
    final status = await _settings.status();
    final dismissed = await _memory.wasDismissed();
    if (isClosed) return;
    emit(state.copyWith(status: status, promptDismissed: dismissed));
  }

  /// From a tap only — in a browser the prompt is shown in response to one and nothing else.
  Future<PushStatus> enable() async {
    if (state.busy) return state.status;
    emit(state.copyWith(busy: true));
    final status = await _settings.enable();
    if (!isClosed) emit(state.copyWith(status: status, busy: false));
    return status;
  }

  Future<void> dismissPrompt() async {
    emit(state.copyWith(promptDismissed: true));
    await _memory.dismiss();
  }
}
