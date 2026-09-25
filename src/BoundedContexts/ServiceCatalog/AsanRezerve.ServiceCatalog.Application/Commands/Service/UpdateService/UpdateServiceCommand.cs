// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Service/UpdateService/UpdateServiceCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Domain.Enums;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Service.UpdateService
{
    public sealed record UpdateServiceCommand(
        Guid ServiceId,
     string Name,
        string Description,
        string CategoryName,
        ServiceType ServiceType,
        decimal BasePrice,
        string Currency,
        int DurationMinutes,
        int? PreparationMinutes = null,
        int? BufferMinutes = null,
        bool RequiresDeposit = false,
        decimal DepositPercentage = 0,
        bool AvailableAtLocation = true,
        bool AvailableAsMobile = false,
        int MaxAdvanceBookingDays = 90,
        int MinAdvanceBookingHours = 1,
        int MaxConcurrentBookings = 1,
        string? ImageUrl = null,
        Guid? IdempotencyKey = null) : ICommand<UpdateServiceResult>;
}