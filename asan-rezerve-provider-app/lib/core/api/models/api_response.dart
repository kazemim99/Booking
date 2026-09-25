/// Generic backend response envelope produced by `ApiResponseMiddleware`
/// (AUTH_SPECIFICATION.md §5): `{ success, statusCode, message, data, ... }`.
///
/// Manual JSON (no json_serializable codegen — see CLAUDE.md).
class ApiResponse<T> {
  final bool success;
  final T? data;
  final String? message;
  final int? statusCode;
  final List<String>? errors;

  const ApiResponse({
    required this.success,
    this.data,
    this.message,
    this.statusCode,
    this.errors,
  });

  factory ApiResponse.fromJson(
    Map<String, dynamic> json,
    T Function(Object? json) fromDataJson,
  ) {
    // `success` may be absent on some shapes; default to true for 2xx bodies.
    final rawSuccess = json['success'];
    final data = json['data'];
    return ApiResponse<T>(
      success: rawSuccess is bool ? rawSuccess : true,
      data: data == null ? null : fromDataJson(data),
      message: json['message'] as String?,
      statusCode: (json['statusCode'] as num?)?.toInt(),
      errors: _parseErrors(json['errors']),
    );
  }

  static List<String>? _parseErrors(Object? raw) {
    if (raw == null) return null;
    if (raw is List) return raw.map((e) => e.toString()).toList();
    if (raw is Map) {
      // FluentValidation-style { field: [msgs] }
      return raw.values
          .expand((v) => v is List ? v.map((e) => e.toString()) : [v.toString()])
          .toList();
    }
    return [raw.toString()];
  }

  String? get firstError =>
      (errors != null && errors!.isNotEmpty) ? errors!.first : null;
}
