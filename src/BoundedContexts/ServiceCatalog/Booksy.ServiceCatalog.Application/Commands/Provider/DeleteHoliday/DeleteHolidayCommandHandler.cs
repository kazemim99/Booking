// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/DeleteHoliday/DeleteHolidayCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.DeleteHoliday;

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
