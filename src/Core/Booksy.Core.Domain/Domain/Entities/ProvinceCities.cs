using Booksy.Core.Domain.Base;

namespace Booksy.Core.Domain.Domain.Entities
{
    /// <summary>
    /// Represents a hierarchical location (Province or City)
    /// </summary>
    public sealed class ProvinceCities : Entity<int>
    {
        /// <summary>
        /// The name of the location (Province or City name in Persian)
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// The province code from the original data
        /// </summary>
        public int ProvinceCode { get; private set; }

        /// <summary>
        /// The city code from the original data (null for provinces)
        /// </summary>
        public int? CityCode { get; private set; }

        /// <summary>
        /// The parent location ID (null for provinces, province ID for cities)
        /// </summary>
        public int? ParentId { get; private set; }

        /// <summary>
        /// The type of location: "Province" or "City"
        /// </summary>
        public string Type { get; private set; } = string.Empty;

        // Navigation property for Entity Framework
        public ProvinceCities? Parent { get; private set; }
        public ICollection<ProvinceCities> Children { get; private set; } = new List<ProvinceCities>();

        // Private constructor for EF Core
        private ProvinceCities() { }

        /// <summary>
        /// Creates a new province (root level location)
        /// </summary>
        public static ProvinceCities CreateProvince(int id, string name, int provinceCode)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Province name cannot be empty", nameof(name));

            return new ProvinceCities
            {
                Id = id,
                Name = name,
                ProvinceCode = provinceCode,
                CityCode = null,
                ParentId = null,
                Type = "Province"
            };
        }

        /// <summary>
        /// Creates a new city (child level location)
        /// </summary>
        public static ProvinceCities CreateCity(int id, string name, int provinceCode, int cityCode, int parentId)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("City name cannot be empty", nameof(name));

            return new ProvinceCities
            {
                Id = id,
                Name = name,
                ProvinceCode = provinceCode,
                CityCode = cityCode,
                ParentId = parentId,
                Type = "City"
            };
        }
    }
}
