//using MediatR;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Booksy.ServiceCatalog.Application.Commands.RegisterProvider;
//using Booksy.ServiceCatalog.Application.Commands.UpdateProvider;
//using Booksy.ServiceCatalog.Application.Commands.ActivateProvider;
//using Booksy.ServiceCatalog.Application.Commands.DeactivateProvider;
//using Booksy.ServiceCatalog.Application.Queries.GetProviderById;
//using Booksy.ServiceCatalog.Application.Queries.GetProvidersByLocation;
//using Booksy.ServiceCatalog.Application.Queries.SearchProviders;
//using Booksy.ServiceCatalog.API.Models.Requests;
//using Booksy.ServiceCatalog.API.Models.Responses;
//using Booksy.ServiceCatalog.Domain.ValueObjects;
//using Booksy.Core.Application.DTOs;

//namespace Booksy.ServiceCatalog.API.Controllers.V1;



//[ApiController]
//[Route("api/v1/[controller]")]
//public class CategoriesController : ControllerBase
//{
//    /// <summary>
//    /// Get all available service categories
//    /// </summary>
//    [HttpGet]
//    [AllowAnonymous]
//    public ActionResult<IEnumerable<CategoryResponse>> GetCategories()
//    {
//        var categories = Enum.GetValues<ServiceCategory>()
//            .Select(category => new CategoryResponse
//            {
//                Id = (int)category,
//                Name = category.ToString(),
//                DisplayName = GetCategoryDisplayName(category),
//                Description = GetCategoryDescription(category)
//            })
//            .ToList();

//        return Ok(categories);
//    }

//    /// <summary>
//    /// Get provider types
//    /// </summary>
//    [HttpGet("provider-types")]
//    [AllowAnonymous]
//    public ActionResult<IEnumerable<ProviderTypeResponse>> GetProviderTypes()
//    {
//        var providerTypes = Enum.GetValues<ProviderType>()
//            .Select(type => new ProviderTypeResponse
//            {
//                Id = (int)type,
//                Name = type.ToString(),
//                DisplayName = GetProviderTypeDisplayName(type),
//                Description = GetProviderTypeDescription(type)
//            })
//            .ToList();

//        return Ok(providerTypes);
//    }

//    private static string GetCategoryDisplayName(ServiceCategory category)
//    {
//        return category switch
//        {
//            ServiceCategory.BeautyAndWellness => "Beauty & Wellness",
//            ServiceCategory.HealthAndMedical => "Health & Medical",
//            ServiceCategory.FitnessAndSports => "Fitness & Sports",
//            ServiceCategory.EducationAndTraining => "Education & Training",
//            ServiceCategory.HomeAndMaintenance => "Home & Maintenance",
//            ServiceCategory.ProfessionalServices => "Professional Services",
//            ServiceCategory.AutomotiveServices => "Automotive Services",
//            ServiceCategory.PetCare => "Pet Care",
//            ServiceCategory.EventsAndEntertainment => "Events & Entertainment",
//            ServiceCategory.TechnologyServices => "Technology Services",
//            _ => category.ToString()
//        };
//    }

//    private static string GetCategoryDescription(ServiceCategory category)
//    {
//        return category switch
//        {
//            ServiceCategory.BeautyAndWellness => "Hair, makeup, massage, spa, and beauty services",
//            ServiceCategory.HealthAndMedical => "Medical consultations, therapy, and healthcare services",
//            ServiceCategory.FitnessAndSports => "Personal training, sports coaching, and fitness services",
//            ServiceCategory.EducationAndTraining => "Tutoring, coaching, workshops, and educational services",
//            ServiceCategory.HomeAndMaintenance => "Home repairs, cleaning, and maintenance services",
//            ServiceCategory.ProfessionalServices => "Legal, consulting, accounting, and business services",
//            ServiceCategory.AutomotiveServices => "Car maintenance, repairs, and automotive services",
//            ServiceCategory.PetCare => "Pet grooming, training, and veterinary services",
//            ServiceCategory.EventsAndEntertainment => "Event planning, entertainment, and celebration services",
//            ServiceCategory.TechnologyServices => "IT support, software development, and tech consulting",
//            _ => $"Services related to {category}"
//        };
//    }

//    private static string GetProviderTypeDisplayName(ProviderType type)
//    {
//        return type switch
//        {
//            ProviderType.GymFitness => "Gym & Fitness",
//            ProviderType.HomeServices => "Home Services",
//            ProviderType.PetServices => "Pet Services",
//            _ => type.ToString()
//        };
//    }

//    private static string GetProviderTypeDescription(ProviderType type)
//    {
//        return type switch
//        {
//            ProviderType.Individual => "Solo service provider or freelancer",
//            ProviderType.Business => "General business or company",
//            ProviderType.Clinic => "Medical or therapeutic clinic",
//            ProviderType.Salon => "Beauty salon or barbershop",
//            ProviderType.Spa => "Spa and wellness center",
//            ProviderType.GymFitness => "Gym, fitness center, or sports facility",
//            ProviderType.Educational => "School, training center, or educational institution",
//            ProviderType.Medical => "Hospital, medical practice, or healthcare facility",
//            ProviderType.Automotive => "Auto repair shop or automotive service center",
//            ProviderType.HomeServices => "Home improvement or maintenance company",
//            ProviderType.PetServices => "Pet care facility or veterinary practice",
//            ProviderType.Professional => "Professional services firm or consultancy",
//            _ => $"Provider type: {type}"
//        };
//    }
//}
