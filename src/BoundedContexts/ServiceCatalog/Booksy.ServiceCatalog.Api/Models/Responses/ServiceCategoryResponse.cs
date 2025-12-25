namespace Booksy.ServiceCatalog.API.Models.Responses;

/// <summary>
/// Response model for service category information
/// </summary>
public class ServiceCategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PersianName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
