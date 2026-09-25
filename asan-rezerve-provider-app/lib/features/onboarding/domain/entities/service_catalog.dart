/// One suggested service: what salons of this kind usually offer, with a market
/// price and a typical duration the provider can adjust.
class ServicePreset {
  final String name;

  /// Minutes. A starting point — chair time varies by salon.
  final int minutes;

  /// Toman, as step 4 displays prices. A SUGGESTION, never a claim about this salon.
  final double price;

  /// For services priced per unit rather than per visit («به‌ازای هر شاخه»).
  final String? unitNote;

  const ServicePreset(this.name, this.minutes, this.price, {this.unitNote});
}

/// A named block of the catalogue, e.g. «رنگ و خدمات تخصصی مو».
class ServiceCatalogGroup {
  final String title;
  final List<ServicePreset> services;

  const ServiceCatalogGroup(this.title, this.services);
}

/// The usual services of a men's barbershop and a women's salon, so onboarding
/// is a few taps instead of sixty dialogs.
///
/// Prices are market rates (شهریور ۱۴۰۵) supplied by the product owner, in
/// Toman. They are seed content for a form, deliberately NOT fetched from the
/// backend: a network dependency would make this step fail exactly where it is
/// meant to save time.
class ServiceCatalog {
  ServiceCatalog._();

  /// Keyed by [BusinessCategory.id]. An unknown category yields no catalogue,
  /// and the provider adds services by hand as before.
  static List<ServiceCatalogGroup> forCategory(String? categoryId) =>
      switch (categoryId) {
        'barbershop' => menSalon,
        'hair_salon' => womenSalon,
        _ => const [],
      };

  static const menSalon = <ServiceCatalogGroup>[
    ServiceCatalogGroup('اصلاح و مو', [
      ServicePreset('اصلاح سر با ماشین', 30, 250000),
      ServicePreset('اصلاح سر با قیچی', 45, 350000),
      ServicePreset('اصلاح سر مدل‌دار', 60, 450000),
      ServicePreset('اصلاح سر و صورت', 60, 450000),
      ServicePreset('اصلاح ریش و سبیل', 20, 150000),
      ServicePreset('خط‌گیری ریش', 15, 100000),
      ServicePreset('شیو کامل صورت', 30, 200000),
      ServicePreset('اصلاح دور گردن', 15, 100000),
      ServicePreset('شستشوی مو', 15, 100000),
      ServicePreset('سشوار و حالت‌دهی', 20, 200000),
      ServicePreset('بافت مو مردانه', 45, 300000),
    ]),
    ServiceCatalogGroup('رنگ و خدمات تخصصی مو', [
      ServicePreset('رنگ مو مردانه', 60, 800000),
      ServicePreset('رنگ ریشه', 45, 600000),
      ServicePreset('رنگ ریش و سبیل', 30, 200000),
      ServicePreset('دکلره مو', 90, 1500000),
      ServicePreset('مش مو', 90, 1500000),
      ServicePreset('کراتینه مو کوتاه', 120, 2500000),
      ServicePreset('احیای مو', 90, 1500000),
      ServicePreset('صاف کردن مو', 120, 1500000),
    ]),
    ServiceCatalogGroup('پوست و مراقبت', [
      ServicePreset('پاکسازی پوست آقایان', 45, 500000),
      ServicePreset('فیشیال صورت', 60, 900000),
      ServicePreset('ماسک صورت', 30, 250000),
      ServicePreset('اسکراب صورت', 30, 300000),
      ServicePreset('بخور صورت', 20, 200000),
      ServicePreset('ماساژ صورت', 30, 300000),
      ServicePreset('ماساژ سر', 20, 250000),
      ServicePreset('وکس گوش و بینی', 15, 150000),
      ServicePreset('اصلاح ابرو آقایان', 15, 150000),
      ServicePreset('مانیکور مردانه', 30, 300000),
      ServicePreset('پدیکور مردانه', 45, 450000),
    ]),
  ];

