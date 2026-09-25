import 'package:equatable/equatable.dart';

import '../../../core/utils/persian_digits.dart';

/// Discounts and campaigns (openspec/changes/add-discounts-and-campaigns). Enum values travel as the server's names.

enum PromotionActivation {
  automatic('Automatic'),
  code('Code');

  final String wire;
  const PromotionActivation(this.wire);

  static PromotionActivation parse(Object? v) =>
      values.firstWhere((e) => e.wire.toLowerCase() == '$v'.toLowerCase(), orElse: () => automatic);
}

enum DiscountKind {
  percentage('Percentage'),
  fixedAmount('FixedAmount');

  final String wire;
  const DiscountKind(this.wire);

  static DiscountKind parse(Object? v) =>
      values.firstWhere((e) => e.wire.toLowerCase() == '$v'.toLowerCase(), orElse: () => percentage);
}

/// What customers meet right now; derived on the server from status, dates and usage.
enum PromotionState {
  scheduled('Scheduled'),
  active('Active'),
  paused('Paused'),
  expired('Expired'),
  exhausted('Exhausted'),
  ended('Ended');

  final String wire;
  const PromotionState(this.wire);

  static PromotionState parse(Object? v) =>
      values.firstWhere((e) => e.wire.toLowerCase() == '$v'.toLowerCase(), orElse: () => active);
}

enum PromotionAction { pause, resume, end }

/// The Persian week, Saturday first. Values are the server's day numbers (0 = Sunday … 6 = Saturday).
const persianWeek = [6, 0, 1, 2, 3, 4, 5];

class Promotion extends Equatable {
  final String id;
  final bool isPlatform;
  final String title;
  final String? description;
  final PromotionActivation activation;
  final String? code;
  final DiscountKind kind;
  final double value;
  final double? maxDiscountAmount;
  final double? minimumSubtotal;
  final bool newCustomersOnly;
  final List<String> serviceIds;
  final List<int> daysOfWeek;
  final String? dailyStartTime;
  final String? dailyEndTime;
  final DateTime startsAt;
  final DateTime? endsAt;
  final int? totalUsageLimit;
  final int? perCustomerLimit;
  final String status;
  final PromotionState state;
  final bool pausedByPlatform;
  final int uses;
  final double totalDiscount;

  const Promotion({
    required this.id,
    required this.isPlatform,
    required this.title,
    this.description,
    required this.activation,
    this.code,
    required this.kind,
    required this.value,
    this.maxDiscountAmount,
    this.minimumSubtotal,
    this.newCustomersOnly = false,
    this.serviceIds = const [],
    this.daysOfWeek = const [],
    this.dailyStartTime,
    this.dailyEndTime,
    required this.startsAt,
    this.endsAt,
    this.totalUsageLimit,
    this.perCustomerLimit,
    required this.status,
    required this.state,
    this.pausedByPlatform = false,
    this.uses = 0,
    this.totalDiscount = 0,
  });

  factory Promotion.fromJson(Map<String, dynamic> json) => Promotion(
        id: '${json['id'] ?? ''}',
        isPlatform: '${json['owner']}'.toLowerCase() == 'platform',
        title: json['title'] as String? ?? '',
        description: json['description'] as String?,
        activation: PromotionActivation.parse(json['activation']),
        code: json['code'] as String?,
        kind: DiscountKind.parse(json['discountKind']),
        value: (json['discountValue'] as num?)?.toDouble() ?? 0,
        maxDiscountAmount: (json['maxDiscountAmount'] as num?)?.toDouble(),
        minimumSubtotal: (json['minimumSubtotal'] as num?)?.toDouble(),
        newCustomersOnly: json['newCustomersOnly'] == true,
        serviceIds: (json['serviceIds'] as List? ?? const []).map((e) => '$e').toList(),
        daysOfWeek: (json['daysOfWeek'] as List? ?? const []).whereType<num>().map((e) => e.toInt()).toList(),
        dailyStartTime: json['dailyStartTime'] as String?,
        dailyEndTime: json['dailyEndTime'] as String?,
        startsAt: DateTime.tryParse('${json['startsAt']}')?.toUtc() ?? DateTime.now().toUtc(),
        endsAt: json['endsAt'] == null ? null : DateTime.tryParse('${json['endsAt']}')?.toUtc(),
        totalUsageLimit: (json['totalUsageLimit'] as num?)?.toInt(),
        perCustomerLimit: (json['perCustomerLimit'] as num?)?.toInt(),
        status: '${json['status'] ?? 'Active'}',
        state: PromotionState.parse(json['state']),
        pausedByPlatform: json['pausedByPlatform'] == true,
        uses: (json['uses'] as num?)?.toInt() ?? 0,
        totalDiscount: (json['totalDiscount'] as num?)?.toDouble() ?? 0,
      );

