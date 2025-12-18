
namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    public sealed class ServiceCategory : ValueObject
    {
        private ServiceCategory()
        {

        }
        public string Name { get; }
        public string? Description { get; }
        public string? IconUrl { get; }
        public string Color { get; }
        public string Slug { get; }
        public string Gradient { get; }
        public int DisplayOrder { get; }

        private ServiceCategory(string name, string slug, string? description = null, string? iconUrl = null, string color = "#6366F1", string? gradient = null, int displayOrder = 0)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Category name cannot be empty", nameof(name));

            Name = name.Trim();
            Slug = slug.Trim().ToLowerInvariant();
            Description = description?.Trim();
            IconUrl = iconUrl;
            Color = color;
            Gradient = gradient ?? $"linear-gradient(135deg, {color} 0%, {color} 100%)";
            DisplayOrder = displayOrder;
        }

        public static ServiceCategory Create(string name, string slug, string? description = null, string? iconUrl = null, string color = "#6366F1", string? gradient = null, int displayOrder = 0)
            => new(name, slug, description, iconUrl, color, gradient, displayOrder);

        // Predefined categories - Persian names matching database
        public static ServiceCategory Beauty => Create("زیبایی", "beauty", "خدمات زیبایی و آرایشی", "💅", "#EC4899", "linear-gradient(135deg, #EC4899 0%, #F472B6 100%)", 1);
        public static ServiceCategory Makeup => Create("آرایش", "makeup", "خدمات آرایش و میکاپ", "💄", "#F472B6", "linear-gradient(135deg, #F472B6 0%, #FB923C 100%)", 2);
        public static ServiceCategory BeautyAndMakeup => Create("آرایش و زیبایی", "beauty-makeup", "خدمات آرایش و زیبایی", "✨", "#EC4899", "linear-gradient(135deg, #EC4899 0%, #A855F7 100%)", 3);
        public static ServiceCategory HairCare => Create("مراقبت مو", "hair-care", "خدمات مراقبت و درمان مو", "💇", "#8B5CF6", "linear-gradient(135deg, #8B5CF6 0%, #A78BFA 100%)", 4);
        public static ServiceCategory SkinCare => Create("مراقبت پوست", "skin-care", "خدمات مراقبت پوست", "🧖", "#10B981", "linear-gradient(135deg, #10B981 0%, #34D399 100%)", 5);
        public static ServiceCategory Massage => Create("ماساژ", "massage", "خدمات ماساژ", "💆", "#06B6D4", "linear-gradient(135deg, #06B6D4 0%, #22D3EE 100%)", 6);
        public static ServiceCategory TherapeuticMassage => Create("ماساژ درمانی", "therapeutic-massage", "ماساژ درمانی و فیزیوتراپی", "💆‍♀️", "#0EA5E9", "linear-gradient(135deg, #0EA5E9 0%, #06B6D4 100%)", 7);
        public static ServiceCategory Fitness => Create("فیتنس", "fitness", "خدمات ورزشی و فیتنس", "🏋️", "#F59E0B", "linear-gradient(135deg, #F59E0B 0%, #FBBF24 100%)", 8);

        /// <summary>
        /// All available categories for querying
        /// </summary>
        public static IReadOnlyList<ServiceCategory> All => new[]
        {
            Beauty,
            Makeup,
            BeautyAndMakeup,
            HairCare,
            SkinCare,
            Massage,
            TherapeuticMassage,
            Fitness
        };

        public override string ToString() => Name;

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Name.ToLowerInvariant();
        }
    }
}