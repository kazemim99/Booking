/// The words an error response carries for the customer, whichever of the API's
/// three shapes it came in — null when it carries none:
///
/// * the host's envelope: `{ message, error: { code, message, errors: { field: [..] } } }`;
/// * a controller's own `{ success: false, errors: [ { code, message, field } ] }`
///   (creating a review answers this way);
/// * a bare `{ message }` or `{ errors: { field: [..] } }`.
///
/// A validation failure's headline is the server's English wrapper
/// («Validation failed for property …»); the customer's words are in the
/// per-field list, so that is read instead
/// (openspec/changes/_inline/customer-reviews-and-nahal-seed).
String? serverMessage(Object? data) {
  if (data is! Map) return null;

  final errors = data['errors'];
  if (errors is List && errors.isNotEmpty) {
    final first = errors.first;
    final text = first is Map ? _text(first['message']) : _text(first);
    if (text != null) return text;
  }

  final error = data['error'] is Map ? data['error'] as Map : null;
  final message = _text(data['message']) ?? _text(error?['message']);
  if (message == null || message.startsWith('Validation failed')) {
    final fields = errors is Map ? errors : error?['errors'];
    if (fields is Map) {
      for (final value in fields.values) {
        final text = value is List && value.isNotEmpty ? _text(value.first) : _text(value);
        if (text != null) return text;
      }
    }
  }
  return message;
}

String? _text(Object? value) =>
    value is String && value.trim().isNotEmpty ? value.trim() : null;
