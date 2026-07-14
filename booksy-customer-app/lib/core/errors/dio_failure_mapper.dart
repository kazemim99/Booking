import 'package:dio/dio.dart';
import '../constants/app_strings.dart';
import 'failures.dart';

/// Maps Dio errors to user-facing failures. Connectivity problems fail
/// fast with an explicit offline message instead of a generic error, so
/// network actions never appear to hang or fail mysteriously while offline.
Failure mapDioFailure(DioException e) {
  switch (e.type) {
    case DioExceptionType.connectionError:
    case DioExceptionType.connectionTimeout:
    case DioExceptionType.sendTimeout:
    case DioExceptionType.receiveTimeout:
      return const NetworkFailure(AppStrings.offlineActionError);
    default:
      return ServerFailure(e.message ?? AppStrings.genericError);
  }
}
