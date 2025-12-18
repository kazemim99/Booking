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
        /// Category name in Persian
        /// </summary>
        public string Name { get; set; } = string.Empty;

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
        /// Display order (lower = higher priority)
        /// </summary>
        public int DisplayOrder { get; set; }
    }
}
