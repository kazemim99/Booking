namespace Booksy.ServiceCatalog.Domain.Enums.Extensions;

/// <summary>
/// Extension methods for ServiceCategory enum providing metadata and display information
/// </summary>
public static class ServiceCategoryExtensions
{
    /// <summary>
    /// Gets the Persian display name for the category
    /// </summary>
    /// <param name="category">The service category</param>
    /// <returns>Persian name</returns>
    public static string ToPersianName(this ServiceCategory category) => category switch
    {
        ServiceCategory.HairSalon => "آرایشگاه زنانه",
        ServiceCategory.Barbershop => "آرایشگاه مردانه",
        ServiceCategory.BeautySalon => "سالن زیبایی",
        ServiceCategory.NailSalon => "آرایش ناخن",
        ServiceCategory.Spa => "اسپا",
        ServiceCategory.Massage => "ماساژ",
        ServiceCategory.Gym => "باشگاه ورزشی",
        ServiceCategory.Yoga => "یوگا و مدیتیشن",
        ServiceCategory.MedicalClinic => "کلینیک پزشکی",
        ServiceCategory.Dental => "دندانپزشکی",
        ServiceCategory.Physiotherapy => "فیزیوتراپی",
        ServiceCategory.Tutoring => "آموزش خصوصی",
        ServiceCategory.Automotive => "تعمیرات خودرو",
        ServiceCategory.HomeServices => "خدمات منزل",
        ServiceCategory.PetCare => "مراقبت حیوانات",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown service category")
    };

    /// <summary>
    /// Gets the English display name for the category
    /// </summary>
    /// <param name="category">The service category</param>
    /// <returns>English name</returns>
    public static string ToEnglishName(this ServiceCategory category) => category switch
    {
        ServiceCategory.HairSalon => "Women's Hair Salon",
        ServiceCategory.Barbershop => "Men's Barbershop",
        ServiceCategory.BeautySalon => "Beauty Salon",
        ServiceCategory.NailSalon => "Nail Salon",
        ServiceCategory.Spa => "Spa & Wellness",
        ServiceCategory.Massage => "Massage Therapy",
        ServiceCategory.Gym => "Gym & Fitness",
        ServiceCategory.Yoga => "Yoga & Meditation",
        ServiceCategory.MedicalClinic => "Medical Clinic",
        ServiceCategory.Dental => "Dental Clinic",
        ServiceCategory.Physiotherapy => "Physiotherapy",
        ServiceCategory.Tutoring => "Private Tutoring",
        ServiceCategory.Automotive => "Auto Repair & Service",
        ServiceCategory.HomeServices => "Home Services",
        ServiceCategory.PetCare => "Pet Care",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown service category")
    };

    /// <summary>
    /// Gets the emoji icon for the category
    /// </summary>
    /// <param name="category">The service category</param>
    /// <returns>Emoji icon</returns>
    public static string ToIcon(this ServiceCategory category) => category switch
    {
        ServiceCategory.HairSalon => "💇‍♀️",
        ServiceCategory.Barbershop => "💇‍♂️",
        ServiceCategory.BeautySalon => "💅",
        ServiceCategory.NailSalon => "💅",
        ServiceCategory.Spa => "🧖",
        ServiceCategory.Massage => "💆",
        ServiceCategory.Gym => "🏋️",
        ServiceCategory.Yoga => "🧘",
        ServiceCategory.MedicalClinic => "🏥",
        ServiceCategory.Dental => "🦷",
        ServiceCategory.Physiotherapy => "💆‍♀️",
        ServiceCategory.Tutoring => "📚",
        ServiceCategory.Automotive => "🚗",
        ServiceCategory.HomeServices => "🏠",
        ServiceCategory.PetCare => "🐾",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown service category")
    };

