namespace Booksy.ServiceCatalog.Domain.Enums;

/// <summary>
/// Defines the primary business category for service providers.
/// Each provider must have exactly ONE category that defines their core business type.
/// </summary>
public enum ServiceCategory
{
    // Beauty & Personal Care (1-5)

    /// <summary>
    /// Women's hair salon - آرایشگاه زنانه
    /// </summary>
    HairSalon = 1,

    /// <summary>
    /// Men's barbershop - آرایشگاه مردانه
    /// </summary>
    Barbershop = 2,

    /// <summary>
    /// Beauty salon (makeup, nails, skincare) - سالن زیبایی
    /// </summary>
    BeautySalon = 3,

    /// <summary>
    /// Nail salon - آرایش ناخن
    /// </summary>
    NailSalon = 4,

    /// <summary>
    /// Spa & wellness - اسپا
    /// </summary>
    Spa = 5,

    // Health & Wellness (6-8)

    /// <summary>
    /// Massage therapy - ماساژ
    /// </summary>
    Massage = 6,

    /// <summary>
    /// Gym & fitness - باشگاه ورزشی
    /// </summary>
    Gym = 7,

    /// <summary>
    /// Yoga & meditation - یوگا و مدیتیشن
    /// </summary>
    Yoga = 8,

    // Medical (9-11)

    /// <summary>
    /// Medical clinic - کلینیک پزشکی
    /// </summary>
    MedicalClinic = 9,

    /// <summary>
    /// Dental clinic - دندانپزشکی
    /// </summary>
    Dental = 10,

    /// <summary>
    /// Physiotherapy - فیزیوتراپی
    /// </summary>
    Physiotherapy = 11,

    // Professional Services (12-15)

    /// <summary>
    /// Private tutoring & education - آموزش خصوصی
    /// </summary>
    Tutoring = 12,

    /// <summary>
    /// Auto repair & service - تعمیرات خودرو
    /// </summary>
    Automotive = 13,

    /// <summary>
    /// Home services - خدمات منزل
    /// </summary>
    HomeServices = 14,

    /// <summary>
    /// Pet care - مراقبت حیوانات
    /// </summary>
    PetCare = 15
}
