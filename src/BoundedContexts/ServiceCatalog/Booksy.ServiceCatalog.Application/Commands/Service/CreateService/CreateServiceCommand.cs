// ========================================
// Booksy.ServiceCatalog.Application/Commands/Service/CreateService/CreateServiceCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Service.CreateService
{
    /// <summary>
    /// Command to create a new service offering
    /// </summary>
    public sealed record CreateServiceCommand(
        Guid ProviderId,
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
        Guid? IdempotencyKey = null) : ICommand<CreateServiceResult>;
}