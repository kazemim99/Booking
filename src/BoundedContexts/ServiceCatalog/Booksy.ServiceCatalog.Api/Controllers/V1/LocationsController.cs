using Booksy.Core.Domain.Domain.Entities;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booksy.ServiceCatalog.Api.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class LocationsController : ControllerBase
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<LocationsController> _logger;

        public LocationsController(
            ServiceCatalogDbContext context,
            ILogger<LocationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all provinces
        /// </summary>
        /// <returns>List of provinces</returns>
        [HttpGet("provinces")]
        [ProducesResponseType(typeof(IEnumerable<LocationDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<LocationDto>>> GetProvinces()
        {
            var provinces = await _context.Set<ProvinceCities>()
                .Where(l => l.Type == "Province")
                .OrderBy(l => l.Name)
                .Select(l => new LocationDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    ProvinceCode = l.ProvinceCode,
                    Type = l.Type
                })
                .ToListAsync();

            return Ok(provinces);
        }

        /// <summary>
        /// Get cities for a specific province
        /// </summary>
        /// <param name="provinceId">The province ID</param>
        /// <returns>List of cities in the province</returns>
        [HttpGet("provinces/{provinceId}/cities")]
        [ProducesResponseType(typeof(IEnumerable<LocationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<LocationDto>>> GetCitiesByProvince(int provinceId)
        {
            // Check if province exists
            var provinceExists = await _context.Set<ProvinceCities>()
                .AnyAsync(l => l.Id == provinceId && l.Type == "Province");

            if (!provinceExists)
            {
                return NotFound(new { message = $"Province with ID {provinceId} not found" });
            }

            var cities = await _context.Set<ProvinceCities>()
                .Where(l => l.ParentId == provinceId && l.Type == "City")
                .OrderBy(l => l.Name)
                .Select(l => new LocationDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    ProvinceCode = l.ProvinceCode,
                    CityCode = l.CityCode,
                    ParentId = l.ParentId,
                    Type = l.Type
                })
                .ToListAsync();

            return Ok(cities);
        }

        /// <summary>
        /// Get the complete hierarchy (provinces with their cities)
        /// </summary>
        /// <returns>Hierarchical list of provinces with nested cities</returns>
        [HttpGet("hierarchy")]
        [ProducesResponseType(typeof(IEnumerable<ProvinceHierarchyDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ProvinceHierarchyDto>>> GetHierarchy()
        {
            var provinces = await _context.Set<ProvinceCities>()
                .Include(l => l.Children)
                .Where(l => l.Type == "Province")
                .OrderBy(l => l.Name)
                .ToListAsync();

            var hierarchy = provinces.Select(p => new ProvinceHierarchyDto
            {
                Id = p.Id,
                Name = p.Name,
                ProvinceCode = p.ProvinceCode,
                Cities = p.Children
                    .OrderBy(c => c.Name)
                    .Select(c => new CityDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        CityCode = c.CityCode
                    })
                    .ToList()
            });

            return Ok(hierarchy);
        }

        /// <summary>
        /// Search locations by name
        /// </summary>
        /// <param name="query">Search term (partial match)</param>
        /// <param name="type">Optional type filter: "Province" or "City"</param>
        /// <returns>Matching locations</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<LocationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<LocationDto>>> Search(
            [FromQuery] string query,
            [FromQuery] string? type = null)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { message = "Search query is required" });
            }

            if (query.Length < 2)
            {
                return BadRequest(new { message = "Search query must be at least 2 characters" });
            }

            var locationsQuery = _context.Set<ProvinceCities>()
                .Where(l => l.Name.Contains(query));

            if (!string.IsNullOrEmpty(type))
            {
                if (type != "Province" && type != "City")
                {
                    return BadRequest(new { message = "Type must be either 'Province' or 'City'" });
                }
                locationsQuery = locationsQuery.Where(l => l.Type == type);
            }

            var results = await locationsQuery
                .OrderBy(l => l.Type)
                .ThenBy(l => l.Name)
                .Take(50)
                .Select(l => new LocationDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    ProvinceCode = l.ProvinceCode,
                    CityCode = l.CityCode,
                    ParentId = l.ParentId,
                    Type = l.Type
                })
                .ToListAsync();

            return Ok(results);
        }

        /// <summary>
        /// Get a specific location by ID
        /// </summary>
        /// <param name="id">Location ID</param>
        /// <returns>Location details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LocationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LocationDto>> GetById(int id)
        {
            var location = await _context.Set<ProvinceCities>()
                .Where(l => l.Id == id)
                .Select(l => new LocationDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    ProvinceCode = l.ProvinceCode,
                    CityCode = l.CityCode,
                    ParentId = l.ParentId,
                    Type = l.Type
                })
                .FirstOrDefaultAsync();

            if (location == null)
            {
                return NotFound(new { message = $"Location with ID {id} not found" });
            }

            return Ok(location);
        }

        /// <summary>
        /// Get location with parent information
        /// </summary>
        /// <param name="id">Location ID</param>
        /// <returns>Location with parent details</returns>
        [HttpGet("{id}/with-parent")]
        [ProducesResponseType(typeof(LocationWithParentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LocationWithParentDto>> GetWithParent(int id)
        {
            var location = await _context.Set<ProvinceCities>()
                .Include(l => l.Parent)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location == null)
            {
                return NotFound(new { message = $"Location with ID {id} not found" });
            }

            var result = new LocationWithParentDto
            {
                Id = location.Id,
                Name = location.Name,
                ProvinceCode = location.ProvinceCode,
                CityCode = location.CityCode,
                ParentId = location.ParentId,
                Type = location.Type,
                Parent = location.Parent != null ? new LocationDto
                {
                    Id = location.Parent.Id,
                    Name = location.Parent.Name,
                    ProvinceCode = location.Parent.ProvinceCode,
                    Type = location.Parent.Type
                } : null
            };

            return Ok(result);
        }
    }

    #region DTOs

    public class LocationDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProvinceCode { get; set; }
        public int? CityCode { get; set; }
        public int? ParentId { get; set; }
        public string Type { get; set; } = string.Empty;
    }

    public class ProvinceHierarchyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProvinceCode { get; set; }
        public List<CityDto> Cities { get; set; } = new();
    }

    public class CityDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? CityCode { get; set; }
    }

    public class LocationWithParentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProvinceCode { get; set; }
        public int? CityCode { get; set; }
        public int? ParentId { get; set; }
        public string Type { get; set; } = string.Empty;
        public LocationDto? Parent { get; set; }
    }

    #endregion
}
