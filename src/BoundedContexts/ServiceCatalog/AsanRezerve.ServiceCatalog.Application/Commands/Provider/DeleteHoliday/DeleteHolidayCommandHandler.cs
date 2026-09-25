// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Provider/DeleteHoliday/DeleteHolidayCommandHandler.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.DeleteHoliday;

public sealed class DeleteHolidayCommandHandler
    : ICommandHandler<DeleteHolidayCommand, DeleteHolidayResult>
{
    private readonly IProviderWriteRepository _providerRepository;

    public DeleteHolidayCommandHandler(IProviderWriteRepository providerRepository)
    {
        _providerRepository = providerRepository;
    }

    public async Task<DeleteHolidayResult> Handle(
        DeleteHolidayCommand command,
        CancellationToken cancellationToken)
    {
        // Retrieve provider
        var provider = await _providerRepository.GetByIdAsync(
            ProviderId.From(command.ProviderId),
            cancellationToken);

        if (provider == null)
            throw new DomainValidationException("Provider not found");

        // Remove holiday
        provider.RemoveHoliday(command.HolidayId);

        // Save changes
        await _providerRepository.UpdateAsync(provider, cancellationToken);

        return new DeleteHolidayResult(
            Success: true,
            Message: "Holiday deleted successfully");
    }
}