    /// <summary>
    /// Gets the brand color hex code for the category
    /// </summary>
    /// <param name="category">The service category</param>
    /// <returns>Hex color code (e.g., #8B5CF6)</returns>
    public static string ToColorHex(this ServiceCategory category) => category switch
    {
        ServiceCategory.HairSalon => "#8B5CF6",      // Purple
        ServiceCategory.Barbershop => "#3B82F6",     // Blue
        ServiceCategory.BeautySalon => "#EC4899",    // Pink
        ServiceCategory.NailSalon => "#F472B6",      // Light Pink
        ServiceCategory.Spa => "#06B6D4",            // Cyan
        ServiceCategory.Massage => "#10B981",        // Green
        ServiceCategory.Gym => "#F59E0B",            // Orange
        ServiceCategory.Yoga => "#A855F7",           // Light Purple
        ServiceCategory.MedicalClinic => "#EF4444",  // Red
        ServiceCategory.Dental => "#22D3EE",         // Light Cyan
        ServiceCategory.Physiotherapy => "#14B8A6",  // Teal
        ServiceCategory.Tutoring => "#6366F1",       // Indigo
        ServiceCategory.Automotive => "#64748B",     // Slate
        ServiceCategory.HomeServices => "#84CC16",   // Lime
        ServiceCategory.PetCare => "#FBBF24",        // Yellow
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown service category")
    };

    /// <summary>
    /// Gets the CSS gradient for the category
    /// </summary>
    /// <param name="category">The service category</param>
    /// <returns>CSS gradient string</returns>
    public static string ToGradient(this ServiceCategory category) => category switch
    {
        ServiceCategory.HairSalon => "linear-gradient(135deg, #8B5CF6 0%, #A78BFA 100%)",
        ServiceCategory.Barbershop => "linear-gradient(135deg, #3B82F6 0%, #60A5FA 100%)",
        ServiceCategory.BeautySalon => "linear-gradient(135deg, #EC4899 0%, #F472B6 100%)",
        ServiceCategory.NailSalon => "linear-gradient(135deg, #F472B6 0%, #FB923C 100%)",
        ServiceCategory.Spa => "linear-gradient(135deg, #06B6D4 0%, #22D3EE 100%)",
        ServiceCategory.Massage => "linear-gradient(135deg, #10B981 0%, #34D399 100%)",
        ServiceCategory.Gym => "linear-gradient(135deg, #F59E0B 0%, #FBBF24 100%)",
        ServiceCategory.Yoga => "linear-gradient(135deg, #A855F7 0%, #C084FC 100%)",
        ServiceCategory.MedicalClinic => "linear-gradient(135deg, #EF4444 0%, #F87171 100%)",
        ServiceCategory.Dental => "linear-gradient(135deg, #22D3EE 0%, #67E8F9 100%)",
        ServiceCategory.Physiotherapy => "linear-gradient(135deg, #14B8A6 0%, #2DD4BF 100%)",
        ServiceCategory.Tutoring => "linear-gradient(135deg, #6366F1 0%, #818CF8 100%)",
        ServiceCategory.Automotive => "linear-gradient(135deg, #64748B 0%, #94A3B8 100%)",
        ServiceCategory.HomeServices => "linear-gradient(135deg, #84CC16 0%, #A3E635 100%)",
        ServiceCategory.PetCare => "linear-gradient(135deg, #FBBF24 0%, #FCD34D 100%)",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown service category")
    };

    /// <summary>
    /// Gets the URL-friendly slug for the category
    /// </summary>
    /// <param name="category">The service category</param>
    /// <returns>Lowercase slug (e.g., "hair-salon")</returns>
    public static string ToSlug(this ServiceCategory category) => category switch
    {
        ServiceCategory.HairSalon => "hair-salon",
        ServiceCategory.Barbershop => "barbershop",
        ServiceCategory.BeautySalon => "beauty-salon",
        ServiceCategory.NailSalon => "nail-salon",
        ServiceCategory.Spa => "spa",
        ServiceCategory.Massage => "massage",
        ServiceCategory.Gym => "gym",
        ServiceCategory.Yoga => "yoga",
        ServiceCategory.MedicalClinic => "medical-clinic",
        ServiceCategory.Dental => "dental",
        ServiceCategory.Physiotherapy => "physiotherapy",
        ServiceCategory.Tutoring => "tutoring",
        ServiceCategory.Automotive => "automotive",
        ServiceCategory.HomeServices => "home-services",
        ServiceCategory.PetCare => "pet-care",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown service category")
    };

