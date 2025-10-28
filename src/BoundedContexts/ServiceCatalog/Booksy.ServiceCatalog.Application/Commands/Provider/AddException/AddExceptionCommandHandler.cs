// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/AddException/AddExceptionCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Entities;
using ExceptionSchedule = Booksy.ServiceCatalog.Domain.Entities.ExceptionSchedule;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.AddException;

public sealed class AddExceptionCommandHandler
    : ICommandHandler<AddExceptionCommand, AddExceptionResult>
{
    private readonly IProviderWriteRepository _providerRepository;

    public AddExceptionCommandHandler(IProviderWriteRepository providerRepository)
    {
        _providerRepository = providerRepository;
    }

    public async Task<AddExceptionResult> Handle(
        AddExceptionCommand command,
        CancellationToken cancellationToken)
    {
        // Retrieve provider
        var provider = await _providerRepository.GetByIdAsync(
            ProviderId.From(command.ProviderId),
            cancellationToken);

        if (provider == null)
            throw new DomainValidationException("Provider not found");

        // Add exception
        ExceptionSchedule exception;
        if (command.OpenTime.HasValue && command.CloseTime.HasValue)
        {
            exception = provider.AddException(
                command.Date,
                command.OpenTime.Value,
                command.CloseTime.Value,
                command.Reason);
        }
        else
        {
            exception = provider.AddClosedException(command.Date, command.Reason);
        }

        // Save changes
        await _providerRepository.UpdateAsync(provider, cancellationToken);

        return new AddExceptionResult(
            ExceptionId: exception.Id,
            Success: true,
            Message: "Exception added successfully");
    }
}
