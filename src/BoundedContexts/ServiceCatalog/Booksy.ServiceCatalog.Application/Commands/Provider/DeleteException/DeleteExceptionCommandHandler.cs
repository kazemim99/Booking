// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/DeleteException/DeleteExceptionCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.DeleteException;

public sealed class DeleteExceptionCommandHandler
    : ICommandHandler<DeleteExceptionCommand, DeleteExceptionResult>
{
    private readonly IProviderWriteRepository _providerRepository;

    public DeleteExceptionCommandHandler(IProviderWriteRepository providerRepository)
    {
        _providerRepository = providerRepository;
    }

    public async Task<DeleteExceptionResult> Handle(
        DeleteExceptionCommand command,
        CancellationToken cancellationToken)
    {
        // Retrieve provider
        var provider = await _providerRepository.GetByIdAsync(
            ProviderId.From(command.ProviderId),
            cancellationToken);

        if (provider == null)
            throw new DomainValidationException("Provider not found");

        // Remove exception
        provider.RemoveException(command.ExceptionId);

        // Save changes
        await _providerRepository.UpdateAsync(provider, cancellationToken);

        return new DeleteExceptionResult(
            Success: true,
            Message: "Exception deleted successfully");
    }
}
