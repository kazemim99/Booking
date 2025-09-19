
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

        private ServiceCategory(string name, string? description = null, string? iconUrl = null, string color = "#6366F1")
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Category name cannot be empty", nameof(name));

            Name = name.Trim();
            Description = description?.Trim();
            IconUrl = iconUrl;
            Color = color;
        }

        public static ServiceCategory Create(string name, string? description = null, string? iconUrl = null, string color = "#6366F1")
            => new(name, description, iconUrl, color);

        // Predefined categories
        public static ServiceCategory Beauty => Create("Beauty & Wellness", "Beauty and wellness services", null, "#EC4899");
        public static ServiceCategory Health => Create("Healthcare", "Health and medical services", null, "#10B981");
        public static ServiceCategory Fitness => Create("Fitness & Sports", "Fitness and sports services", null, "#F59E0B");
        public static ServiceCategory Education => Create("Education & Training", "Educational and training services", null, "#3B82F6");
        public static ServiceCategory Professional => Create("Professional Services", "Professional and business services", null, "#6366F1");
        public static ServiceCategory Home => Create("Home Services", "Home and maintenance services", null, "#8B5CF6");
        public static ServiceCategory Automotive => Create("Automotive", "Car and vehicle services", null, "#EF4444");
        public static ServiceCategory Pet => Create("Pet Services", "Pet care and veterinary services", null, "#06B6D4");

        public override string ToString() => Name;

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Name.ToLowerInvariant();
        }
    }
}