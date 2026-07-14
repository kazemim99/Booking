import 'package:dio/dio.dart';
import '../../constants/app_strings.dart';

/// Maps transport/HTTP errors to user-friendly Persian messages
/// (AUTH_SPECIFICATION.md §9). Attaches the message to the DioException so the
/// repository/failure mapper can surface it.
class ErrorInterceptor extends Interceptor {
  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    String message;

    switch (err.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
        message = AppStrings.networkError;
        break;
      case DioExceptionType.connectionError:
        message = AppStrings.networkError;
        break;
      case DioExceptionType.badResponse:
        message = _httpMessage(err.response);
        break;
      case DioExceptionType.cancel:
        message = 'درخواست لغو شد';
        break;
      default:
        message = AppStrings.genericError;
    }

    handler.next(
      DioException(
        requestOptions: err.requestOptions,
        response: err.response,
        type: err.type,
        error: message,
        message: message,
      ),
    );
  }

  String _httpMessage(Response? response) {
    if (response == null) return AppStrings.genericError;
    final data = response.data;
    String? serverMessage;
    if (data is Map<String, dynamic>) {
      serverMessage = data['message'] as String?;
      final errors = data['errors'];
      if (errors is List && errors.isNotEmpty) {
        serverMessage = errors.first.toString();
      } else if (errors is Map && errors.isNotEmpty) {
        final first = errors.values.first;
        if (first is List && first.isNotEmpty) serverMessage = first.first.toString();
      }
    }

    switch (response.statusCode) {
      case 400:
      case 422:
        return serverMessage ?? 'داده‌های ورودی نامعتبر است';
      case 401:
        return serverMessage ?? AppStrings.verifyCodeError;
      case 403:
        return serverMessage ?? 'شما اجازه دسترسی به این بخش را ندارید';
      case 404:
        return serverMessage ?? 'اطلاعات مورد نظر یافت نشد';
      case 409:
        return serverMessage ?? 'این اطلاعات قبلاً ثبت شده است';
      case 429:
        return serverMessage ??
            'تعداد درخواست‌های شما بیش از حد مجاز است. لطفاً کمی صبر کنید';
      case 500:
      case 502:
      case 503:
      case 504:
        return serverMessage ?? 'خطای سرور رخ داده است. لطفاً بعداً تلاش کنید';
      default:
        return serverMessage ?? AppStrings.genericError;
    }
  }
}