  bool get isEnded => status == 'Ended';
  bool get isPaused => status == 'Paused';

  /// Lifecycle actions that make sense now. A promotion the platform paused cannot be resumed by the salon.
  List<PromotionAction> get actions {
    if (isEnded) return const [];
    if (isPaused) return pausedByPlatform ? const [PromotionAction.end] : const [PromotionAction.resume, PromotionAction.end];
    return const [PromotionAction.pause, PromotionAction.end];
  }

  /// "۲۰٪ تخفیف" · "۵۰,۰۰۰ تومان تخفیف" · "۲۰٪ تخفیف تا سقف ۱۰۰,۰۰۰ تومان".
  String get benefitText => describeBenefit(kind, value, maxDiscountAmount);

  @override
  List<Object?> get props => [id, status, state, uses, title, value, kind, code, endsAt, totalDiscount];
}

String describeBenefit(DiscountKind kind, double value, double? cap) {
  if (kind == DiscountKind.fixedAmount) {
    return '${PersianDigits.toPersian(PriceText.format(value))} تومان تخفیف';
  }
  final percent = '${PersianDigits.toPersian(_trim(value))}٪ تخفیف';
  return cap == null ? percent : '$percent تا سقف ${PersianDigits.toPersian(PriceText.format(cap))} تومان';
}

String _trim(double v) => v == v.roundToDouble() ? v.toStringAsFixed(0) : v.toString();

/// A platform campaign as this salon sees it.
class CampaignOffer extends Equatable {
  final Promotion campaign;
  final bool isJoined;

  const CampaignOffer({required this.campaign, required this.isJoined});

  factory CampaignOffer.fromJson(Map<String, dynamic> json) => CampaignOffer(
        campaign: Promotion.fromJson(Map<String, dynamic>.from(json['campaign'] as Map? ?? const {})),
        isJoined: json['isJoined'] == true,
      );

  CampaignOffer withJoined(bool joined) => CampaignOffer(campaign: campaign, isJoined: joined);

  @override
  List<Object?> get props => [campaign, isJoined];
}

/// What the salon submits. Instants are sent as UTC ISO-8601; times of day as "HH:mm".
class PromotionDraft extends Equatable {
  static const minPercent = 1.0;
  static const maxPercent = 90.0;
  static final codePattern = RegExp(r'^[A-Z0-9-]{4,20}$');

  final String title;
  final PromotionActivation activation;
  final String code;
  final DiscountKind kind;
  final double? value;
  final double? maxDiscountAmount;
  final double? minimumSubtotal;
  final bool newCustomersOnly;
  final List<String> serviceIds;
  final List<int> daysOfWeek;
  final String? dailyStartTime;
  final String? dailyEndTime;
  final DateTime? startsAt;
  final DateTime? endsAt;
  final int? totalUsageLimit;
  final int? perCustomerLimit;

  const PromotionDraft({
    this.title = '',
    this.activation = PromotionActivation.automatic,
    this.code = '',
    this.kind = DiscountKind.percentage,
    this.value = 10,
    this.maxDiscountAmount,
    this.minimumSubtotal,
    this.newCustomersOnly = false,
    this.serviceIds = const [],
    this.daysOfWeek = const [],
    this.dailyStartTime,
    this.dailyEndTime,
    this.startsAt,
    this.endsAt,
    this.totalUsageLimit,
    this.perCustomerLimit,
  });

  factory PromotionDraft.fromPromotion(Promotion p) => PromotionDraft(
        title: p.title,
        activation: p.activation,
        code: p.code ?? '',
        kind: p.kind,
        value: p.value,
        maxDiscountAmount: p.maxDiscountAmount,
        minimumSubtotal: p.minimumSubtotal,
        newCustomersOnly: p.newCustomersOnly,
        serviceIds: p.serviceIds,
        daysOfWeek: p.daysOfWeek,
        dailyStartTime: p.dailyStartTime,
        dailyEndTime: p.dailyEndTime,
        startsAt: p.startsAt,
        endsAt: p.endsAt,
        totalUsageLimit: p.totalUsageLimit,
        perCustomerLimit: p.perCustomerLimit,
      );

  String get normalizedCode => code.trim().toUpperCase();

