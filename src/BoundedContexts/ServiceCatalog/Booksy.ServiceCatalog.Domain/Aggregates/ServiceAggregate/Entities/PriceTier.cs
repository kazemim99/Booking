// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/ServiceAggregate/Entities/PriceTier.cs
// ========================================

namespace Booksy.ServiceCatalog.Domain.Entities
{
    /// <summary>
    /// Price tier for service within Service aggregate
    /// </summary>
    public sealed class PriceTier : Entity<Guid>
    {
        public string Name { get; private set; }
        public string? Description { get; private set; }
        public Price Price { get; private set; }
        public bool IsDefault { get; private set; }
        public bool IsActive { get; private set; }
        public int SortOrder { get; private set; }
        public Dictionary<string, string> Attributes { get; private set; } = new();
        public ServiceId ServiceId { get; private set; }

        // Private constructor for EF Core
        private PriceTier() : base() { }

        internal static PriceTier Create(string name, Price price, string? description = null)
        {
            return new PriceTier
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                Price = price,
                IsDefault = false,
                IsActive = true,
                SortOrder = 0,
            };
        }

        public void Update(string name, string? description, Price price)
        {
            Name = name;
            Description = description;
            Price = price;
        }

        public void SetAsDefault()
        {
            IsDefault = true;
        }

        public void UnsetAsDefault()
        {
            IsDefault = false;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void SetSortOrder(int sortOrder)
        {
            if (sortOrder < 0)
                throw new ArgumentException("Sort order cannot be negative", nameof(sortOrder));

            SortOrder = sortOrder;
        }

        public void SetAttribute(string key, string value)
        {
            Attributes[key] = value;
        }

        public void RemoveAttribute(string key)
        {
            Attributes.Remove(key);
        }

        public string? GetAttribute(string key)
        {
            return Attributes.TryGetValue(key, out var value) ? value : null;
        }
    }
}