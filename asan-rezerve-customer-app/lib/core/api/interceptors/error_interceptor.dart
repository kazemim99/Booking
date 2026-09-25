import 'package:dio/dio.dart';
import 'package:injectable/injectable.dart';

import '../../errors/server_message.dart';

/// Error Interceptor
/// Handles API errors and provides user-friendly error messages
@injectable
class ErrorInterceptor extends Interceptor {
  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    String errorMessage = 'خطای نامشخص رخ داده است';

    switch (err.type) {
      case DioExceptionType.connectionTimeout:
        errorMessage = 'زمان اتصال به سرور به پایان رسید';
        break;
      case DioExceptionType.sendTimeout:
        errorMessage = 'زمان ارسال درخواست به پایان رسید';
        break;
      case DioExceptionType.receiveTimeout:
        errorMessage = 'زمان دریافت پاسخ به پایان رسید';
        break;
      case DioExceptionType.badResponse:
        errorMessage = _handleHttpError(err.response);
        break;
      case DioExceptionType.cancel:
        errorMessage = 'درخواست لغو شد';
        break;
      case DioExceptionType.connectionError:
        errorMessage = 'لطفا اتصال اینترنت خود را بررسی کنید';
        break;
      default:
        errorMessage = 'خطا در برقراری ارتباط با سرور';
    }

    // Attach user-friendly error message
    final customError = DioException(
      requestOptions: err.requestOptions,
      response: err.response,
      type: err.type,
      error: errorMessage,
      message: errorMessage,
    );

    return handler.next(customError);
  }

  String _handleHttpError(Response? response) {
    if (response == null) {
      return 'خطای سرور';
    }

    final statusCode = response.statusCode;
    final data = response.data;

    // The server's own words, whichever shape they came in. A list of
    // `{ code, message }` used to be shown as its toString().
    final message = serverMessage(data);

    switch (statusCode) {
      case 400:
        return message ?? 'درخواست نامعتبر است';
      case 401:
        return message ?? 'لطفا وارد حساب کاربری خود شوید';
      case 403:
        return message ?? 'شما دسترسی لازم را ندارید';
      case 404:
        return message ?? 'اطلاعات مورد نظر یافت نشد';
      case 422:
        return message ?? 'اطلاعات ورودی نامعتبر است';
      case 429:
        return message ?? 'تعداد درخواست‌های شما بیش از حد مجاز است';
      case 500:
        return message ?? 'خطای سرور رخ داده است';
      case 502:
        return message ?? 'سرور در دسترس نیست';
      case 503:
        return message ?? 'سرویس موقتا در دسترس نیست';
      default:
        return message ?? 'خطای ناشناخته رخ داده است';
    }
  }
}