  static const womenSalon = <ServiceCatalogGroup>[
    ServiceCatalogGroup('اصلاح و مو', [
      ServicePreset('کوتاهی ساده مو', 45, 600000),
      ServicePreset('کوتاهی مو مدل‌دار', 60, 800000),
      ServicePreset('کوتاهی ژورنالی', 75, 1000000),
      ServicePreset('موخوره‌گیری', 45, 400000),
      ServicePreset('اصلاح چتری', 15, 300000),
      ServicePreset('براشینگ ساده', 45, 450000),
      ServicePreset('براشینگ حرفه‌ای', 60, 600000),
      ServicePreset('شستشوی موی کوتاه', 20, 200000),
      ServicePreset('شستشوی موی بلند', 30, 300000),
      ServicePreset('سشوار ساده', 30, 250000),
      ServicePreset('بافت مو ساده', 30, 300000),
      ServicePreset('بافت مو حرفه‌ای', 60, 700000),
      ServicePreset('شینیون ساده', 60, 1000000),
      ServicePreset('شینیون مجلسی', 90, 2000000),
    ]),
    ServiceCatalogGroup('اصلاح صورت و ابرو', [
      ServicePreset('اصلاح صورت با بند', 30, 250000),
      ServicePreset('اصلاح ابرو با بند', 20, 200000),
      ServicePreset('اصلاح ابرو با موچین', 20, 200000),
      ServicePreset('اصلاح صورت و ابرو', 40, 400000),
      ServicePreset('وکس صورت', 30, 300000),
      ServicePreset('وکس صورت و گردن', 40, 400000),
      ServicePreset('وکس دست', 30, 300000),
      ServicePreset('وکس پا', 45, 400000),
      ServicePreset('وکس زیر بغل', 15, 200000),
      ServicePreset('وکس کامل بدن', 90, 1000000),
      ServicePreset('رنگ ابرو', 20, 200000),
      ServicePreset('رنگ مژه', 20, 250000),
    ]),
    ServiceCatalogGroup('رنگ و خدمات تخصصی مو', [
      ServicePreset('رنگ موی کوتاه', 90, 1000000),
      ServicePreset('رنگ موی متوسط', 120, 1500000),
      ServicePreset('رنگ موی بلند', 150, 2500000),
      ServicePreset('رنگ ریشه', 60, 800000),
      ServicePreset('رنگساژ', 60, 800000),
      ServicePreset('مش و هایلایت', 150, 2500000),
      ServicePreset('بالیاژ', 180, 4000000),
      ServicePreset('آمبره', 180, 4000000),
      ServicePreset('دکلره کامل', 150, 3000000),
      ServicePreset('دکوپاژ', 120, 2000000),
      ServicePreset('کراتینه مو', 180, 6000000),
      ServicePreset('بوتاکس مو', 150, 4000000),
      ServicePreset('پروتئین‌تراپی مو', 150, 4000000),
      ServicePreset('احیا و تراپی مو', 120, 2500000),
      ServicePreset('اکستنشن مو', 120, 50000, unitNote: 'به‌ازای هر شاخه'),
    ]),
    ServiceCatalogGroup('خدمات ناخن', [
      ServicePreset('مانیکور ساده', 45, 350000),
      ServicePreset('مانیکور با ژل', 60, 700000),
      ServicePreset('پدیکور ساده', 45, 500000),
      ServicePreset('پدیکور با ژل', 60, 800000),
      ServicePreset('ژل پولیش دست', 45, 500000),
      ServicePreset('ژل پولیش پا', 45, 500000),
      ServicePreset('کاشت ناخن', 120, 1000000),
      ServicePreset('ترمیم کاشت', 90, 700000),
      ServicePreset('لمینت ناخن', 60, 700000),
      ServicePreset('طراحی ناخن', 15, 100000, unitNote: 'به‌ازای هر ناخن'),
      ServicePreset('ریموو ژل یا کاشت', 30, 250000),
    ]),
    ServiceCatalogGroup('آرایش و زیبایی', [
      ServicePreset('میکاپ ساده', 60, 1000000),
      ServicePreset('میکاپ مجلسی', 90, 2000000),
      ServicePreset('میکاپ عروس', 180, 8000000),
      ServicePreset('شینیون عروس', 120, 4000000),
      ServicePreset('لیفت ابرو', 45, 700000),
      ServicePreset('لمینت مژه', 60, 800000),
      ServicePreset('اکستنشن مژه', 120, 1000000),
      ServicePreset('ترمیم مژه', 60, 700000),
    ]),
    ServiceCatalogGroup('پوست و مراقبت', [
      ServicePreset('پاکسازی پوست', 60, 700000),
      ServicePreset('فیشیال معمولی', 60, 1500000),
      ServicePreset('فیشیال VIP', 90, 3500000),
      ServicePreset('ماسک صورت', 30, 300000),
      ServicePreset('اسکراب صورت', 30, 350000),
      ServicePreset('بخور صورت', 20, 200000),
      ServicePreset('ماساژ صورت', 30, 350000),
      ServicePreset('ماساژ سر', 20, 250000),
    ]),
  ];
}
