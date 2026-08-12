import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/core/errors/dio_failure_mapper.dart';
import 'package:booksy_customer_app/core/errors/failures.dart';

/// Every network action funnels through [mapDioFailure], so whatever it returns is
/// literally what the user reads in the snackbar.
void main() {
  DioException error(int status) => DioException(
        requestOptions: RequestOptions(path: '/v1/Bookings'),
        response: Response(
          requestOptions: RequestOptions(path: '/v1/Bookings'),
          statusCode: status,
        ),
        type: DioExceptionType.badResponse,
        // Dio fills this in itself; it is the string that used to reach the user.
        message: 'The request returned an invalid status code of $status.',
      );

  group('mapDioFailure', () {
    test('401 asks the user to sign in again rather than leaking Dio text', () {
      // Regression: 401 fell through to the default branch, so a failed booking
      // showed "The request returned an invalid status code of 401." — untranslated
      // and with no hint that signing in again was the fix. By the time this runs,
      // AuthInterceptor has already tried the refresh token and cleared the session.
      final failure = mapDioFailure(error(401));

      expect(failure, isA<UnauthorizedFailure>());
      expect(failure.message, AppStrings.sessionExpiredError);
      expect(failure.message, isNot(contains('401')));
      expect(failure.message, isNot(contains('status code')));
    });

    test('403 reads as a permission problem, not an expired session', () {
      final failure = mapDioFailure(error(403));

      expect(failure, isA<UnauthorizedFailure>());
      expect(failure.message, AppStrings.forbiddenError);
    });

    test('other server statuses keep the generic server failure', () {
      expect(mapDioFailure(error(500)), isA<ServerFailure>());
    });

    test('connectivity problems still fail fast with the offline message', () {
      final offline = DioException(
        requestOptions: RequestOptions(path: '/v1/Bookings'),
        type: DioExceptionType.connectionError,
      );

      final failure = mapDioFailure(offline);

      expect(failure, isA<NetworkFailure>());
      expect(failure.message, AppStrings.offlineActionError);
    });
  });
}