  /// The first problem, in Persian, or null when the draft can be sent. Mirrors the server (which has the last word).
  String? validate(DateTime nowUtc) {
    if (title.trim().isEmpty) return 'عنوان تخفیف را وارد کنید.';
    if (title.trim().length > 80) return 'عنوان حداکثر ۸۰ کاراکتر است.';
    if (activation == PromotionActivation.code && !codePattern.hasMatch(normalizedCode)) {
      return 'کد باید ۴ تا ۲۰ کاراکتر و فقط حروف انگلیسی، عدد و خط تیره باشد.';
    }
    final v = value;
    if (kind == DiscountKind.percentage) {
      if (v == null || v < minPercent || v > maxPercent) return 'درصد تخفیف باید بین ۱ تا ۹۰ باشد.';
    } else if (v == null || v <= 0 || v != v.roundToDouble()) {
      return 'مبلغ تخفیف را به تومان وارد کنید.';
    }
    if ((dailyStartTime == null) != (dailyEndTime == null)) return 'ساعت شروع و پایان را هر دو انتخاب کنید.';
    if (dailyStartTime != null && dailyStartTime!.compareTo(dailyEndTime!) >= 0) {
      return 'ساعت شروع باید قبل از ساعت پایان باشد.';
    }
    if (endsAt != null && !endsAt!.isAfter(startsAt ?? nowUtc)) return 'تاریخ پایان باید بعد از شروع باشد.';
    if ((totalUsageLimit ?? 1) < 1 || (perCustomerLimit ?? 1) < 1) return 'سقف استفاده باید حداقل ۱ باشد.';
    return null;
  }

  Map<String, dynamic> toJson() => {
        'title': title.trim(),
        'activation': activation.wire,
        'code': activation == PromotionActivation.code ? normalizedCode : null,
        'discountKind': kind.wire,
        'discountValue': value ?? 0,
        'maxDiscountAmount': kind == DiscountKind.percentage ? maxDiscountAmount : null,
        'minimumSubtotal': (minimumSubtotal ?? 0) > 0 ? minimumSubtotal : null,
        'newCustomersOnly': newCustomersOnly,
        'serviceIds': serviceIds.isEmpty ? null : serviceIds,
        'daysOfWeek': daysOfWeek.isEmpty ? null : ([...daysOfWeek]..sort()),
        'dailyStartTime': dailyStartTime,
        'dailyEndTime': dailyEndTime,
        'startsAt': startsAt?.toUtc().toIso8601String(),
        'endsAt': endsAt?.toUtc().toIso8601String(),
        'totalUsageLimit': totalUsageLimit,
        'perCustomerLimit': perCustomerLimit,
      };

  PromotionDraft copyWith({
    String? title,
    PromotionActivation? activation,
    String? code,
    DiscountKind? kind,
    double? Function()? value,
    double? Function()? maxDiscountAmount,
    double? Function()? minimumSubtotal,
    bool? newCustomersOnly,
    List<String>? serviceIds,
    List<int>? daysOfWeek,
    String? Function()? dailyStartTime,
    String? Function()? dailyEndTime,
    DateTime? Function()? startsAt,
    DateTime? Function()? endsAt,
    int? Function()? totalUsageLimit,
    int? Function()? perCustomerLimit,
  }) =>
      PromotionDraft(
        title: title ?? this.title,
        activation: activation ?? this.activation,
        code: code ?? this.code,
        kind: kind ?? this.kind,
        value: value != null ? value() : this.value,
        maxDiscountAmount: maxDiscountAmount != null ? maxDiscountAmount() : this.maxDiscountAmount,
        minimumSubtotal: minimumSubtotal != null ? minimumSubtotal() : this.minimumSubtotal,
        newCustomersOnly: newCustomersOnly ?? this.newCustomersOnly,
        serviceIds: serviceIds ?? this.serviceIds,
        daysOfWeek: daysOfWeek ?? this.daysOfWeek,
        dailyStartTime: dailyStartTime != null ? dailyStartTime() : this.dailyStartTime,
        dailyEndTime: dailyEndTime != null ? dailyEndTime() : this.dailyEndTime,
        startsAt: startsAt != null ? startsAt() : this.startsAt,
        endsAt: endsAt != null ? endsAt() : this.endsAt,
        totalUsageLimit: totalUsageLimit != null ? totalUsageLimit() : this.totalUsageLimit,
        perCustomerLimit: perCustomerLimit != null ? perCustomerLimit() : this.perCustomerLimit,
      );

  @override
  List<Object?> get props => [toJson()];
}

/// A readable random code: no 0/O or 1/I, since codes are read aloud and typed on phones.
String generatePromotionCode(int Function(int max) nextInt) {
  const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
  final buffer = StringBuffer('OFF-');
  for (var i = 0; i < 6; i++) {
    buffer.write(alphabet[nextInt(alphabet.length)]);
  }
  return buffer.toString();
}
