// ========================================
// Booksy.ServiceCatalog.Application/Mappings/ServiceMappingProfile.cs
// ========================================
using AutoMapper;
using Booksy.ServiceCatalog.Application.DTOs.Service;
using Booksy.ServiceCatalog.Domain.Entities;

namespace Booksy.ServiceCatalog.Application.Mappings
{
    public sealed class ServiceMappingProfile : Profile
    {
        public ServiceMappingProfile()
        {
            CreateMap<Domain.Aggregates.Service, ServiceDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value))
                .ForMember(dest => dest.ProviderId, opt => opt.MapFrom(src => src.ProviderId.Value))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category.Name))
                .ForMember(dest => dest.BasePrice, opt => opt.MapFrom(src => src.BasePrice.Amount))
                .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.BasePrice.Currency))
                .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.Duration.Value))
                .ForMember(dest => dest.PreparationTime, opt => opt.MapFrom(src => src.PreparationTime != null ? src.PreparationTime.Value : (int?)null))
                .ForMember(dest => dest.BufferTime, opt => opt.MapFrom(src => src.BufferTime != null ? src.BufferTime.Value : (int?)null));

            CreateMap<ServiceOption, ServiceOptionDto>()
                .ForMember(dest => dest.AdditionalPrice, opt => opt.MapFrom(src => src.AdditionalPrice.Amount))
                .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.AdditionalPrice.Currency))
                .ForMember(dest => dest.AdditionalDuration, opt => opt.MapFrom(src => src.AdditionalDuration != null ? src.AdditionalDuration.Value : (int?)null));

            CreateMap<PriceTier, PriceTierDto>()
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price.Amount))
                .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Price.Currency));
        }
    }
}