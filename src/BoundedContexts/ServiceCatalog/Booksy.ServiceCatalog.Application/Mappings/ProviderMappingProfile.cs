// ========================================
// Booksy.ServiceCatalog.Application/Mappings/ProviderMappingProfile.cs
// ========================================
using AutoMapper;
using Booksy.ServiceCatalog.Application.DTOs.Provider;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Entities;

namespace Booksy.ServiceCatalog.Application.Mappings
{
    public sealed class ProviderMappingProfile : Profile
    {
        public ProviderMappingProfile()
        {
            CreateMap<Provider, ProviderDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value))
                .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.OwnerId.Value))
                .ForMember(dest => dest.Profile, opt => opt.MapFrom(src => src.Profile))
                .ForMember(dest => dest.ContactInfo, opt => opt.MapFrom(src => src.ContactInfo))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address));

            CreateMap<BusinessProfile, BusinessProfileDto>();

            CreateMap<Staff, StaffDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone != null ? src.Phone.Value : null));

            CreateMap<BusinessHours, BusinessHoursDto>()
                .ForMember(dest => dest.IsOpen, opt => opt.MapFrom(src => src.IsOpen))
                .ForMember(dest => dest.OpenTime, opt => opt.MapFrom(src => src.OpenTime))
                .ForMember(dest => dest.CloseTime, opt => opt.MapFrom(src => src.CloseTime));
        }
    }
}