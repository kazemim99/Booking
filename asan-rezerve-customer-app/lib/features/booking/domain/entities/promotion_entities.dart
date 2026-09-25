import 'package:equatable/equatable.dart';

import '../../../../core/utils/persian_formatter.dart';

/// Discounts as a customer meets them (openspec/changes/add-discounts-and-campaigns). The server decides every price;
/// these only carry and describe what it said.

/// An automatic offer at a salon. Codes are never listed. Days: 0 = Sunday … 6 = Saturday; empty = every day.
class PublicOffer extends Equatable {
  final String id;
  final String title;
  final bool isPercentage;
  final double value;
  final double? maxDiscountAmount;
  final double? minimumSubtotal;
  final bool newCustomersOnly;
  final List<String> serviceIds;
  final List<int> daysOfWeek;
  final String? dailyStartTime;
  final String? dailyEndTime;

  const PublicOffer({
    required this.id,
    required this.title,
    required this.isPercentage,
    required this.value,
    this.maxDiscountAmount,
    this.minimumSubtotal,
    this.newCustomersOnly = false,
    this.serviceIds = const [],
    this.daysOfWeek = const [],
    this.dailyStartTime,
    this.dailyEndTime,
  });

  factory PublicOffer.fromJson(Map<String, dynamic> json) => PublicOffer(
        id: '${json['id'] ?? ''}',
        title: json['title'] as String? ?? '',
        isPercentage: '${json['discountKind']}'.toLowerCase() != 'fixedamount',
        value: (json['discountValue'] as num?)?.toDouble() ?? 0,
        maxDiscountAmount: (json['maxDiscountAmount'] as num?)?.toDouble(),
        minimumSubtotal: (json['minimumSubtotal'] as num?)?.toDouble(),
        newCustomersOnly: json['newCustomersOnly'] == true,
        serviceIds: (json['serviceIds'] as List? ?? const []).map((e) => '$e').toList(),
        daysOfWeek: (json['daysOfWeek'] as List? ?? const []).whereType<num>().map((e) => e.toInt()).toList(),
        dailyStartTime: json['dailyStartTime'] as String?,
        dailyEndTime: json['dailyEndTime'] as String?,
      );

  /// Nothing about the day, time or customer can change it — so its discounted price may be shown as a fact.
  bool get isUnconditional =>
      !newCustomersOnly &&
      (daysOfWeek.isEmpty || daysOfWeek.length == 7) &&
      !(dailyStartTime != null && dailyEndTime != null);

  /// The server's arithmetic: percentage (capped) or a fixed amount, never over 90%, floored to a whole Toman.
  int discountFor(double price) {
    var raw = isPercentage ? price * value / 100 : value;
    if (isPercentage && maxDiscountAmount != null && raw > maxDiscountAmount!) raw = maxDiscountAmount!;
    final ceiling = price * 0.9;
    return (raw > ceiling ? ceiling : raw).floor();
  }

  String get badge => isPercentage
      ? '${PersianFormatter.intToPersian(value.round())}٪ تخفیف'
      : '${PersianFormatter.formatNumber(value.round())} تومان تخفیف';

  static const _weekOrder = [6, 0, 1, 2, 3, 4, 5];
  static const _dayNames = {
    6: 'شنبه', 0: 'یکشنبه', 1: 'دوشنبه', 2: 'سه‌شنبه', 3: 'چهارشنبه', 4: 'پنجشنبه', 5: 'جمعه',
  };

  /// The conditions in one line, or null when there are none.
  String? get condition {
    final parts = <String>[
      if (newCustomersOnly) 'برای اولین نوبت',
      if (daysOfWeek.isNotEmpty && daysOfWeek.length < 7)
        _weekOrder.where(daysOfWeek.contains).map((d) => _dayNames[d]!).join('، '),
      if (dailyStartTime != null && dailyEndTime != null)
        'ساعت ${PersianFormatter.toPersianDigits(dailyStartTime!)} تا ${PersianFormatter.toPersianDigits(dailyEndTime!)}',
      if ((minimumSubtotal ?? 0) > 0) 'از ${PersianFormatter.formatNumber(minimumSubtotal!.round())} تومان',
    ];
    return parts.isEmpty ? null : parts.join(' · ');
  }

  @override
  List<Object?> get props => [id, title, isPercentage, value, maxDiscountAmount, minimumSubtotal, newCustomersOnly,
        serviceIds, daysOfWeek, dailyStartTime, dailyEndTime];
}

/// The best offer to show on one service, and its price when that price is certain.
class ServiceOffer extends Equatable {
  final PublicOffer offer;
  final int discount;

  /// Null when the day, time or customer decides whether it applies.
  final int? discountedPrice;

  const ServiceOffer({required this.offer, required this.discount, this.discountedPrice});

  @override
  List<Object?> get props => [offer, discount, discountedPrice];
}

ServiceOffer? offerForService(String serviceId, double price, List<PublicOffer> offers) {
  ServiceOffer? best;
  for (final offer in offers) {
    if (offer.serviceIds.isNotEmpty && !offer.serviceIds.contains(serviceId)) continue;
    if (offer.minimumSubtotal != null && price < offer.minimumSubtotal!) continue;
    final discount = offer.discountFor(price);
    if (discount <= 0 || (best != null && best.discount >= discount)) continue;
    best = ServiceOffer(
      offer: offer,
      discount: discount,
      discountedPrice: offer.isUnconditional ? price.round() - discount : null,
    );
  }
  return best;
}

enum CodeOutcome { none, applied, notFound, notEligible, betterOfferApplied }

class PriceQuote extends Equatable {
  final double subtotal;
  final double discount;
  final double total;
  final String? discountTitle;
  final String? discountCode;
  final CodeOutcome codeOutcome;

  /// Persian, for the customer: why the code did or did not apply.
  final String? codeMessage;

  const PriceQuote({
    required this.subtotal,
    required this.discount,
    required this.total,
    this.discountTitle,
    this.discountCode,
    this.codeOutcome = CodeOutcome.none,
    this.codeMessage,
  });

  factory PriceQuote.fromJson(Map<String, dynamic> json) {
    final applied = json['appliedDiscount'];
    final outcome = '${json['codeOutcome'] ?? 'None'}'.toLowerCase();
    return PriceQuote(
      subtotal: (json['subtotal'] as num?)?.toDouble() ?? 0,
      discount: (json['discount'] as num?)?.toDouble() ?? 0,
      total: (json['total'] as num?)?.toDouble() ?? 0,
      discountTitle: applied is Map ? applied['title'] as String? : null,
      discountCode: applied is Map ? applied['code'] as String? : null,
      codeOutcome: CodeOutcome.values.firstWhere((o) => o.name.toLowerCase() == outcome, orElse: () => CodeOutcome.none),
      codeMessage: json['codeMessage'] as String?,
    );
  }

  bool get hasDiscount => discount > 0;

  @override
  List<Object?> get props => [subtotal, discount, total, discountTitle, discountCode, codeOutcome, codeMessage];
}
