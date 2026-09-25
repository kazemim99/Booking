import '../../features/home/domain/entities/saved_customer.dart';
import 'contact_picker.dart';

/// Native builds have no picker wired yet (a future native build adds one).
ContactPicker createContactPicker() => _Unsupported();

class _Unsupported implements ContactPicker {
  @override
  bool get isSupported => false;

  @override
  Future<List<CustomerDraft>> pick({bool multiple = true}) async => const [];
}
