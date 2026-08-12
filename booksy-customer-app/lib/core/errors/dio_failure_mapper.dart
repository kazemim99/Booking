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
      // A 401 here means the session is gone for good: AuthInterceptor already
      // tried the refresh token and cleared the session before rejecting. Left
      // to the default branch it surfaced Dio's own untranslated text ("The
      // request returned an invalid status code of 401"), which told the user
      // nothing — most visibly on Confirm Booking, where a whole flow ended in
      // an opaque error instead of "sign in again".
      final status = e.response?.statusCode;
      if (status == 401) {
        return const UnauthorizedFailure(AppStrings.sessionExpiredError);
      }
      if (status == 403) {
        return const UnauthorizedFailure(AppStrings.forbiddenError);
      }
      return ServerFailure(e.message ?? AppStrings.genericError);
  }
}
