// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/ServiceAggregate/Entities/ServiceOption.cs
// ========================================
namespace Booksy.ServiceCatalog.Domain.Entities
{
    /// <summary>
    /// Service option/add-on within Service aggregate
    /// </summary>
    public sealed class ServiceOption : Entity<Guid>
    {
        public string Name { get; private set; }
        public string? Description { get; private set; }
        public Price AdditionalPrice { get; private set; }
        public Duration? AdditionalDuration { get; private set; }
        public bool IsRequired { get; private set; }
        public bool IsActive { get; private set; }
        public int SortOrder { get; private set; }
        // ServiceId is a shadow property managed by EF Core for owned entity relationship
        // Private constructor for EF Core
        private ServiceOption() : base() { }

        internal static ServiceOption Create(string name, Price additionalPrice, Duration? additionalDuration = null, string? description = null)
        {
            var option = new ServiceOption
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                AdditionalPrice = additionalPrice,
                AdditionalDuration = additionalDuration,
                IsRequired = false,
                IsActive = true,
                SortOrder = 0
            };
            option.SetCreatedAt(DateTime.UtcNow);
            return option;
        }

        public void Update(string name, string? description, Price additionalPrice, Duration? additionalDuration = null)
        {
            Name = name;
            Description = description;
            AdditionalPrice = additionalPrice;
            AdditionalDuration = additionalDuration;
        }

        public void SetRequired(bool isRequired)
        {
            IsRequired = isRequired;
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

        public Price GetTotalAdditionalCost(int quantity = 1)
        {
            return AdditionalPrice * quantity;
        }
    }
}