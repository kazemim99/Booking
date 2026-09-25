import 'package:asan_rezerve_provider_app/core/contacts/contact_picker.dart';
import 'package:asan_rezerve_provider_app/features/home/domain/entities/saved_customer.dart';

/// Stands in for the phone's contact picker: [picked] is what the provider
/// "ticks"; [calls] counts how often the picker was opened.
class FakeContactPicker implements ContactPicker {
  @override
  final bool isSupported;
  final List<CustomerDraft> picked;
  int calls = 0;

  FakeContactPicker({this.isSupported = true, this.picked = const []});

  @override
  Future<List<CustomerDraft>> pick({bool multiple = true}) async {
    calls++;
    return picked;
  }
}
