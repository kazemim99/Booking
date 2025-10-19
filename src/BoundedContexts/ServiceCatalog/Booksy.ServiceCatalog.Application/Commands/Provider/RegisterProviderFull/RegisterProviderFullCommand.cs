// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/RegisterProviderFull/RegisterProviderFullCommand.cs
// ========================================

using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.RegisterProviderFull
{
    /// <summary>
    /// Command to register a provider with complete multi-step data
    /// </summary>
    public sealed record RegisterProviderFullCommand(
        Guid OwnerId,
        string CategoryId,
        BusinessInfoDto BusinessInfo,
        AddressDto Address,
        LocationDto? Location,
        Dictionary<int, DayHoursDto?> BusinessHours,
        List<ServiceDto> Services,
        List<string> AssistanceOptions,
        List<TeamMemberDto> TeamMembers,
        Guid? IdempotencyKey = null) : ICommand<RegisterProviderFullResult>;

    /// <summary>
    /// Business information DTO
    /// </summary>
    public sealed record BusinessInfoDto(
        string BusinessName,
        string OwnerFirstName,
        string OwnerLastName,
        string PhoneNumber);

    /// <summary>
    /// Address DTO
    /// </summary>
    public sealed record AddressDto(
        string AddressLine1,
        string? AddressLine2,
        string City,
        string ZipCode);

    /// <summary>
    /// Location coordinates DTO
    /// </summary>
    public sealed record LocationDto(
        double Latitude,
        double Longitude,
        string? FormattedAddress);

    /// <summary>
    /// Day hours DTO
    /// </summary>
    public sealed record DayHoursDto(
        int DayOfWeek,
        bool IsOpen,
        TimeSlotDto? OpenTime,
        TimeSlotDto? CloseTime,
        List<BreakTimeDto> Breaks);

    /// <summary>
    /// Time slot DTO
    /// </summary>
    public sealed record TimeSlotDto(
        int Hours,
        int Minutes);

    /// <summary>
    /// Break time DTO
    /// </summary>
    public sealed record BreakTimeDto(
        TimeSlotDto Start,
        TimeSlotDto End);

    /// <summary>
    /// Service DTO
    /// </summary>
    public sealed record ServiceDto(
        string Name,
        int DurationHours,
        int DurationMinutes,
        decimal Price,
        string PriceType);

    /// <summary>
    /// Team member DTO
    /// </summary>
    public sealed record TeamMemberDto(
        string Name,
        string Email,
        string PhoneNumber,
        string CountryCode,
        string Position,
        bool IsOwner);
}
