import 'package:json_annotation/json_annotation.dart';

part 'api_response.g.dart';

/// Generic API Response wrapper
/// Matches your backend ApiResponse<T> format
@JsonSerializable(genericArgumentFactories: true)
class ApiResponse<T> {
  final T? data;
  final bool success;
  final String? message;
  final List<String>? errors;
  final int? statusCode;

  ApiResponse({
    this.data,
    this.success = true,
    this.message,
    this.errors,
    this.statusCode,
  });

  factory ApiResponse.fromJson(
    Map<String, dynamic> json,
    T Function(Object? json) fromJsonT,
  ) =>
      _$ApiResponseFromJson(json, fromJsonT);

  Map<String, dynamic> toJson(Object Function(T value) toJsonT) =>
      _$ApiResponseToJson(this, toJsonT);

  /// Check if response is successful
  bool get isSuccess => success && errors == null || errors!.isEmpty;

  /// Get first error message
  String? get firstError => errors?.isNotEmpty == true ? errors!.first : null;
}

/// Paginated response wrapper
@JsonSerializable(genericArgumentFactories: true)
class PaginatedResponse<T> {
  final List<T> items;
  final int totalItems;
  final int pageNumber;
  final int pageSize;
  final int totalPages;

  PaginatedResponse({
    required this.items,
    required this.totalItems,
    required this.pageNumber,
    required this.pageSize,
    required this.totalPages,
  });

  factory PaginatedResponse.fromJson(
    Map<String, dynamic> json,
    T Function(Object? json) fromJsonT,
  ) =>
      _$PaginatedResponseFromJson(json, fromJsonT);

  Map<String, dynamic> toJson(Object Function(T value) toJsonT) =>
      _$PaginatedResponseToJson(this, toJsonT);

  bool get hasNextPage => pageNumber < totalPages;
  bool get hasPreviousPage => pageNumber > 1;
}
