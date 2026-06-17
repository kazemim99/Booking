import 'package:dio/dio.dart';
import 'package:injectable/injectable.dart';
import 'package:retrofit/retrofit.dart';
import '../../../../core/api/config/api_constants.dart';
import '../../../../core/api/models/api_response.dart';
import '../models/auth_models.dart';

part 'auth_api_service.g.dart';

/// Auth API Service
/// Handles authentication-related API calls using Retrofit
@RestApi()
@injectable
abstract class AuthApiService {
  @factoryMethod
  factory AuthApiService(@Named('authDio') Dio dio) = _AuthApiService;

  /// Send OTP verification code to phone number
  /// POST /api/v1/Auth/send-verification-code
  @POST(ApiConstants.sendVerificationCode)
  Future<ApiResponse<SendVerificationCodeResponse>> sendVerificationCode(
    @Body() SendVerificationCodeRequest request,
  );

  /// Complete customer authentication (verify OTP + login/register)
  /// POST /api/v1/Auth/customer/complete-authentication
  @POST(ApiConstants.completeCustomerAuth)
  Future<ApiResponse<CompleteCustomerAuthResponse>> completeCustomerAuth(
    @Body() CompleteCustomerAuthRequest request,
  );

  /// Resend OTP code
  /// POST /api/v1/PhoneVerification/resend
  @POST(ApiConstants.resendOtp)
  Future<ApiResponse<SendVerificationCodeResponse>> resendOtp(
    @Body() ResendOtpRequest request,
  );

  /// Refresh access token
  /// POST /api/v1/Auth/refresh
  @POST(ApiConstants.refreshToken)
  Future<ApiResponse<RefreshTokenResponse>> refreshToken(
    @Body() RefreshTokenRequest request,
  );

  /// Logout
  /// POST /api/v1/Auth/logout
  /// Returns HTTP Response directly (no data payload expected)
  @POST(ApiConstants.logout)
  Future<HttpResponse<dynamic>> logout();
}
