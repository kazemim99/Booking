// ========================================
// Booksy.ServiceCatalog.Application/Queries/Category/GetCategoriesWithCounts/CategoryWithCountViewModel.cs
// ========================================

namespace Booksy.ServiceCatalog.Application.Queries.Category.GetCategoriesWithCounts
{
    /// <summary>
    /// Category with provider count for display
    /// </summary>
    public sealed class CategoryWithCountViewModel
    {
        /// <summary>
        /// ServiceCategory enum value. This is the identifier clients filter and register with,
        /// and it matches the integer stored in Providers.PrimaryCategory.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Stable enum member name (e.g. "HairSalon"), for clients that key off the name
        /// rather than the number.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Category name in Persian
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Category name in English
        /// </summary>
        public string EnglishName { get; set; } = string.Empty;

        /// <summary>
        /// Category slug for URL/routing
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Category description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Icon/emoji for display
        /// </summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// Color hex code for UI
        /// </summary>
        public string Color { get; set; } = "#6366F1";

        /// <summary>
        /// Gradient string for cards
        /// </summary>
        public string Gradient { get; set; } = "linear-gradient(135deg, #667eea 0%, #764ba2 100%)";

        /// <summary>
        /// Number of active providers offering this category
        /// </summary>
        public int ProviderCount { get; set; }

        /// <summary>
        /// True when the category is part of the taxonomy but has no active providers yet.
        /// The browse page shows these as "Coming Soon" rather than hiding them, so the
        /// catalogue reads as complete.
        /// </summary>
        public bool IsComingSoon { get; set; }

        /// <summary>
        /// Display order (lower = higher priority)
        /// </summary>
        public int DisplayOrder { get; set; }
    }
}
