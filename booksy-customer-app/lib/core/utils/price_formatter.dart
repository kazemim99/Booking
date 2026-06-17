import 'persian_formatter.dart';

/// Price formatting utilities for Iranian Toman currency
class PriceFormatter {
  PriceFormatter._();

  /// Format price as "۴۵۰٬۰۰۰ تومان"
  static String format(int amount) {
    final formatted = PersianFormatter.formatNumber(amount);
    return '$formatted تومان';
  }

  /// Format price range as "از ۱۰۰٬۰۰۰ تومان"
  static String formatFrom(int amount) {
    return 'از ${format(amount)}';
  }

  /// Format price range as "۱۰۰٬۰۰۰ - ۵۰۰٬۰۰۰ تومان"
  static String formatRange(int min, int max) {
    final minFormatted = PersianFormatter.formatNumber(min);
    final maxFormatted = PersianFormatter.formatNumber(max);
    return '$minFormatted - $maxFormatted تومان';
  }

  /// Format with discount: "۴۵۰٬۰۰۰ تومان (۲۰٪ تخفیف)"
  static String formatWithDiscount(int amount, int discount) {
    final formattedAmount = format(amount);
    final discountPercent = PersianFormatter.intToPersian(discount);
    return '$formattedAmount ($discountPercent٪ تخفیف)';
  }

  /// Format original and discounted price
  static String formatDiscounted(int original, int discounted) {
    final originalFormatted = PersianFormatter.formatNumber(original);
    final discountedFormatted = format(discounted);
    return '$discountedFormatted (قیمت اصلی: $originalFormatted تومان)';
  }
}

/// Extension methods for int (price)
extension PriceIntExtension on int {
  /// Format as price
  String formatPrice() => PriceFormatter.format(this);

  /// Format as starting price
  String formatPriceFrom() => PriceFormatter.formatFrom(this);
}