    /// <summary>
    /// Gets a brief description for the category
    /// </summary>
    /// <param name="category">The service category</param>
    /// <returns>Category description</returns>
    public static string ToDescription(this ServiceCategory category) => category switch
    {
        ServiceCategory.HairSalon => "خدمات آرایشگری زنانه شامل کوتاهی، رنگ، مش و...",
        ServiceCategory.Barbershop => "خدمات آرایشگری مردانه شامل اصلاح، کوتاهی و...",
        ServiceCategory.BeautySalon => "خدمات زیبایی شامل آرایش، پاکسازی پوست و...",
        ServiceCategory.NailSalon => "خدمات ناخن شامل مانیکور، پدیکور، طراحی ناخن",
        ServiceCategory.Spa => "خدمات اسپا و آرامش بخشی",
        ServiceCategory.Massage => "خدمات ماساژ و رفع خستگی",
        ServiceCategory.Gym => "خدمات ورزشی و تناسب اندام",
        ServiceCategory.Yoga => "خدمات یوگا و مدیتیشن",
        ServiceCategory.MedicalClinic => "خدمات پزشکی و درمانی",
        ServiceCategory.Dental => "خدمات دندانپزشکی",
        ServiceCategory.Physiotherapy => "خدمات فیزیوتراپی و توانبخشی",
        ServiceCategory.Tutoring => "خدمات آموزشی و تدریس خصوصی",
        ServiceCategory.Automotive => "خدمات تعمیرات و نگهداری خودرو",
        ServiceCategory.HomeServices => "خدمات منزل و تعمیرات",
        ServiceCategory.PetCare => "خدمات مراقبت و نگهداری حیوانات",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown service category")
    };

    /// <summary>
    /// Gets all available service categories
    /// </summary>
    /// <returns>Array of all service categories</returns>
    public static ServiceCategory[] GetAll()
    {
        return Enum.GetValues<ServiceCategory>();
    }

    /// <summary>
    /// Tries to parse a slug to a ServiceCategory
    /// </summary>
    /// <param name="slug">The slug to parse</param>
    /// <param name="category">The parsed category if successful</param>
    /// <returns>True if parsing succeeded, false otherwise</returns>
    public static bool TryParseSlug(string slug, out ServiceCategory category)
    {
        category = slug?.ToLowerInvariant() switch
        {
            "hair-salon" or "hair_salon" => ServiceCategory.HairSalon,
            "barbershop" or "barber" => ServiceCategory.Barbershop,
            "beauty-salon" or "beauty_salon" or "beauty" => ServiceCategory.BeautySalon,
            "nail-salon" or "nail_salon" or "nails" => ServiceCategory.NailSalon,
            "spa" => ServiceCategory.Spa,
            "massage" => ServiceCategory.Massage,
            "gym" or "fitness" => ServiceCategory.Gym,
            "yoga" => ServiceCategory.Yoga,
            "medical-clinic" or "medical_clinic" or "clinic" => ServiceCategory.MedicalClinic,
            "dental" => ServiceCategory.Dental,
            "physiotherapy" or "physio" => ServiceCategory.Physiotherapy,
            "tutoring" or "education" => ServiceCategory.Tutoring,
            "automotive" or "auto" => ServiceCategory.Automotive,
            "home-services" or "home_services" => ServiceCategory.HomeServices,
            "pet-care" or "pet_care" or "pet" => ServiceCategory.PetCare,
            _ => (ServiceCategory?)null
        } ?? default;

        return category != default || slug == "0";
    }
}
