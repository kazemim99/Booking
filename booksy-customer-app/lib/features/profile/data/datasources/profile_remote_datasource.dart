import 'package:dio/dio.dart';
import '../../../../core/api/config/api_constants.dart';

/// Remote data source for the customer's own profile.
/// PUT /api/v1/Customers/{id} (UpdateCustomerProfileCommand).
class ProfileRemoteDataSource {
  final Dio userManagementDio;

  ProfileRemoteDataSource({required this.userManagementDio});

  Future<void> updateProfile({
    required String customerId,
    required String firstName,
    required String lastName,
  }) async {
    final response = await userManagementDio.put(
      '/${ApiConstants.apiVersion}/Customers/$customerId',
      data: {
        'customerId': customerId,
        'firstName': firstName,
        'lastName': lastName,
      },
    );
    if (response.statusCode != 200 && response.statusCode != 204) {
      throw DioException(
        requestOptions: response.requestOptions,
        response: response,
        type: DioExceptionType.badResponse,
        message: 'Failed to update profile',
      );
    }
  }
}
